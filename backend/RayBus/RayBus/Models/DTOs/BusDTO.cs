namespace RayBus.Models.DTOs
{
    /// <summary>
    /// Otobüs seferi için Data Transfer Object
    /// </summary>
    public class BusDTO
    {
        public int TripID { get; set; }
        public string VehicleCode { get; set; } = string.Empty;
        public string FromCity { get; set; } = string.Empty;
        public string ToCity { get; set; } = string.Empty;
        public string? DepartureTerminal { get; set; }
        public string? ArrivalTerminal { get; set; }
        public DateTime DepartureDate { get; set; }
        public TimeSpan DepartureTime { get; set; }
        public DateTime? ArrivalDate { get; set; }
        public TimeSpan? ArrivalTime { get; set; }
        public decimal Price { get; set; }
        public int AvailableSeats { get; set; }
        public string BusModel { get; set; } = string.Empty;
        public string? LayoutType { get; set; }
    }

    /// <summary>
    /// Otobüs arama isteği için DTO
    /// </summary>
    public class BusSearchDTO
    {
        public string From { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
        public DateTime Date { get; set; }
    }
}


