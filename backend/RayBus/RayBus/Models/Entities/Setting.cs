namespace RayBus.Models.Entities
{
    public class Setting
    {
        public string SettingKey { get; set; } = string.Empty;
        public string? SettingValue { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}

