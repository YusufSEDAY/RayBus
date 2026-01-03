namespace RayBus.Models.Entities
{
    public class City
    {
        public int CityID { get; set; }
        public string CityName { get; set; } = string.Empty;
        
        // Navigation properties
        public ICollection<Terminal> Terminals { get; set; } = new List<Terminal>();
        public ICollection<Station> Stations { get; set; } = new List<Station>();
        public ICollection<Trip> DepartureTrips { get; set; } = new List<Trip>();
        public ICollection<Trip> ArrivalTrips { get; set; } = new List<Trip>();
    }
}

