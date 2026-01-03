namespace RayBus.Models.DTOs
{
    /// <summary>
    /// Bilet detay DTO (PDF için)
    /// </summary>
    public class TicketDetailDTO
    {
        // Rezervasyon Bilgileri
        public int ReservationID { get; set; }
        public string? BiletNumarasi { get; set; }
        public DateTime RezervasyonTarihi { get; set; }
        public string RezervasyonDurumu { get; set; } = string.Empty;
        
        // Kullanıcı Bilgileri
        public int UserID { get; set; }
        public string KullaniciAdi { get; set; } = string.Empty;
        public string? KullaniciEmail { get; set; }
        public string? KullaniciTelefon { get; set; }
        
        // Sefer Bilgileri
        public int TripID { get; set; }
        public DateTime KalkisTarihi { get; set; }
        public TimeSpan KalkisSaati { get; set; }
        public DateTime? VarisTarihi { get; set; }
        public TimeSpan? VarisSaati { get; set; }
        public decimal SeferFiyati { get; set; }
        
        // Şehir Bilgileri
        public string KalkisSehri { get; set; } = string.Empty;
        public string VarisSehri { get; set; } = string.Empty;
        
        // Terminal/İstasyon Bilgileri
        public string? KalkisTerminali { get; set; }
        public string? VarisTerminali { get; set; }
        public string? KalkisIstasyonu { get; set; }
        public string? VarisIstasyonu { get; set; }
        
        // Araç Bilgileri
        public int VehicleID { get; set; }
        public string AracPlakasi { get; set; } = string.Empty;
        public string AracTipi { get; set; } = string.Empty;
        public string AracTipiTurkce { get; set; } = string.Empty;
        
        // Koltuk Bilgileri
        public int SeatID { get; set; }
        public string KoltukNumarasi { get; set; } = string.Empty;
        public bool KoltukDurumu { get; set; }
        
        // Ödeme Bilgileri
        public int? PaymentID { get; set; }
        public decimal OdenenTutar { get; set; }
        public DateTime? OdemeTarihi { get; set; }
        public string? OdemeYontemi { get; set; }
        public string? OdemeDurumu { get; set; }
        
        // Vagon Bilgileri (Tren için)
        public int? VagonNumarasi { get; set; }
        
        // Otobüs Bilgileri
        public string? OtobusModeli { get; set; }
        public string? KoltukDuzeni { get; set; }
        
        // Tren Bilgileri
        public string? TrenModeli { get; set; }
        
        // Ek Bilgiler
        public int? SeyahatSuresiSaat { get; set; }
    }
}

