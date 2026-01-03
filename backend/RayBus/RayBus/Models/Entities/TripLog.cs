namespace RayBus.Models.Entities
{
    public class TripLog
    {
        public int LogID { get; set; }
        public int TripID { get; set; }
        public string ColumnName { get; set; } = string.Empty;
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
        public int? ChangedByUserID { get; set; }
        public string? Action { get; set; } // İşlem tipi (örn: 'Created', 'Updated', 'Deleted')
        public DateTime? LogDate { get; set; } // Log tarihi
        public string? Description { get; set; } // Açıklama
        
        // Navigation properties
        public Trip? Trip { get; set; }
        public User? ChangedByUser { get; set; }
    }
}

