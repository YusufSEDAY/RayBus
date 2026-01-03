namespace RayBus.Models.Entities
{
    public class Train
    {
        public int TrainID { get; set; }
        public string? TrainModel { get; set; }
        
        // Navigation properties
        public Vehicle? Vehicle { get; set; }
        public ICollection<Wagon> Wagons { get; set; } = new List<Wagon>();
    }
}

