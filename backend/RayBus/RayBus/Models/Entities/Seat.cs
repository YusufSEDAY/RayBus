namespace RayBus.Models.Entities
{
    public class Seat
    {
        public int SeatID { get; set; }
        public int VehicleID { get; set; }
        public int? WagonID { get; set; }
        public string SeatNo { get; set; } = string.Empty;
        public string? SeatPosition { get; set; }
        public bool IsActive { get; set; } = true;
        
        // Navigation properties
        public Vehicle? Vehicle { get; set; }
        public Wagon? Wagon { get; set; }
        public ICollection<TripSeat> TripSeats { get; set; } = new List<TripSeat>();
    }
}

