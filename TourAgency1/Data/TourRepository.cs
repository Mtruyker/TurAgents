using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TourAgency.Models;

namespace TourAgency.Data
{
    public class TourRepository
    {
        private readonly string _connectionString;

        public TourRepository(string? connectionString = null)
        {
            _connectionString = connectionString ?? DatabaseSettings.ConnectionString;
        }

        public async Task<AgencyData> GetAgencyDataAsync()
        {
            await using var dataSource = NpgsqlDataSource.Create(_connectionString);

            return new AgencyData
            {
                Destinations = await GetDestinationsAsync(dataSource),
                Hotels = await GetHotelsAsync(dataSource),
                Tours = await GetToursAsync(dataSource),
                Clients = await GetClientsAsync(dataSource),
                Bookings = await GetBookingsAsync(dataSource),
                Payments = await GetPaymentsAsync(dataSource)
            };
        }

        public async Task<IReadOnlyList<Tour>> SearchToursAsync(string? destination, DateTime? startFrom, decimal? maxPrice, int peopleCount)
        {
            const string sql = """
                SELECT t.id, t.destination_id, t.hotel_id, d.name, d.country, d.city, h.name, h.stars,
                       t.title, t.description, t.price_per_person, t.start_date, t.end_date,
                       t.available_spots, t.meal_type, t.transport_type
                FROM tours t
                JOIN destinations d ON d.id = t.destination_id
                JOIN hotels h ON h.id = t.hotel_id
                WHERE (@destination IS NULL OR d.name ILIKE '%' || @destination || '%' OR d.country ILIKE '%' || @destination || '%' OR d.city ILIKE '%' || @destination || '%')
                  AND (@start_from IS NULL OR t.start_date >= @start_from)
                  AND (@max_price IS NULL OR t.price_per_person <= @max_price)
                  AND t.available_spots >= @people_count
                ORDER BY t.start_date, t.price_per_person, t.id;
                """;

            await using var dataSource = NpgsqlDataSource.Create(_connectionString);
            await using var command = dataSource.CreateCommand(sql);
            command.Parameters.Add("destination", NpgsqlDbType.Text).Value = string.IsNullOrWhiteSpace(destination) ? DBNull.Value : destination.Trim();
            command.Parameters.Add("start_from", NpgsqlDbType.Date).Value = startFrom.HasValue ? startFrom.Value.Date : DBNull.Value;
            command.Parameters.Add("max_price", NpgsqlDbType.Numeric).Value = maxPrice.HasValue ? maxPrice.Value : DBNull.Value;
            command.Parameters.AddWithValue("people_count", Math.Max(1, peopleCount));

            var tours = new List<Tour>();
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                tours.Add(ReadTour(reader));
            }

            return tours;
        }

        public async Task AddClientAsync(Client client)
        {
            const string sql = """
                INSERT INTO clients (full_name, phone, email, passport_number)
                VALUES (@full_name, @phone, @email, @passport_number);
                """;

            await using var dataSource = NpgsqlDataSource.Create(_connectionString);
            await using var command = dataSource.CreateCommand(sql);
            command.Parameters.AddWithValue("full_name", client.FullName.Trim());
            command.Parameters.AddWithValue("phone", client.Phone.Trim());
            command.Parameters.Add("email", NpgsqlDbType.Text).Value = ToDbText(client.Email);
            command.Parameters.Add("passport_number", NpgsqlDbType.Text).Value = ToDbText(client.PassportNumber);
            await command.ExecuteNonQueryAsync();
        }

        public async Task<int> CreateBookingAsync(int tourId, int clientId, int peopleCount, string notes)
        {
            const string tourSql = """
                SELECT price_per_person, available_spots
                FROM tours
                WHERE id = @tour_id
                FOR UPDATE;
                """;

            const string insertSql = """
                INSERT INTO bookings (tour_id, client_id, people_count, total_price, status, notes)
                VALUES (@tour_id, @client_id, @people_count, @total_price, 'new', @notes)
                RETURNING id;
                """;

            const string updateSql = """
                UPDATE tours
                SET available_spots = available_spots - @people_count
                WHERE id = @tour_id;
                """;

            await using var dataSource = NpgsqlDataSource.Create(_connectionString);
            await using var connection = await dataSource.OpenConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            await using var tourCommand = new NpgsqlCommand(tourSql, connection, transaction);
            tourCommand.Parameters.AddWithValue("tour_id", tourId);
            await using var reader = await tourCommand.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                throw new InvalidOperationException("Выбранный тур не найден.");
            }

            var price = reader.GetDecimal(0);
            var availableSpots = reader.GetInt32(1);
            await reader.CloseAsync();

