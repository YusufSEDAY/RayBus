namespace RayBus.Models.DTOs
{
    /// <summary>
    /// Ödeme log kayıtları için DTO
    /// </summary>
    public class PaymentLogDTO
    {
        public int LogID { get; set; }
        public int PaymentID { get; set; }
        public string Action { get; set; } = string.Empty; // 'Olusturuldu', 'DurumDegisikligi', vb.
        public string? OldStatus { get; set; } // Eski durum
        public string? NewStatus { get; set; } // Yeni durum
        public DateTime LogDate { get; set; }
        public string? Description { get; set; } // Ek açıklama
    }
}

