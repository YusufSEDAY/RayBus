namespace RayBus.Models.Entities
{
    public class Payment
    {
        public int PaymentID { get; set; }
        public int ReservationID { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
        public string? PaymentMethod { get; set; } // 'Card', 'VirtualPOS', etc.
        public string Status { get; set; } = "Completed"; // 'Completed','Failed','Refunded'
        public string? TransactionRef { get; set; }
        
        // Navigation properties
        public Reservation? Reservation { get; set; }
        public ICollection<PaymentLog> PaymentLogs { get; set; } = new List<PaymentLog>();
    }
}

