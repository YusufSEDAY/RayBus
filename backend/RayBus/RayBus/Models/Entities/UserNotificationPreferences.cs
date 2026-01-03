namespace RayBus.Models.Entities
{
    /// <summary>
    /// Kullanıcı bildirim tercihleri
    /// </summary>
    public class UserNotificationPreferences
    {
        public int PreferenceID { get; set; }
        public int UserID { get; set; }
        public bool EmailNotifications { get; set; } = true;
        public bool SMSNotifications { get; set; } = false;
        public bool ReservationNotifications { get; set; } = true;
        public bool PaymentNotifications { get; set; } = true;
        public bool CancellationNotifications { get; set; } = true;
        public bool ReminderNotifications { get; set; } = true;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation property
        public User? User { get; set; }
    }
}

