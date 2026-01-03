namespace RayBus.Models.Entities
{
    public class Vehicle
    {
        public int VehicleID { get; set; }
        public string VehicleType { get; set; } = string.Empty; // 'Bus' or 'Train'
        public string? PlateOrCode { get; set; }
        public int SeatCount { get; set; } = 0;
        public bool Active { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? CompanyID { get; set; } // Şirket ID (Foreign Key to Users)
        
        // Navigation properties
        public Bus? Bus { get; set; }
        public Train? Train { get; set; }
        public User? Company { get; set; } // Şirket (Company User)
        public ICollection<Seat> Seats { get; set; } = new List<Seat>();
        public ICollection<Trip> Trips { get; set; } = new List<Trip>();
    }
}

