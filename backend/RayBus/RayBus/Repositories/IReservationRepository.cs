using RayBus.Models.Entities;

namespace RayBus.Repositories
{
    /// <summary>
    /// Rezervasyon repository interface'i
    /// </summary>
    public interface IReservationRepository
    {
        Task<IEnumerable<Reservation>> GetAllAsync();
        Task<IEnumerable<Reservation>> GetByUserIdAsync(int userId);
        Task<Reservation?> GetByIdAsync(int id);
        Task<Reservation> AddAsync(Reservation reservation);
        Task<Reservation> UpdateAsync(Reservation reservation);
        Task<bool> DeleteAsync(int id);
        Task<bool> CancelAsync(int id, int? cancelReasonID = null, int? performedByUserId = null);
        
        /// <summary>
        /// Stored procedure kullanarak rezervasyon yapar (transaction güvenli)
        /// </summary>
        Task<(bool Success, int ReservationID, string ErrorMessage, string PaymentStatus)> CreateReservationUsingStoredProcedureAsync(
            int tripId, int seatId, int userId, decimal price, string paymentMethod, byte islemTipi);
        
        /// <summary>
        /// Stored procedure kullanarak ödeme tamamlar (transaction güvenli)
        /// </summary>
        Task<(bool Success, string ErrorMessage)> CompletePaymentUsingStoredProcedureAsync(
            int reservationId, decimal price, string paymentMethod);
        
        /// <summary>
        /// Stored procedure kullanarak kullanıcı biletlerini getirir
        /// </summary>
        Task<IEnumerable<UserTicketDTO>> GetUserTicketsUsingStoredProcedureAsync(int userId);
        
        /// <summary>
        /// Rezervasyon log kaydı ekler
        /// </summary>
        Task AddReservationLogAsync(int reservationId, string action, string details, int? performedByUserId = null);
    }
    
    /// <summary>
    /// Stored procedure'den dönen kullanıcı bilet DTO
    /// </summary>
    public class UserTicketDTO
    {
        public int ReservationID { get; set; }
        public int TripID { get; set; } // Eklendi
        public string Guzergah { get; set; } = string.Empty;
        public DateTime DepartureDate { get; set; }
        public string KalkisSaati { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;
        public string PlateOrCode { get; set; } = string.Empty;
        public string SeatNo { get; set; } = string.Empty;
        public decimal OdenenTutar { get; set; }
        public decimal TripFiyati { get; set; } // YENİ: Stored procedure'den gelen Trip fiyatı
        public string RezervasyonDurumu { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public DateTime IslemTarihi { get; set; }
    }
}


