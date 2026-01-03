namespace RayBus.Models.Entities
{
    public class User
    {
        public int UserID { get; set; }
        public int RoleID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public byte Status { get; set; } = 1; // 1=Active, 0=Inactive
        
        // Navigation properties
        public Role? Role { get; set; }
        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
        public ICollection<TripLog> TripLogs { get; set; } = new List<TripLog>();
        public ICollection<ReservationLog> ReservationLogs { get; set; } = new List<ReservationLog>();
        public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>(); // Şirket araçları
    }
}

