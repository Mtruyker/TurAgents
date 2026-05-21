using System;

namespace TourAgency.Models
{
    public class Booking
    {
        public int Id { get; set; }
        public int TourId { get; set; }
        public int ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string ClientPhone { get; set; } = string.Empty;
        public string PassportNumber { get; set; } = string.Empty;
        public string TourTitle { get; set; } = string.Empty;
        public string DestinationName { get; set; } = string.Empty;
        public string HotelName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int PeopleCount { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal Balance => TotalPrice - PaidAmount;
        public string Status { get; set; } = string.Empty;
        public DateTime BookedAt { get; set; }
        public string Notes { get; set; } = string.Empty;
    }
}
