namespace RayBus.Models.DTOs
{
    /// <summary>
    /// Kullanıcı istatistikleri DTO
    /// </summary>
    public class UserStatisticsDTO
    {
        public int UserID { get; set; }
        public string? KullaniciAdi { get; set; }
        public string? KullaniciEmail { get; set; }
        public decimal ToplamHarcama { get; set; }
        public int ToplamSeyahatSayisi { get; set; }
        public int GelecekSeyahatSayisi { get; set; }
        public int GecmisSeyahatSayisi { get; set; }
        public decimal OrtalamaSeyahatFiyati { get; set; }
        public string? EnCokGidilenSehir { get; set; }
        public DateTime? SonSeyahatTarihi { get; set; }
        public int ToplamRezervasyonSayisi { get; set; }
        public int IptalEdilenRezervasyonSayisi { get; set; }
        public DateTime? KayitTarihi { get; set; }
    }

    /// <summary>
    /// Kullanıcı raporu - Genel istatistikler
    /// </summary>
    public class UserReportGeneralDTO
    {
        public string RaporTipi { get; set; } = string.Empty;
        public string? KullaniciAdi { get; set; }
        public decimal ToplamHarcama { get; set; }
        public int ToplamSeyahatSayisi { get; set; }
        public int GelecekSeyahatSayisi { get; set; }
        public int GecmisSeyahatSayisi { get; set; }
        public decimal OrtalamaSeyahatFiyati { get; set; }
        public string? EnCokGidilenSehir { get; set; }
        public DateTime? SonSeyahatTarihi { get; set; }
        public int ToplamRezervasyonSayisi { get; set; }
        public int IptalEdilenRezervasyonSayisi { get; set; }
    }

    /// <summary>
    /// Kullanıcı raporu - Son seyahatler
    /// </summary>
    public class UserReportTripDTO
    {
        public int ReservationID { get; set; }
        public DateTime SeferTarihi { get; set; }
        public TimeSpan SeferSaati { get; set; }
        public string KalkisSehri { get; set; } = string.Empty;
        public string VarisSehri { get; set; } = string.Empty;
        public decimal OdenenTutar { get; set; }
        public string RezervasyonDurumu { get; set; } = string.Empty;
        public DateTime RezervasyonTarihi { get; set; }
    }

    /// <summary>
    /// Kullanıcı raporu - Aylık harcama
    /// </summary>
    public class UserReportMonthlyDTO
    {
        public int Yil { get; set; }
        public int Ay { get; set; }
        public decimal AylikHarcama { get; set; }
        public int AylikSeyahatSayisi { get; set; }
    }
}

