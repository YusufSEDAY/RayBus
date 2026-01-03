namespace RayBus.Models.DTOs
{
    /// <summary>
    /// Tren seferi için Data Transfer Object
    /// </summary>
    public class TrainDTO
    {
        public int TripID { get; set; }
        public string VehicleCode { get; set; } = string.Empty;
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
        public int AvailableSeats { get; set; }
        public string TrainModel { get; set; } = string.Empty;
    }

    /// <summary>
    /// Tren arama isteği için DTO
    /// </summary>
    public class TrainSearchDTO
    {
        public string From { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
        public DateTime Date { get; set; }
    }
}


