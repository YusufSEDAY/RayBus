namespace RayBus.Models.Entities
{
    public class Trip
    {
        public int TripID { get; set; }
        public int VehicleID { get; set; }
        public int FromCityID { get; set; }
        public int ToCityID { get; set; }
        public int? DepartureTerminalID { get; set; }
        public int? ArrivalTerminalID { get; set; }
        public int? DepartureStationID { get; set; }
        public int? ArrivalStationID { get; set; }
        public DateTime DepartureDate { get; set; }
        public TimeSpan DepartureTime { get; set; }
        public DateTime? ArrivalDate { get; set; }
        public TimeSpan? ArrivalTime { get; set; }
        public decimal Price { get; set; } = 0.00m;
        public byte Status { get; set; } = 1; // 1=Active, 0=Cancelled
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public Vehicle? Vehicle { get; set; }
        public City? FromCity { get; set; }
        public City? ToCity { get; set; }
        public Terminal? DepartureTerminal { get; set; }
        public Terminal? ArrivalTerminal { get; set; }
        public Station? DepartureStation { get; set; }
        public Station? ArrivalStation { get; set; }
        public ICollection<TripSeat> TripSeats { get; set; } = new List<TripSeat>();
        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
        public ICollection<TripLog> TripLogs { get; set; } = new List<TripLog>();
    }
}

