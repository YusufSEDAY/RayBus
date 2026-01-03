namespace RayBus.Models.DTOs
{
    public class CityDTO
    {
        public int CityID { get; set; }
        public string CityName { get; set; } = string.Empty;
    }

    public class StationDTO
    {
        public int StationID { get; set; }
        public string StationName { get; set; } = string.Empty;
        public int CityID { get; set; }
        public string CityName { get; set; } = string.Empty;
    }

    public class TerminalDTO
    {
        public int TerminalID { get; set; }
        public string TerminalName { get; set; } = string.Empty;
        public int CityID { get; set; }
        public string CityName { get; set; } = string.Empty;
    }
}

