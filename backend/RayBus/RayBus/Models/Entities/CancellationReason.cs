namespace RayBus.Models.Entities
{
    public class CancellationReason
    {
        public int ReasonID { get; set; }
        public string ReasonText { get; set; } = string.Empty;
        
        // Navigation properties
        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    }
}

