namespace RayBus.Models.Entities
{
    public class TripSeat
    {
        public int TripSeatID { get; set; }
        public int TripID { get; set; }
        public int SeatID { get; set; }
        public bool IsReserved { get; set; } = false;
        public DateTime? ReservedAt { get; set; }
        
        // Navigation properties
        public Trip? Trip { get; set; }
        public Seat? Seat { get; set; }
        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    }
}

