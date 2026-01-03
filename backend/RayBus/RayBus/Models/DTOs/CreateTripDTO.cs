namespace RayBus.Models.DTOs
{
    public class CreateTripDTO
    {
        public int VehicleID { get; set; }
        public int FromCityID { get; set; }
        public int ToCityID { get; set; }
        public int? DepartureTerminalID { get; set; }
        public int? ArrivalTerminalID { get; set; }
        public int? DepartureStationID { get; set; }
        public int? ArrivalStationID { get; set; }
        public DateTime DepartureDate { get; set; }
        public TimeSpan DepartureTime { get; set; }
        public DateTime? ArrivalDate { get; set; }
        public TimeSpan? ArrivalTime { get; set; }
        public decimal Price { get; set; }
    }
}

