namespace RayBus.Models.Entities
{
    public class Role
    {
        public int RoleID { get; set; }
        public string RoleName { get; set; } = string.Empty;
        
        // Navigation properties
        public ICollection<User> Users { get; set; } = new List<User>();
    }
}

