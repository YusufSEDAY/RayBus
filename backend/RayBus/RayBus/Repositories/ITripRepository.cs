using RayBus.Models.Entities;

namespace RayBus.Repositories
{
    /// <summary>
    /// Sefer (Trip) repository interface'i
    /// </summary>
    public interface ITripRepository
    {
        Task<IEnumerable<Trip>> GetAllAsync();
        Task<Trip?> GetByIdAsync(int id);
        Task<IEnumerable<Trip>> SearchAsync(int fromCityId, int toCityId, DateTime date, string? vehicleType = null);
        Task<IEnumerable<Trip>> GetByVehicleTypeAsync(string vehicleType);
        Task<Trip> AddAsync(Trip trip);
        Task<Trip> UpdateAsync(Trip trip);
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<TripSeat>> GetAvailableSeatsAsync(int tripId);
        Task<IEnumerable<TripSeat>> GetAllSeatsAsync(int tripId);
        
        /// <summary>
        /// Stored procedure kullanarak sefer arama
        /// </summary>
        Task<IEnumerable<TripSearchResultDTO>> SearchUsingStoredProcedureAsync(int fromCityId, int toCityId, DateTime date);
        
        /// <summary>
        /// Stored procedure kullanarak sefer koltuk durumunu getir
        /// </summary>
        Task<IEnumerable<SeatStatusDTO>> GetSeatStatusUsingStoredProcedureAsync(int tripId);
    }
    
    /// <summary>
    /// Stored procedure'den dönen koltuk durumu DTO
    /// </summary>
    public class SeatStatusDTO
    {
        public int SeatID { get; set; }
        public string SeatNo { get; set; } = string.Empty;
        public string? SeatPosition { get; set; }
        public bool IsReserved { get; set; }
        public string? PaymentStatus { get; set; } // 'Pending', 'Paid', 'Refunded' veya NULL
        public int? VagonNo { get; set; }
    }
    
    /// <summary>
    /// Stored procedure'den dönen sefer arama sonucu DTO
    /// </summary>
    public class TripSearchResultDTO
    {
        public int TripID { get; set; }
        public string KalkisSehri { get; set; } = string.Empty;
        public string VarisSehri { get; set; } = string.Empty;
        public DateTime DepartureDate { get; set; }
        public string KalkisSaati { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string VehicleType { get; set; } = string.Empty;
        public string AracPlakaNo { get; set; } = string.Empty;
        public string? AracModeli { get; set; }
        public string? KoltukDuzeni { get; set; }
        public int BosKoltukSayisi { get; set; }
        public string? KalkisNoktasi { get; set; }
        public string? VarisNoktasi { get; set; }
    }
}

