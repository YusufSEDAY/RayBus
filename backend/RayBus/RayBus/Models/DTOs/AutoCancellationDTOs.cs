namespace RayBus.Models.DTOs
{
    /// <summary>
    /// Otomatik iptal log DTO
    /// </summary>
    public class AutoCancellationLogDTO
    {
        public int LogID { get; set; }
        public int ReservationID { get; set; }
        public int UserID { get; set; }
        public string UserName { get; set; } = string.Empty;
        public DateTime CancelledAt { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime OriginalReservationDate { get; set; }
        public int TimeoutMinutes { get; set; }
    }

    /// <summary>
    /// Otomatik iptal sonuç DTO
    /// </summary>
    public class AutoCancellationResultDTO
    {
        public int IptalEdilenSayisi { get; set; }
        public string Durum { get; set; } = string.Empty;
        public string IslemTarihi { get; set; } = string.Empty;
    }

    /// <summary>
    /// Otomatik iptal ayarları DTO
    /// </summary>
    public class AutoCancellationSettingsDTO
    {
        public int TimeoutMinutes { get; set; } = 15;
        public string Durum { get; set; } = string.Empty;
        public string Aciklama { get; set; } = string.Empty;
    }
}

