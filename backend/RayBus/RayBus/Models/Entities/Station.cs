namespace RayBus.Models.Entities
{
    public class Station
    {
        public int StationID { get; set; }
        public int CityID { get; set; }
        public string StationName { get; set; } = string.Empty;
        public string? Address { get; set; }
        
        // Navigation properties
        public City? City { get; set; }
        public ICollection<Trip> DepartureTrips { get; set; } = new List<Trip>();
        public ICollection<Trip> ArrivalTrips { get; set; } = new List<Trip>();
    }
}

