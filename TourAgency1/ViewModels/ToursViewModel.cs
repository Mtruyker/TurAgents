using Avalonia.Collections;
using ReactiveUI;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using System.Windows.Input;
using TourAgency.Data;
using TourAgency.Models;

namespace TourAgency.ViewModels
{
    public class ToursViewModel : ReactiveObject
    {
        private readonly TourRepository _tourRepository;
        private AvaloniaList<Destination> _destinations = new();
        private AvaloniaList<Hotel> _hotels = new();
        private AvaloniaList<Tour> _tours = new();
        private AvaloniaList<Client> _clients = new();
        private AvaloniaList<Booking> _bookings = new();
        private AvaloniaList<Payment> _payments = new();
        private Tour? _selectedTour;
        private Client? _selectedClient;
        private Booking? _selectedBooking;
        private string _statusMessage = "Загрузка данных из PostgreSQL...";
        private bool _hasStatusMessage = true;
        private string _destinationFilter = string.Empty;
        private string _startFromText = string.Empty;
        private string _maxPriceFilter = string.Empty;
        private int _peopleCount = 1;
        private string _newClientName = string.Empty;
        private string _newClientPhone = string.Empty;
        private string _newClientEmail = string.Empty;
        private string _newClientPassport = string.Empty;
        private string _bookingNotes = string.Empty;
        private string _paymentAmount = string.Empty;
        private string _paymentMethod = "Карта";
        private string _paymentComment = string.Empty;
        private string _contractText = string.Empty;

        public ToursViewModel()
            : this(new TourRepository())
        {
        }

        public ToursViewModel(TourRepository tourRepository)
        {
            _tourRepository = tourRepository;
            LoadDataCommand = ReactiveCommand.CreateFromTask(LoadDataAsync);
            SearchToursCommand = ReactiveCommand.CreateFromTask(SearchToursAsync);
            ResetFiltersCommand = ReactiveCommand.CreateFromTask(ResetFiltersAsync);
            AddClientCommand = ReactiveCommand.CreateFromTask(AddClientAsync);
            CreateBookingCommand = ReactiveCommand.CreateFromTask(CreateBookingAsync);
            AddPaymentCommand = ReactiveCommand.CreateFromTask(AddPaymentAsync);
            GenerateContractCommand = ReactiveCommand.Create(GenerateContract);
            SaveContractCommand = ReactiveCommand.Create(SaveContract);
            _ = LoadDataAsync();
        }

        public AvaloniaList<Destination> Destinations
        {
            get => _destinations;
            set => this.RaiseAndSetIfChanged(ref _destinations, value);
        }

        public AvaloniaList<Hotel> Hotels
        {
            get => _hotels;
            set => this.RaiseAndSetIfChanged(ref _hotels, value);
        }

        public AvaloniaList<Tour> Tours
        {
            get => _tours;
            set => this.RaiseAndSetIfChanged(ref _tours, value);
        }

        public AvaloniaList<Client> Clients
        {
            get => _clients;
            set => this.RaiseAndSetIfChanged(ref _clients, value);
        }

        public AvaloniaList<Booking> Bookings
        {
            get => _bookings;
            set => this.RaiseAndSetIfChanged(ref _bookings, value);
        }

        public AvaloniaList<Payment> Payments
        {
            get => _payments;
            set => this.RaiseAndSetIfChanged(ref _payments, value);
        }

        public AvaloniaList<string> PaymentMethods { get; } = new()
        {
            "Карта",
            "Наличные",
            "Перевод",
            "Счет"
        };

