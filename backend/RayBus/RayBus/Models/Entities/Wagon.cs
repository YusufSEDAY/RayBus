namespace RayBus.Models.Entities
{
    public class Wagon
    {
        public int WagonID { get; set; }
        public int TrainID { get; set; }
        public int WagonNo { get; set; }
        public int SeatCount { get; set; }
        
        // Navigation properties
        public Train? Train { get; set; }
        public ICollection<Seat> Seats { get; set; } = new List<Seat>();
    }
}

