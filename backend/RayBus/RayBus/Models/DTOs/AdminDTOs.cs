namespace RayBus.Models.DTOs
{
    public class AdminUserDTO
    {
        public int UserID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public byte Status { get; set; }
        public byte Durum { get; set; } // SP'den gelen Durum (Status ile aynı)
        public DateTime CreatedAt { get; set; }
        public DateTime KayitTarihi { get; set; } // SP'den gelen KayitTarihi (CreatedAt ile aynı)
        public decimal ToplamHarcama { get; set; } // SP'den gelen toplam harcama
    }

    public class UpdateUserStatusDTO
    {
        public byte Status { get; set; }
        public string? Sebep { get; set; } // Ban nedeni (opsiyonel)
    }

    public class UpdateUserDTO
    {
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
    }

    public class UpdateVehicleDTO
    {
        public string? PlateOrCode { get; set; }
        public string? VehicleType { get; set; }
        public bool? Active { get; set; }
        public int? CompanyID { get; set; }
    }

    public class UpdateTripDTO
    {
        public int? FromCityID { get; set; }
        public int? ToCityID { get; set; }
        public int? VehicleID { get; set; }
        public int? DepartureTerminalID { get; set; }
        public int? ArrivalTerminalID { get; set; }
        public int? DepartureStationID { get; set; }
        public int? ArrivalStationID { get; set; }
        public DateTime? DepartureDate { get; set; }
        public TimeSpan? DepartureTime { get; set; }
        public DateTime? ArrivalDate { get; set; }
        public TimeSpan? ArrivalTime { get; set; }
        public decimal? Price { get; set; }
    }

    public class AdminReservationDTO
    {
        public int ReservationID { get; set; }
        public int UserID { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int TripID { get; set; }
        public string TripRoute { get; set; } = string.Empty;
        public int SeatID { get; set; }
        public string SeatNo { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public DateTime ReservationDate { get; set; }
    }

    public class UpdateReservationStatusDTO
    {
        public string Status { get; set; } = string.Empty;
    }

    public class CancelReservationDTO
    {
        public int? CancelReasonID { get; set; }
        public string? Reason { get; set; }
    }

    public class AdminTripDTO
    {
        public int TripID { get; set; }
        public int VehicleID { get; set; }
        public string VehicleType { get; set; } = string.Empty;
        public int? VehicleCompanyID { get; set; }
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
    }

    public class UpdateTripStatusDTO
    {
        public byte Status { get; set; }
    }

    public class CancelTripDTO
    {
        public string? IptalNedeni { get; set; } // İptal nedeni (opsiyonel)
    }

    public class DashboardStatsDTO
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int TotalReservations { get; set; }
        public int ActiveReservations { get; set; }
        public int TotalTrips { get; set; }
        public int ActiveTrips { get; set; }
        public decimal TotalRevenue { get; set; }
        
        // View'dan gelen ek istatistikler
        public int ToplamAktifUye { get; set; } // View'dan: ToplamAktifUye
        public int GelecekSeferler { get; set; } // View'dan: GelecekSeferler
        public decimal GunlukCiro { get; set; } // View'dan: GunlukCiro
        public int ToplamSatis { get; set; } // View'dan: ToplamSatis
        public int SonIslemLoglari { get; set; } // View'dan: SonIslemLoglari
    }

    public class CreateVehicleDTO
    {
        public string PlakaNo { get; set; } = string.Empty;
        public string AracTipi { get; set; } = string.Empty; // 'Bus' veya 'Train'
        public int ToplamKoltuk { get; set; }
        public int? SirketID { get; set; } // Opsiyonel: Şirket ID (NULL ise admin ekliyor)
    }

    public class DailyFinancialReportDTO
    {
        public DateTime IslemTarihi { get; set; }
        public string? OdemeYontemi { get; set; }
        public int ToplamSatisAdedi { get; set; }
        public decimal ToplamCiro { get; set; }
    }

    /// <summary>
    /// Güzergah Ciro Raporu DTO (vw_Guzergah_Ciro_Raporu view'ından)
    /// </summary>
    public class GuzergahCiroRaporuDTO
    {
        public string Guzergah { get; set; } = string.Empty; // "İstanbul - Ankara"
        public string AracTipi { get; set; } = string.Empty; // "Bus" veya "Train"
        public int ToplamSatisAdedi { get; set; }
        public decimal ToplamCiro { get; set; }
        public decimal? OrtalamaBiletFiyati { get; set; }
    }

    /// <summary>
    /// Sefer Detayları DTO (vw_Sefer_Detaylari view'ından)
    /// </summary>
    public class SeferDetaylariDTO
    {
        public int TripID { get; set; }
        public DateTime DepartureDate { get; set; }
        public TimeSpan DepartureTime { get; set; }
        public string Nereden { get; set; } = string.Empty;
        public string Nereye { get; set; } = string.Empty;
        public string AracTipi { get; set; } = string.Empty;
        public string PlakaNo { get; set; } = string.Empty;
        public decimal BiletFiyati { get; set; }
        public int ToplamKoltuk { get; set; }
        public int SatilanKoltuk { get; set; }
        public string SeferDurumu { get; set; } = string.Empty; // "Aktif" veya "İptal"
        public int BosKoltuk => ToplamKoltuk - SatilanKoltuk;
        public decimal DolulukOrani => ToplamKoltuk > 0 ? (decimal)SatilanKoltuk / ToplamKoltuk * 100 : 0;
    }
}

