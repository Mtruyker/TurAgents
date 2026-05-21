using System;

namespace TourAgency.Models
{
    public class Payment
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public DateTime PaidAt { get; set; }
        public decimal Amount { get; set; }
        public string Method { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
    }
}
