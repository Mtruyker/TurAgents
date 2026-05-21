using System;

namespace TourAgency.Data
{
    public static class DatabaseSettings
    {
        private const string DefaultConnectionString =
            "Host=10.164.203.35;Port=5432;Database=tour_agency;Username=postgres;Password=12345678";

        public static string ConnectionString =>
            Environment.GetEnvironmentVariable("TOUR_AGENCY_CONNECTION_STRING")
            ?? DefaultConnectionString;
    }
}
