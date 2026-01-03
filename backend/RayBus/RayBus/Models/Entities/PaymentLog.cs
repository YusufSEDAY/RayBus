namespace RayBus.Models.Entities
{
    /// <summary>
    /// Ödeme işlemlerinin log kayıtları için entity
    /// </summary>
    public class PaymentLog
    {
        public int LogID { get; set; }
        public int PaymentID { get; set; }
        public string Action { get; set; } = string.Empty; // 'Olusturuldu', 'DurumDegisikligi', vb.
        public string? OldStatus { get; set; } // Eski durum
        public string? NewStatus { get; set; } // Yeni durum
        public DateTime LogDate { get; set; } = DateTime.UtcNow;
        public string? Description { get; set; } // Ek açıklama
        
        // Navigation property
        public Payment? Payment { get; set; }
    }
}

