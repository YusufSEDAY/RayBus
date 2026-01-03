namespace RayBus.Models.Entities
{
    public class ReservationLog
    {
        public int LogID { get; set; }
        public int ReservationID { get; set; }
        public string Action { get; set; } = string.Empty; // 'Created','Cancelled','Updated'
        public string? Details { get; set; }
        public DateTime LogDate { get; set; } = DateTime.UtcNow;
        public int? PerformedBy { get; set; }
        
        // Navigation properties
        public Reservation? Reservation { get; set; }
        public User? PerformedByUser { get; set; }
    }
}

