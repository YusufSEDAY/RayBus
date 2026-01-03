namespace RayBus.Models.DTOs
{
    /// <summary>
    /// Rezervasyon için Data Transfer Object
    /// </summary>
    public class ReservationDTO
    {
        public int ReservationID { get; set; }
        public int UserID { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int TripID { get; set; }
        public int SeatID { get; set; }
        public string SeatNumber { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;
        public string FromCity { get; set; } = string.Empty;
        public string ToCity { get; set; } = string.Empty;
        public DateTime DepartureDate { get; set; }
        public TimeSpan DepartureTime { get; set; }
        public DateTime? ArrivalDate { get; set; }
        public TimeSpan? ArrivalTime { get; set; }
        public decimal Price { get; set; }
        public DateTime ReservationDate { get; set; }
        public string Status { get; set; } = string.Empty; // "Reserved", "Cancelled", "Completed"
        public string PaymentStatus { get; set; } = string.Empty; // "Pending", "Paid", "Refunded"
        public string? CancelReason { get; set; }
    }

    /// <summary>
    /// Yeni rezervasyon oluşturma isteği için DTO
    /// </summary>
    public class CreateReservationDTO
    {
        public int UserID { get; set; }
        public int TripID { get; set; }
        public int SeatID { get; set; }
        public decimal Price { get; set; }
        public string PaymentMethod { get; set; } = "Kredi Kartı"; // Varsayılan: "Kredi Kartı", "Havale", vb.
        /// <summary>
        /// İşlem tipi: 0 = Sadece Rezervasyon (PaymentStatus: Pending), 1 = Satın Alma (PaymentStatus: Paid)
        /// </summary>
        public byte IslemTipi { get; set; } = 1; // Varsayılan: Satın Alma
        public CardInfoDTO? CardInfo { get; set; } // Kredi kartı bilgileri (simülasyon için, sadece IslemTipi = 1 ise)
    }

    /// <summary>
    /// Ödeme tamamlama isteği için DTO
    /// </summary>
    public class CompletePaymentDTO
    {
        public int ReservationID { get; set; }
        public decimal Price { get; set; }
        public string PaymentMethod { get; set; } = "Kredi Kartı";
        public CardInfoDTO? CardInfo { get; set; } // Kredi kartı bilgileri (simülasyon için)
    }

    /// <summary>
    /// Kredi kartı bilgileri için DTO (simülasyon)
    /// </summary>
    public class CardInfoDTO
    {
        public string Last4Digits { get; set; } = string.Empty; // Son 4 hane
        public string CardHolder { get; set; } = string.Empty; // Kart sahibi adı
        public string ExpiryMonth { get; set; } = string.Empty; // Son kullanma ayı
        public string ExpiryYear { get; set; } = string.Empty; // Son kullanma yılı
        public string MaskedCardNumber { get; set; } = string.Empty; // Maskelenmiş kart numarası (gösterim için)
    }

    /// <summary>
    /// İptal nedeni DTO
    /// </summary>
    public class CancellationReasonDTO
    {
        public int ReasonID { get; set; }
        public string ReasonText { get; set; } = string.Empty;
    }

    /// <summary>
    /// Özel iptal nedeni ekleme DTO
    /// </summary>
    public class CreateCancellationReasonDTO
    {
        public string ReasonText { get; set; } = string.Empty;
    }
}


