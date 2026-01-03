namespace RayBus.Models.Entities
{
    /// <summary>
    /// Otomatik iptal log kayıtları
    /// </summary>
    public class AutoCancellationLog
    {
        public int LogID { get; set; }
        public int ReservationID { get; set; }
        public int UserID { get; set; }
        public DateTime CancelledAt { get; set; } = DateTime.UtcNow;
        public string Reason { get; set; } = string.Empty;
        public DateTime OriginalReservationDate { get; set; }
        public int TimeoutMinutes { get; set; } = 15;
        
        // Navigation properties
        public Reservation? Reservation { get; set; }
        public User? User { get; set; }
    }
}

