namespace RayBus.Models.Entities
{
    public class Bus
    {
        public int BusID { get; set; }
        public string? BusModel { get; set; }
        public string? LayoutType { get; set; }
        
        // Navigation property
        public Vehicle? Vehicle { get; set; }
    }
}

