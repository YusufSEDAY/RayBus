namespace RayBus.Models.Entities
{
    /// <summary>
    /// Bildirim kuyruğu
    /// </summary>
    public class NotificationQueue
    {
        public int NotificationID { get; set; }
        public int UserID { get; set; }
        public string NotificationType { get; set; } = string.Empty; // 'Reservation', 'Payment', 'Cancellation', 'Reminder'
        public string NotificationMethod { get; set; } = "Email"; // 'Email', 'SMS', 'Both'
        public string? Subject { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending"; // 'Pending', 'Sent', 'Failed'
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? SentAt { get; set; }
        public int RetryCount { get; set; } = 0;
        public string? ErrorMessage { get; set; }
        public int? RelatedReservationID { get; set; }
        
        // Navigation properties
        public User? User { get; set; }
        public Reservation? RelatedReservation { get; set; }
    }
}

