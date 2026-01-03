namespace RayBus.Models.DTOs
{
    /// <summary>
    /// Sefer detayı için DTO
    /// </summary>
    public class TripDetailDTO
    {
        public int TripID { get; set; }
        public string VehicleCode { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;
        public string? VehicleModel { get; set; }
        public string FromCity { get; set; } = string.Empty;
        public string ToCity { get; set; } = string.Empty;
        public string? DepartureTerminal { get; set; }
        public string? ArrivalTerminal { get; set; }
        public string? DepartureStation { get; set; }
        public string? ArrivalStation { get; set; }
        public DateTime DepartureDate { get; set; }
        public TimeSpan DepartureTime { get; set; }
        public DateTime? ArrivalDate { get; set; }
        public TimeSpan? ArrivalTime { get; set; }
        public decimal Price { get; set; }
        public int TotalSeats { get; set; }
        public int AvailableSeats { get; set; }
        public string? LayoutType { get; set; } // Otobüs koltuk düzeni (2+1, 2+2)
        public List<SeatInfoDTO> Seats { get; set; } = new();
    }

    /// <summary>
    /// Koltuk bilgisi için DTO
    /// </summary>
    public class SeatInfoDTO
    {
        public int SeatID { get; set; }
        public int TripSeatID { get; set; }
        public string SeatNo { get; set; } = string.Empty;
        public string? SeatPosition { get; set; }
        public int? WagonNo { get; set; }
        public bool IsReserved { get; set; }
        public bool IsActive { get; set; }
        public string? PaymentStatus { get; set; } // 'Pending', 'Paid', 'Refunded' veya NULL
    }
}

