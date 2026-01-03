namespace RayBus.Models.DTOs
{
    public class CompanyTripDTO
    {
        public int TripID { get; set; }
        public int VehicleID { get; set; }
        public string VehicleType { get; set; } = string.Empty;
        public int FromCityID { get; set; }
        public string FromCity { get; set; } = string.Empty;
        public int ToCityID { get; set; }
        public string ToCity { get; set; } = string.Empty;
        public int? DepartureTerminalID { get; set; }
        public string? DepartureTerminal { get; set; }
        public int? ArrivalTerminalID { get; set; }
        public string? ArrivalTerminal { get; set; }
        public int? DepartureStationID { get; set; }
        public string? DepartureStation { get; set; }
        public int? ArrivalStationID { get; set; }
        public string? ArrivalStation { get; set; }
        public DateTime DepartureDate { get; set; }
        public TimeSpan DepartureTime { get; set; }
        public DateTime? ArrivalDate { get; set; }
        public TimeSpan? ArrivalTime { get; set; }
        public decimal Price { get; set; }
        public byte Status { get; set; }
        
        // SP'den gelen ek bilgiler
        public string? AracPlaka { get; set; }
        public string? Guzergah { get; set; }
        public DateTime? Tarih { get; set; }
        public TimeSpan? Saat { get; set; }
        public string? Durum { get; set; } // "Aktif" veya "İptal"
        public int DoluKoltukSayisi { get; set; }
        public int ToplamKoltuk { get; set; }
    }

    public class CompanyStatsDTO
    {
        // Şirket Bilgileri
        public int SirketID { get; set; }
        public string? SirketAdi { get; set; }
        public string? SirketEmail { get; set; }
        
        // Sefer İstatistikleri
        public int TotalTrips { get; set; }
        public int ActiveTrips { get; set; }
        public int IptalSefer { get; set; }
        
        // Rezervasyon İstatistikleri
        public int TotalReservations { get; set; }
        public int ActiveReservations { get; set; }
        public int IptalRezervasyon { get; set; }
        
        // Gelir İstatistikleri
        public decimal ToplamGelir { get; set; }
        public decimal SonBirAyGelir { get; set; }
        
        // Araç İstatistikleri
        public int ToplamArac { get; set; }
        public int OtobusSayisi { get; set; }
        public int TrenSayisi { get; set; }
        
        // Performans Metrikleri
        public decimal OrtalamaDoluKoltukOrani { get; set; }
        public int BuAyEklenenSefer { get; set; }
        
        // Tarih
        public DateTime? SonGuncellemeTarihi { get; set; }
    }

    public class VehicleDTO
    {
        public int VehicleID { get; set; }
        public string VehicleType { get; set; } = string.Empty;
        public string? PlateOrCode { get; set; }
        public int SeatCount { get; set; }
        public bool Active { get; set; }
    }

    public class CancelCompanyTripDTO
    {
        public string? IptalNedeni { get; set; } // İptal nedeni (opsiyonel)
    }
}

