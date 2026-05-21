# PostgreSQL

Приложение по умолчанию подключается к базе:

```text
Host=10.164.203.35;Port=5432;Database=tour_agency;Username=postgres;Password=12345678
```

Строку подключения можно переопределить переменной окружения `TOUR_AGENCY_CONNECTION_STRING`.

Скрипты выполняются по порядку:

```powershell
psql -U postgres -f .\Database\01_create_database.sql
psql -U postgres -d tour_agency -f .\Database\02_create_tables.sql
psql -U postgres -d tour_agency -f .\Database\03_seed_data.sql
```

`02_create_tables.sql` пересоздает таблицы учебной базы: клиенты, направления, отели, туры, заявки и оплаты. Если в базе уже есть важные данные, сделайте резервную копию перед запуском.
