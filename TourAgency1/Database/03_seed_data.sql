INSERT INTO clients (id, full_name, phone, email, passport_number)
VALUES
    (1, 'Иванов Иван Петрович', '+79001234567', 'ivanov@example.com', '4512 123456'),
    (2, 'Петрова Анна Сергеевна', '+79007654321', 'petrova@example.com', '4513 654321'),
    (3, 'Смирнов Олег Андреевич', '+79005553322', 'smirnov@example.com', '4514 112233');

INSERT INTO destinations (id, country, city, name, description)
VALUES
    (1, 'Россия', 'Сочи', 'Сочи, Россия', 'Пляжный отдых на Черном море'),
    (2, 'Турция', 'Стамбул', 'Стамбул, Турция', 'Экскурсионная поездка по историческому центру'),
    (3, 'Армения', 'Ереван', 'Ереван, Армения', 'Гастрономический и исторический тур'),
    (4, 'Россия', 'Калининград', 'Калининград, Россия', 'Балтийские каникулы и экскурсии');

INSERT INTO hotels (id, destination_id, name, stars, address, description)
VALUES
    (1, 1, 'Морской бриз', 4, 'Сочи, Курортный проспект, 15', 'Отель у моря с бассейном'),
    (2, 2, 'Golden Horn View', 4, 'Istanbul, Fatih, 22', 'Отель рядом с историческим центром'),
    (3, 3, 'Cascade Boutique', 5, 'Ереван, ул. Абовяна, 8', 'Бутик-отель в центре города'),
    (4, 4, 'Балтика Парк', 3, 'Калининград, ул. Озерная, 4', 'Уютный городской отель');

INSERT INTO tours (id, destination_id, hotel_id, title, description, price_per_person, start_date, end_date, available_spots, meal_type, transport_type)
VALUES
    (1, 1, 1, 'Черноморская неделя', 'Пляжный отдых, трансфер и обзорная экскурсия', 25000.00, DATE '2026-06-01', DATE '2026-06-08', 15, 'Завтраки', 'Ж/д'),
    (2, 2, 2, 'Стамбул на двоих берегах', 'Айя-София, Босфор и дворцы османского периода', 35000.00, DATE '2026-06-10', DATE '2026-06-17', 8, 'Завтраки', 'Авиа'),
    (3, 3, 3, 'Вкус Армении', 'Дегустации, монастыри и прогулки по Еревану', 42000.00, DATE '2026-07-05', DATE '2026-07-12', 12, 'Полупансион', 'Авиа'),
    (4, 4, 4, 'Балтийские каникулы', 'Калининград, Куршская коса и замки региона', 30000.00, DATE '2026-08-03', DATE '2026-08-09', 20, 'Завтраки', 'Автобус');

INSERT INTO bookings (id, tour_id, client_id, people_count, total_price, status, booked_at, notes)
VALUES
    (1, 1, 1, 2, 50000.00, 'confirmed', TIMESTAMP '2026-05-15 10:00:00', 'Номер с видом на море'),
    (2, 2, 2, 1, 35000.00, 'paid', TIMESTAMP '2026-05-16 12:30:00', 'Нужна страховка'),
    (3, 3, 3, 3, 126000.00, 'new', TIMESTAMP '2026-05-17 09:15:00', 'Семейное размещение');

INSERT INTO payments (id, booking_id, paid_at, amount, method, comment)
VALUES
    (1, 1, TIMESTAMP '2026-05-15 10:30:00', 15000.00, 'Карта', 'Предоплата'),
    (2, 2, TIMESTAMP '2026-05-16 13:00:00', 35000.00, 'Перевод', 'Полная оплата');

SELECT setval(pg_get_serial_sequence('clients', 'id'), COALESCE((SELECT max(id) FROM clients), 1), true);
SELECT setval(pg_get_serial_sequence('destinations', 'id'), COALESCE((SELECT max(id) FROM destinations), 1), true);
SELECT setval(pg_get_serial_sequence('hotels', 'id'), COALESCE((SELECT max(id) FROM hotels), 1), true);
SELECT setval(pg_get_serial_sequence('tours', 'id'), COALESCE((SELECT max(id) FROM tours), 1), true);
SELECT setval(pg_get_serial_sequence('bookings', 'id'), COALESCE((SELECT max(id) FROM bookings), 1), true);
SELECT setval(pg_get_serial_sequence('payments', 'id'), COALESCE((SELECT max(id) FROM payments), 1), true);
