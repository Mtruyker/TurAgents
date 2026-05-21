using System.Collections.Generic;

namespace TourAgency.Models
{
    public class AgencyData
    {
        public IReadOnlyList<Destination> Destinations { get; set; } = [];
        public IReadOnlyList<Hotel> Hotels { get; set; } = [];
        public IReadOnlyList<Tour> Tours { get; set; } = [];
        public IReadOnlyList<Client> Clients { get; set; } = [];
        public IReadOnlyList<Booking> Bookings { get; set; } = [];
        public IReadOnlyList<Payment> Payments { get; set; } = [];
    }
}
