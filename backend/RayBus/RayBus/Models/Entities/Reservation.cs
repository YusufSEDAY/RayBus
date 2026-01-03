namespace RayBus.Models.Entities
{
    public class Reservation
    {
        public int ReservationID { get; set; }
        public int TripID { get; set; }
        public int SeatID { get; set; }
        public int UserID { get; set; }
        public DateTime ReservationDate { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Reserved"; // "Reserved", "Cancelled", "Completed"
        public string PaymentStatus { get; set; } = "Pending"; // "Pending", "Paid", "Refunded"
        public int? CancelReasonID { get; set; }
        public string? TicketNumber { get; set; } // Bilet numarası (PDF için)
        
        // Navigation properties
        public TripSeat? TripSeat { get; set; }
        public User? User { get; set; }
        public CancellationReason? CancelReason { get; set; }
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public ICollection<ReservationLog> ReservationLogs { get; set; } = new List<ReservationLog>();
    }
}

