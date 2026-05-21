using System;

namespace TourAgency.Models
{
    public class Tour
    {
        public int Id { get; set; }
        public int DestinationId { get; set; }
        public int HotelId { get; set; }
        public string DestinationName { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string HotelName { get; set; } = string.Empty;
        public int HotelStars { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal PricePerPerson { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int Nights => Math.Max(0, (EndDate.Date - StartDate.Date).Days);
        public int AvailableSpots { get; set; }
        public string MealType { get; set; } = string.Empty;
        public string TransportType { get; set; } = string.Empty;
        public string DisplayName => $"{DestinationName}, {HotelName}, {StartDate:dd.MM.yyyy}";
    }
}