        public Tour? SelectedTour
        {
            get => _selectedTour;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedTour, value);
                if (value is not null)
                {
                    PaymentAmount = (value.PricePerPerson * PeopleCount).ToString("0.##", CultureInfo.CurrentCulture);
                }
            }
        }

        public Client? SelectedClient
        {
            get => _selectedClient;
            set => this.RaiseAndSetIfChanged(ref _selectedClient, value);
        }

        public Booking? SelectedBooking
        {
            get => _selectedBooking;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedBooking, value);
                if (value is not null)
                {
                    PaymentAmount = Math.Max(0, value.Balance).ToString("0.##", CultureInfo.CurrentCulture);
                    GenerateContract();
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
        }

        public bool HasStatusMessage
        {
            get => _hasStatusMessage;
            set => this.RaiseAndSetIfChanged(ref _hasStatusMessage, value);
        }

        public string DestinationFilter
        {
            get => _destinationFilter;
            set => this.RaiseAndSetIfChanged(ref _destinationFilter, value);
        }

        public string StartFromText
        {
            get => _startFromText;
            set => this.RaiseAndSetIfChanged(ref _startFromText, value);
        }

        public string MaxPriceFilter
        {
            get => _maxPriceFilter;
            set => this.RaiseAndSetIfChanged(ref _maxPriceFilter, value);
        }

        public int PeopleCount
        {
            get => _peopleCount;
            set
            {
                this.RaiseAndSetIfChanged(ref _peopleCount, Math.Max(1, value));
                if (SelectedTour is not null)
                {
                    PaymentAmount = (SelectedTour.PricePerPerson * _peopleCount).ToString("0.##", CultureInfo.CurrentCulture);
                }
            }
        }

        public string NewClientName
        {
            get => _newClientName;
            set => this.RaiseAndSetIfChanged(ref _newClientName, value);
        }

        public string NewClientPhone
        {
            get => _newClientPhone;
            set => this.RaiseAndSetIfChanged(ref _newClientPhone, value);
        }

        public string NewClientEmail
        {
            get => _newClientEmail;
            set => this.RaiseAndSetIfChanged(ref _newClientEmail, value);
        }

        public string NewClientPassport
        {
            get => _newClientPassport;
            set => this.RaiseAndSetIfChanged(ref _newClientPassport, value);
        }

        public string BookingNotes
        {
            get => _bookingNotes;
            set => this.RaiseAndSetIfChanged(ref _bookingNotes, value);
        }

        public string PaymentAmount
        {
            get => _paymentAmount;
            set => this.RaiseAndSetIfChanged(ref _paymentAmount, value);
        }

        public string PaymentMethod
        {
            get => _paymentMethod;
            set => this.RaiseAndSetIfChanged(ref _paymentMethod, value);
        }

        public string PaymentComment
        {
            get => _paymentComment;
            set => this.RaiseAndSetIfChanged(ref _paymentComment, value);
        }

        public string ContractText
        {
            get => _contractText;
            set => this.RaiseAndSetIfChanged(ref _contractText, value);
        }

        public ICommand LoadDataCommand { get; }
        public ICommand SearchToursCommand { get; }
        public ICommand ResetFiltersCommand { get; }
        public ICommand AddClientCommand { get; }
        public ICommand CreateBookingCommand { get; }
        public ICommand AddPaymentCommand { get; }
        public ICommand GenerateContractCommand { get; }
        public ICommand SaveContractCommand { get; }

        private async Task LoadDataAsync()
        {
            try
            {
                var data = await _tourRepository.GetAgencyDataAsync();
                Destinations = new AvaloniaList<Destination>(data.Destinations);
                Hotels = new AvaloniaList<Hotel>(data.Hotels);
                Tours = new AvaloniaList<Tour>(data.Tours);
                Clients = new AvaloniaList<Client>(data.Clients);
                Bookings = new AvaloniaList<Booking>(data.Bookings);
                Payments = new AvaloniaList<Payment>(data.Payments);
                SelectedTour ??= Tours.Count > 0 ? Tours[0] : null;
                SelectedClient ??= Clients.Count > 0 ? Clients[0] : null;
                SelectedBooking = Bookings.Count > 0 ? Bookings[0] : null;
                SetStatus($"Готово: направлений {Destinations.Count}, отелей {Hotels.Count}, туров {Tours.Count}, заявок {Bookings.Count}.", true);
            }
            catch (Exception exception)
            {
                SetStatus($"Не удалось загрузить данные из PostgreSQL: {exception.Message}", true);
            }
        }

        private async Task SearchToursAsync()
        {
            try
            {
                var maxPrice = ParseNullableDecimal(MaxPriceFilter, "максимальная цена");
                var startFrom = ParseNullableDate(StartFromText, "дата начала");
                var tours = await _tourRepository.SearchToursAsync(DestinationFilter, startFrom, maxPrice, PeopleCount);
                Tours = new AvaloniaList<Tour>(tours);
                SelectedTour = Tours.Count > 0 ? Tours[0] : null;
                SetStatus(Tours.Count == 0 ? "По заданным параметрам путевки не найдены." : $"Найдено путевок: {Tours.Count}.", true);
            }
            catch (Exception exception)
            {
                SetStatus($"Ошибка подбора путевок: {exception.Message}", true);
            }
        }

        private async Task ResetFiltersAsync()
        {
            DestinationFilter = string.Empty;
            StartFromText = string.Empty;
            MaxPriceFilter = string.Empty;
            PeopleCount = 1;
            await LoadDataAsync();
        }

        private async Task AddClientAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(NewClientName) || string.IsNullOrWhiteSpace(NewClientPhone))
                {
                    SetStatus("Укажите ФИО и телефон клиента.", true);
                    return;
                }

                await _tourRepository.AddClientAsync(new Client
                {
                    FullName = NewClientName,
                    Phone = NewClientPhone,
                    Email = NewClientEmail,
                    PassportNumber = NewClientPassport
                });

                NewClientName = string.Empty;
                NewClientPhone = string.Empty;
                NewClientEmail = string.Empty;
                NewClientPassport = string.Empty;
                await LoadDataAsync();
                SetStatus("Клиент добавлен.", true);
            }
            catch (Exception exception)
            {
                SetStatus($"Не удалось добавить клиента: {exception.Message}", true);
            }
        }

        private async Task CreateBookingAsync()
        {
            try
            {
                if (SelectedTour is null || SelectedClient is null)
                {
                    SetStatus("Выберите тур и клиента для оформления заявки.", true);
                    return;
                }

                var bookingId = await _tourRepository.CreateBookingAsync(SelectedTour.Id, SelectedClient.Id, PeopleCount, BookingNotes);
                BookingNotes = string.Empty;
                await LoadDataAsync();
                SelectedBooking = FindBooking(bookingId);
                SetStatus($"Заявка №{bookingId} оформлена. Стоимость рассчитана автоматически.", true);
            }
            catch (Exception exception)
            {
                SetStatus($"Не удалось оформить заявку: {exception.Message}", true);
            }
        }

        private async Task AddPaymentAsync()
        {
            try
            {
                if (SelectedBooking is null)
                {
                    SetStatus("Выберите заявку для оплаты.", true);
                    return;
                }

                var amount = ParseRequiredDecimal(PaymentAmount, "сумма оплаты");
                if (amount <= 0)
                {
                    SetStatus("Сумма оплаты должна быть больше нуля.", true);
                    return;
                }

                var bookingId = SelectedBooking.Id;
                await _tourRepository.AddPaymentAsync(bookingId, amount, PaymentMethod, PaymentComment);
                PaymentComment = string.Empty;
                await LoadDataAsync();
                SelectedBooking = FindBooking(bookingId);
                SetStatus("Оплата добавлена, статус заявки обновлен.", true);
            }
            catch (Exception exception)
            {
                SetStatus($"Не удалось добавить оплату: {exception.Message}", true);
            }
        }

        private void GenerateContract()
        {
            if (SelectedBooking is null)
            {
                ContractText = "Выберите заявку для формирования договора.";
                return;
            }

            var booking = SelectedBooking;
            ContractText = $"""
                ДОГОВОР №{booking.Id} НА РЕАЛИЗАЦИЮ ТУРИСТСКОГО ПРОДУКТА

                Дата формирования: {DateTime.Now:dd.MM.yyyy}

                Туристическое агентство оформляет для клиента {booking.ClientName}
                паспорт: {BlankIfEmpty(booking.PassportNumber)}, телефон: {booking.ClientPhone}
                путевку по направлению: {booking.DestinationName}.

                Тур: {booking.TourTitle}
                Отель: {booking.HotelName}
                Даты поездки: {booking.StartDate:dd.MM.yyyy} - {booking.EndDate:dd.MM.yyyy}
                Количество туристов: {booking.PeopleCount}

                Полная стоимость тура: {booking.TotalPrice:N2} руб.
                Оплачено: {booking.PaidAmount:N2} руб.
                Остаток к оплате: {booking.Balance:N2} руб.
                Статус оформления: {TranslateStatus(booking.Status)}

                Примечания: {BlankIfEmpty(booking.Notes)}

                Подписи сторон:
                Агент ______________________
                Клиент _____________________
                """;
        }

        private void SaveContract()
        {
            try
            {
                if (SelectedBooking is null)
                {
                    SetStatus("Выберите заявку для сохранения договора.", true);
                    return;
                }

                GenerateContract();
                var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "TourAgencyContracts");
                Directory.CreateDirectory(directory);
                var filePath = Path.Combine(directory, $"contract_{SelectedBooking.Id}.html");
                var encoded = HtmlEncoder.Default.Encode(ContractText).Replace("\n", "<br>");
                File.WriteAllText(filePath, $"<html><head><meta charset=\"utf-8\"><title>Договор №{SelectedBooking.Id}</title></head><body style=\"font-family:Arial,sans-serif;font-size:14pt;line-height:1.45\">{encoded}<script>window.print()</script></body></html>", Encoding.UTF8);
                Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
                SetStatus($"Договор сохранен: {filePath}", true);
            }
            catch (Exception exception)
            {
                SetStatus($"Не удалось сохранить договор: {exception.Message}", true);
            }
        }

        private Booking? FindBooking(int id)
        {
            foreach (var booking in Bookings)
            {
                if (booking.Id == id)
                {
                    return booking;
                }
            }

            return null;
        }

        private void SetStatus(string message, bool visible)
        {
            StatusMessage = message;
            HasStatusMessage = visible;
        }

        private static decimal? ParseNullableDecimal(string value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return ParseRequiredDecimal(value, fieldName);
        }

        private static DateTime? ParseNullableDate(string value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            string[] formats = ["dd.MM.yyyy", "d.M.yyyy", "yyyy-MM-dd"];
            if (DateTime.TryParseExact(value.Trim(), formats, CultureInfo.CurrentCulture, DateTimeStyles.None, out var date)
                || DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.None, out date))
            {
                return date.Date;
            }

            throw new FormatException($"Некорректное значение поля \"{fieldName}\". Используйте формат ДД.ММ.ГГГГ.");
        }

        private static decimal ParseRequiredDecimal(string value, string fieldName)
        {
            if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out var currentCultureValue))
            {
                return currentCultureValue;
            }

            if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var invariantValue))
            {
                return invariantValue;
            }

            throw new FormatException($"Некорректное значение поля \"{fieldName}\".");
        }

        private static string TranslateStatus(string status)
        {
            return status switch
            {
                "new" => "Новая",
                "confirmed" => "Подтверждена",
                "paid" => "Оплачена",
                "cancelled" => "Отменена",
                _ => status
            };
        }

        private static string BlankIfEmpty(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "-" : value;
        }
    }
}
