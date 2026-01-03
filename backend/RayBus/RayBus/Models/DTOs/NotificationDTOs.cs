namespace RayBus.Models.DTOs
{
    /// <summary>
    /// Bildirim kuyruğu DTO
    /// </summary>
    public class NotificationQueueDTO
    {
        public int NotificationID { get; set; }
        public int UserID { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string NotificationType { get; set; } = string.Empty;
        public string NotificationMethod { get; set; } = string.Empty;
        public string? Subject { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? SentAt { get; set; }
        public int RetryCount { get; set; }
        public string? ErrorMessage { get; set; }
        public int? RelatedReservationID { get; set; }
    }

    /// <summary>
    /// Kullanıcı bildirim tercihleri DTO
    /// </summary>
    public class UserNotificationPreferencesDTO
    {
        public int PreferenceID { get; set; }
        public int UserID { get; set; }
        public bool EmailNotifications { get; set; } = true;
        public bool SMSNotifications { get; set; } = false;
        public bool ReservationNotifications { get; set; } = true;
        public bool PaymentNotifications { get; set; } = true;
        public bool CancellationNotifications { get; set; } = true;
        public bool ReminderNotifications { get; set; } = true;
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Bildirim gönderme isteği DTO
    /// </summary>
    public class SendNotificationDTO
    {
        public int UserID { get; set; }
        public string NotificationType { get; set; } = string.Empty; // 'Reservation', 'Payment', 'Cancellation', 'Reminder'
        public string NotificationMethod { get; set; } = "Email"; // 'Email', 'SMS', 'Both'
        public string Subject { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public int? RelatedReservationID { get; set; }
    }

    /// <summary>
    /// Bildirim durum güncelleme DTO
    /// </summary>
    public class UpdateNotificationStatusDTO
    {
        public int NotificationID { get; set; }
        public string Status { get; set; } = string.Empty; // 'Sent', 'Failed'
        public string? ErrorMessage { get; set; }
    }
}