            if (availableSpots < peopleCount)
            {
                throw new InvalidOperationException($"Недостаточно мест: доступно {availableSpots}.");
            }

            var totalPrice = price * peopleCount;

            await using var insertCommand = new NpgsqlCommand(insertSql, connection, transaction);
            insertCommand.Parameters.AddWithValue("tour_id", tourId);
            insertCommand.Parameters.AddWithValue("client_id", clientId);
            insertCommand.Parameters.AddWithValue("people_count", peopleCount);
            insertCommand.Parameters.AddWithValue("total_price", totalPrice);
            insertCommand.Parameters.Add("notes", NpgsqlDbType.Text).Value = ToDbText(notes);
            var bookingId = (int)(await insertCommand.ExecuteScalarAsync() ?? 0);

            await using var updateCommand = new NpgsqlCommand(updateSql, connection, transaction);
            updateCommand.Parameters.AddWithValue("tour_id", tourId);
            updateCommand.Parameters.AddWithValue("people_count", peopleCount);
            await updateCommand.ExecuteNonQueryAsync();

            await transaction.CommitAsync();
            return bookingId;
        }

        public async Task AddPaymentAsync(int bookingId, decimal amount, string method, string comment)
        {
            const string insertSql = """
                INSERT INTO payments (booking_id, amount, method, comment)
                VALUES (@booking_id, @amount, @method, @comment);
                """;

            const string updateStatusSql = """
                UPDATE bookings b
                SET status = CASE
                    WHEN p.paid_amount >= b.total_price THEN 'paid'
                    WHEN b.status = 'new' THEN 'confirmed'
                    ELSE b.status
                END
                FROM (
                    SELECT booking_id, COALESCE(SUM(amount), 0) AS paid_amount
                    FROM payments
                    WHERE booking_id = @booking_id
                    GROUP BY booking_id
                ) p
                WHERE b.id = p.booking_id;
                """;

            await using var dataSource = NpgsqlDataSource.Create(_connectionString);
            await using var connection = await dataSource.OpenConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            await using var insertCommand = new NpgsqlCommand(insertSql, connection, transaction);
            insertCommand.Parameters.AddWithValue("booking_id", bookingId);
            insertCommand.Parameters.AddWithValue("amount", amount);
            insertCommand.Parameters.AddWithValue("method", method.Trim());
            insertCommand.Parameters.Add("comment", NpgsqlDbType.Text).Value = ToDbText(comment);
            await insertCommand.ExecuteNonQueryAsync();

            await using var statusCommand = new NpgsqlCommand(updateStatusSql, connection, transaction);
            statusCommand.Parameters.AddWithValue("booking_id", bookingId);
            await statusCommand.ExecuteNonQueryAsync();

            await transaction.CommitAsync();
        }

        private static async Task<IReadOnlyList<Destination>> GetDestinationsAsync(NpgsqlDataSource dataSource)
        {
            const string sql = "SELECT id, country, city, name, description FROM destinations ORDER BY country, city, name;";
            var items = new List<Destination>();
            await using var command = dataSource.CreateCommand(sql);
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(new Destination
                {
                    Id = reader.GetInt32(0),
                    Country = reader.GetString(1),
                    City = reader.GetString(2),
                    Name = reader.GetString(3),
                    Description = reader.GetString(4)
                });
            }

            return items;
        }

        private static async Task<IReadOnlyList<Hotel>> GetHotelsAsync(NpgsqlDataSource dataSource)
        {
            const string sql = """
                SELECT h.id, h.destination_id, d.name, h.name, h.stars, h.address, h.description
                FROM hotels h
                JOIN destinations d ON d.id = h.destination_id
                ORDER BY d.name, h.stars DESC, h.name;
                """;
            var items = new List<Hotel>();
            await using var command = dataSource.CreateCommand(sql);
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(new Hotel
                {
                    Id = reader.GetInt32(0),
                    DestinationId = reader.GetInt32(1),
                    DestinationName = reader.GetString(2),
                    Name = reader.GetString(3),
                    Stars = reader.GetInt32(4),
                    Address = reader.GetString(5),
                    Description = reader.GetString(6)
                });
            }

            return items;
        }

        private static async Task<IReadOnlyList<Tour>> GetToursAsync(NpgsqlDataSource dataSource)
        {
            const string sql = """
                SELECT t.id, t.destination_id, t.hotel_id, d.name, d.country, d.city, h.name, h.stars,
                       t.title, t.description, t.price_per_person, t.start_date, t.end_date,
                       t.available_spots, t.meal_type, t.transport_type
                FROM tours t
                JOIN destinations d ON d.id = t.destination_id
                JOIN hotels h ON h.id = t.hotel_id
                ORDER BY t.start_date, t.id;
                """;
            var items = new List<Tour>();
            await using var command = dataSource.CreateCommand(sql);
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(ReadTour(reader));
            }

            return items;
        }

        private static async Task<IReadOnlyList<Client>> GetClientsAsync(NpgsqlDataSource dataSource)
        {
            const string sql = "SELECT id, full_name, phone, COALESCE(email, ''), COALESCE(passport_number, ''), created_at FROM clients ORDER BY full_name;";
            var items = new List<Client>();
            await using var command = dataSource.CreateCommand(sql);
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(new Client
                {
                    Id = reader.GetInt32(0),
                    FullName = reader.GetString(1),
                    Phone = reader.GetString(2),
                    Email = reader.GetString(3),
                    PassportNumber = reader.GetString(4),
                    CreatedAt = DateTime.SpecifyKind(reader.GetDateTime(5), DateTimeKind.Unspecified)
                });
            }

            return items;
        }

        private static async Task<IReadOnlyList<Booking>> GetBookingsAsync(NpgsqlDataSource dataSource)
        {
            const string sql = """
                SELECT b.id, b.tour_id, b.client_id, c.full_name, c.phone, COALESCE(c.passport_number, ''),
                       t.title, d.name, h.name, t.start_date, t.end_date,
                       b.people_count, b.total_price, COALESCE(SUM(p.amount), 0) AS paid_amount,
                       b.status, b.booked_at, COALESCE(b.notes, '')
                FROM bookings b
                JOIN clients c ON c.id = b.client_id
                JOIN tours t ON t.id = b.tour_id
                JOIN destinations d ON d.id = t.destination_id
                JOIN hotels h ON h.id = t.hotel_id
                LEFT JOIN payments p ON p.booking_id = b.id
                GROUP BY b.id, c.id, t.id, d.id, h.id
                ORDER BY b.booked_at DESC, b.id DESC;
                """;
            var items = new List<Booking>();
            await using var command = dataSource.CreateCommand(sql);
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(new Booking
                {
                    Id = reader.GetInt32(0),
                    TourId = reader.GetInt32(1),
                    ClientId = reader.GetInt32(2),
                    ClientName = reader.GetString(3),
                    ClientPhone = reader.GetString(4),
                    PassportNumber = reader.GetString(5),
                    TourTitle = reader.GetString(6),
                    DestinationName = reader.GetString(7),
                    HotelName = reader.GetString(8),
                    StartDate = DateTime.SpecifyKind(reader.GetDateTime(9), DateTimeKind.Unspecified),
                    EndDate = DateTime.SpecifyKind(reader.GetDateTime(10), DateTimeKind.Unspecified),
                    PeopleCount = reader.GetInt32(11),
                    TotalPrice = reader.GetDecimal(12),
                    PaidAmount = reader.GetDecimal(13),
                    Status = reader.GetString(14),
                    BookedAt = DateTime.SpecifyKind(reader.GetDateTime(15), DateTimeKind.Unspecified),
                    Notes = reader.GetString(16)
                });
            }

            return items;
        }

        private static async Task<IReadOnlyList<Payment>> GetPaymentsAsync(NpgsqlDataSource dataSource)
        {
            const string sql = "SELECT id, booking_id, paid_at, amount, method, COALESCE(comment, '') FROM payments ORDER BY paid_at DESC, id DESC;";
            var items = new List<Payment>();
            await using var command = dataSource.CreateCommand(sql);
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(new Payment
                {
                    Id = reader.GetInt32(0),
                    BookingId = reader.GetInt32(1),
                    PaidAt = DateTime.SpecifyKind(reader.GetDateTime(2), DateTimeKind.Unspecified),
                    Amount = reader.GetDecimal(3),
                    Method = reader.GetString(4),
                    Comment = reader.GetString(5)
                });
            }

            return items;
        }

        private static Tour ReadTour(NpgsqlDataReader reader)
        {
            return new Tour
            {
                Id = reader.GetInt32(0),
                DestinationId = reader.GetInt32(1),
                HotelId = reader.GetInt32(2),
                DestinationName = reader.GetString(3),
                Country = reader.GetString(4),
                City = reader.GetString(5),
                HotelName = reader.GetString(6),
                HotelStars = reader.GetInt32(7),
                Title = reader.GetString(8),
                Description = reader.GetString(9),
                PricePerPerson = reader.GetDecimal(10),
                StartDate = DateTime.SpecifyKind(reader.GetDateTime(11), DateTimeKind.Unspecified),
                EndDate = DateTime.SpecifyKind(reader.GetDateTime(12), DateTimeKind.Unspecified),
                AvailableSpots = reader.GetInt32(13),
                MealType = reader.GetString(14),
                TransportType = reader.GetString(15)
            };
        }

        private static object ToDbText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();
        }
    }
}
