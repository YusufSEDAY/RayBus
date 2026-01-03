using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RayBus.Data;
using RayBus.Models.Entities;
using System;

namespace RayBus.Repositories
{
    public class ReservationRepository : IReservationRepository
    {
        private readonly RayBusDbContext _context;
        private readonly string _connectionString;

        public ReservationRepository(RayBusDbContext context)
        {
            _context = context;
            _connectionString = context.Database.GetConnectionString() ?? throw new InvalidOperationException("Connection string not found");
        }

        public async Task<IEnumerable<Reservation>> GetAllAsync()
        {
            return await _context.Reservations
                .Include(r => r.User)
                .Include(r => r.TripSeat)
                    .ThenInclude(ts => ts!.Trip)
                        .ThenInclude(t => t!.Vehicle)
                            .ThenInclude(v => v!.Bus)
                .Include(r => r.TripSeat)
                    .ThenInclude(ts => ts!.Trip)
                        .ThenInclude(t => t!.Vehicle)
                            .ThenInclude(v => v!.Train)
                .Include(r => r.TripSeat)
                    .ThenInclude(ts => ts!.Trip)
                        .ThenInclude(t => t!.FromCity)
                .Include(r => r.TripSeat)
                    .ThenInclude(ts => ts!.Trip)
                        .ThenInclude(t => t!.ToCity)
                .Include(r => r.TripSeat)
                    .ThenInclude(ts => ts!.Seat)
                        .ThenInclude(s => s!.Wagon)
                .Include(r => r.CancelReason)
                .OrderByDescending(r => r.ReservationDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Reservation>> GetByUserIdAsync(int userId)
        {
            return await _context.Reservations
                .Include(r => r.User)
                .Include(r => r.TripSeat)
                    .ThenInclude(ts => ts!.Trip)
                        .ThenInclude(t => t!.Vehicle)
                            .ThenInclude(v => v!.Bus)
                .Include(r => r.TripSeat)
                    .ThenInclude(ts => ts!.Trip)
                        .ThenInclude(t => t!.Vehicle)
                            .ThenInclude(v => v!.Train)
                .Include(r => r.TripSeat)
                    .ThenInclude(ts => ts!.Trip)
                        .ThenInclude(t => t!.FromCity)
                .Include(r => r.TripSeat)
                    .ThenInclude(ts => ts!.Trip)
                        .ThenInclude(t => t!.ToCity)
                .Include(r => r.TripSeat)
                    .ThenInclude(ts => ts!.Trip)
                        .ThenInclude(t => t!.DepartureTerminal)
                .Include(r => r.TripSeat)
                    .ThenInclude(ts => ts!.Trip)
                        .ThenInclude(t => t!.ArrivalTerminal)
                .Include(r => r.TripSeat)
                    .ThenInclude(ts => ts!.Trip)
                        .ThenInclude(t => t!.DepartureStation)
                .Include(r => r.TripSeat)
                    .ThenInclude(ts => ts!.Trip)
                        .ThenInclude(t => t!.ArrivalStation)
                .Include(r => r.TripSeat)
                    .ThenInclude(ts => ts!.Seat)
                        .ThenInclude(s => s!.Wagon)
                .Include(r => r.CancelReason)
                .Where(r => r.UserID == userId && r.Status != "Cancelled")
                .OrderByDescending(r => r.ReservationDate)
                .ToListAsync();
        }

        public async Task<Reservation?> GetByIdAsync(int id)
        {
            return await _context.Reservations
                .Include(r => r.User)
                .Include(r => r.TripSeat)
                    .ThenInclude(ts => ts!.Trip)
                        .ThenInclude(t => t!.Vehicle)
                            .ThenInclude(v => v!.Bus)
                .Include(r => r.TripSeat)
                    .ThenInclude(ts => ts!.Trip)
                        .ThenInclude(t => t!.Vehicle)
                            .ThenInclude(v => v!.Train)
                .Include(r => r.TripSeat)
                    .ThenInclude(ts => ts!.Trip)
                        .ThenInclude(t => t!.FromCity)
                .Include(r => r.TripSeat)
                    .ThenInclude(ts => ts!.Trip)
                        .ThenInclude(t => t!.ToCity)
                .Include(r => r.TripSeat)
                    .ThenInclude(ts => ts!.Seat)
                        .ThenInclude(s => s!.Wagon)
                .Include(r => r.CancelReason)
                .Include(r => r.Payments)
                .FirstOrDefaultAsync(r => r.ReservationID == id);
        }

        public async Task<Reservation> AddAsync(Reservation reservation)
        {
            reservation.ReservationDate = DateTime.UtcNow;
            _context.Reservations.Add(reservation);
            
            // NOT: TripSeat güncellemesi trigger (trg_Rezervasyon_Sonrasi_Koltuk_Guncelle) tarafından otomatik yapılacak
            
            
            await _context.SaveChangesAsync();
            return reservation;
        }

        public async Task<Reservation> UpdateAsync(Reservation reservation)
        {
            _context.Reservations.Update(reservation);
            await _context.SaveChangesAsync();
            return reservation;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null) return false;
            
            _context.Reservations.Remove(reservation);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CancelAsync(int id, int? cancelReasonID = null, int? performedByUserId = null)
        {
            // Önce rezervasyonun var olup olmadığını ve durumunu kontrol et
            var reservation = await _context.Reservations
                .Include(r => r.TripSeat)
                    .ThenInclude(ts => ts!.Trip)
                .FirstOrDefaultAsync(r => r.ReservationID == id);
                
            if (reservation == null || reservation.Status == "Cancelled") 
            {
                return false;
            }
            
            // Raw SQL kullanarak hem UPDATE hem LOG işlemlerini transaction içinde yap
            // Trigger'lar için OUTPUT clause sorunu olmaması için raw SQL kullanıyoruz
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            
            using var transaction = connection.BeginTransaction();
            
            try
            {
                // 1. Rezervasyonu iptal et (CancelReasonID ile birlikte)
                // Trigger (trg_Rezervasyon_Iptali_Sonrasi_Koltuk_Bosa_Cikar) otomatik olarak çalışacak
                using var updateCommand = new SqlCommand(
                    "UPDATE app.Reservations SET Status = @Status, CancelReasonID = @CancelReasonID WHERE ReservationID = @ReservationID",
                    connection,
                    transaction
                );
                
                updateCommand.Parameters.AddWithValue("@Status", "Cancelled");
                updateCommand.Parameters.AddWithValue("@ReservationID", id);
                
                // CancelReasonID null ise DBNull.Value gönder
                if (cancelReasonID.HasValue && cancelReasonID.Value > 0)
                {
                    updateCommand.Parameters.AddWithValue("@CancelReasonID", cancelReasonID.Value);
                }
                else
                {
                    updateCommand.Parameters.AddWithValue("@CancelReasonID", DBNull.Value);
                }
                
                int rowsAffected = await updateCommand.ExecuteNonQueryAsync();
                
                if (rowsAffected > 0)
                {
                    // 2. ReservationLog kaydı ekle
                    var performedBy = performedByUserId ?? reservation.UserID; // int (reservation.UserID int olduğu için)
                    
                    // İptal nedeni bilgisini log'a ekle
                    string reasonText = string.Empty;
                    if (cancelReasonID.HasValue && cancelReasonID.Value > 0)
                    {
                        // CancellationReason tablosundan neden metnini al
                        using var reasonCommand = new SqlCommand(
                            "SELECT ReasonText FROM app.CancellationReasons WHERE ReasonID = @ReasonID",
                            connection,
                            transaction
                        );
                        reasonCommand.Parameters.AddWithValue("@ReasonID", cancelReasonID.Value);
                        var reasonResult = await reasonCommand.ExecuteScalarAsync();
                        if (reasonResult != null)
                        {
                            reasonText = $" İptal Nedeni: {reasonResult}";
                        }
                    }
                    
                    var details = $"Rezervasyon iptal edildi. Sefer: {reservation.TripID}, Koltuk: {reservation.SeatID}{reasonText}";
                    
                    using var logCommand = new SqlCommand(
                        "INSERT INTO log.ReservationLogs (ReservationID, Action, Details, LogDate, PerformedBy) " +
                        "VALUES (@ReservationID, @Action, @Details, @LogDate, @PerformedBy)",
                        connection,
                        transaction
                    );
                    
                    logCommand.Parameters.AddWithValue("@ReservationID", id);
                    logCommand.Parameters.AddWithValue("@Action", "Cancelled");
                    logCommand.Parameters.AddWithValue("@Details", details);
                    logCommand.Parameters.AddWithValue("@LogDate", DateTime.UtcNow);
                    logCommand.Parameters.AddWithValue("@PerformedBy", performedBy);
                    
                    await logCommand.ExecuteNonQueryAsync();
                    
                    transaction.Commit();
                    return true;
                }
                else
                {
                    transaction.Rollback();
                    return false;
                }
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task AddReservationLogAsync(int reservationId, string action, string details, int? performedByUserId = null)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                
                using var command = new SqlCommand(
                    "INSERT INTO log.ReservationLogs (ReservationID, Action, Details, LogDate, PerformedBy) " +
                    "VALUES (@ReservationID, @Action, @Details, @LogDate, @PerformedBy)",
                    connection
                );
                
                command.Parameters.AddWithValue("@ReservationID", reservationId);
                command.Parameters.AddWithValue("@Action", action);
                command.Parameters.AddWithValue("@Details", details ?? string.Empty);
                command.Parameters.AddWithValue("@LogDate", DateTime.UtcNow);
                
                var performedByParam = new SqlParameter("@PerformedBy", System.Data.SqlDbType.Int);
                performedByParam.Value = performedByUserId.HasValue ? (object)performedByUserId.Value : DBNull.Value;
                command.Parameters.Add(performedByParam);
                
                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                // Log hatası kritik değil, sadece log'la
                throw new Exception($"ReservationLog kaydı eklenirken hata oluştu: {ex.Message}", ex);
            }
        }

        public async Task<(bool Success, int ReservationID, string ErrorMessage, string PaymentStatus)> CreateReservationUsingStoredProcedureAsync(
            int tripId, int seatId, int userId, decimal price, string paymentMethod, byte islemTipi)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                using var command = new SqlCommand("[proc].sp_Rezervasyon_Yap", connection)
                {
                    CommandType = System.Data.CommandType.StoredProcedure
                };

                command.Parameters.AddWithValue("@SeferID", tripId);
                command.Parameters.AddWithValue("@KoltukID", seatId);
                command.Parameters.AddWithValue("@KullaniciID", userId);
                command.Parameters.AddWithValue("@Fiyat", price);
                command.Parameters.AddWithValue("@OdemeYontemi", paymentMethod ?? "Kredi Kartı");
                command.Parameters.AddWithValue("@IslemTipi", islemTipi);

                // Stored procedure SELECT ile sonuç döndürüyor: 'Başarılı' AS Sonuc, @YeniRezervasyonID AS RezervasyonID, @OdemeDurumu AS OdemeDurumu
                using var reader = await command.ExecuteReaderAsync();
                
                if (await reader.ReadAsync())
                {
                    var sonuc = reader.GetString(reader.GetOrdinal("Sonuc"));
                    var reservationId = reader.GetInt32(reader.GetOrdinal("RezervasyonID"));
                    var paymentStatus = reader.GetString(reader.GetOrdinal("OdemeDurumu"));

                    if (sonuc == "Başarılı" && reservationId > 0)
                    {
                        return (true, reservationId, string.Empty, paymentStatus);
                    }
                }

                return (false, 0, "Rezervasyon oluşturulamadı", string.Empty);
            }
            catch (SqlException sqlEx)
            {
                // Stored procedure'dan gelen hata mesajı (THROW ile fırlatılan)
                if (sqlEx.Number >= 50001 && sqlEx.Number <= 50003)
                {
                    return (false, 0, sqlEx.Message, string.Empty);
                }
                return (false, 0, $"Veritabanı hatası: {sqlEx.Message}", string.Empty);
            }
            catch (Exception ex)
            {
                return (false, 0, $"Hata: {ex.Message}", string.Empty);
            }
        }

        public async Task<(bool Success, string ErrorMessage)> CompletePaymentUsingStoredProcedureAsync(
            int reservationId, decimal price, string paymentMethod)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                using var command = new SqlCommand("[proc].sp_Odeme_Tamamla", connection)
                {
                    CommandType = System.Data.CommandType.StoredProcedure
                };

                command.Parameters.AddWithValue("@RezervasyonID", reservationId);
                command.Parameters.AddWithValue("@Fiyat", price);
                command.Parameters.AddWithValue("@OdemeYontemi", paymentMethod ?? "Kredi Kartı");

                // Stored procedure SELECT ile sonuç döndürüyor: 'Ödeme Başarıyla Tamamlandı' AS Mesaj
                using var reader = await command.ExecuteReaderAsync();
                
                if (await reader.ReadAsync())
                {
                    var mesaj = reader.GetString(reader.GetOrdinal("Mesaj"));
                    if (mesaj.Contains("Başarıyla"))
                    {
                        return (true, string.Empty);
                    }
                }

                return (false, "Ödeme tamamlanamadı");
            }
            catch (SqlException sqlEx)
            {
                // Stored procedure'dan gelen hata mesajı (THROW ile fırlatılan)
                if (sqlEx.Number >= 50001 && sqlEx.Number <= 50003)
                {
                    return (false, sqlEx.Message);
                }
                return (false, $"Veritabanı hatası: {sqlEx.Message}");
            }
            catch (Exception ex)
            {
                return (false, $"Hata: {ex.Message}");
            }
        }

        public async Task<IEnumerable<UserTicketDTO>> GetUserTicketsUsingStoredProcedureAsync(int userId)
        {
            var results = new List<UserTicketDTO>();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                using var command = new SqlCommand("[proc].sp_Kullanici_Biletleri", connection)
                {
                    CommandType = System.Data.CommandType.StoredProcedure
                };

                command.Parameters.AddWithValue("@KullaniciID", userId);

                using var reader = await command.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    results.Add(new UserTicketDTO
                    {
                        ReservationID = reader.GetInt32(reader.GetOrdinal("ReservationID")),
                        TripID = HasColumn(reader, "TripID") 
                            ? reader.GetInt32(reader.GetOrdinal("TripID")) 
                            : 0, // Backward compatibility
                        Guzergah = reader.GetString(reader.GetOrdinal("Guzergah")),
                        DepartureDate = reader.GetDateTime(reader.GetOrdinal("DepartureDate")),
                        KalkisSaati = reader.GetString(reader.GetOrdinal("KalkisSaati")),
                        VehicleType = reader.GetString(reader.GetOrdinal("VehicleType")),
                        PlateOrCode = reader.IsDBNull(reader.GetOrdinal("PlateOrCode")) 
                            ? string.Empty 
                            : reader.GetString(reader.GetOrdinal("PlateOrCode")),
                        SeatNo = reader.GetString(reader.GetOrdinal("SeatNo")),
                        OdenenTutar = reader.IsDBNull(reader.GetOrdinal("OdenenTutar")) 
                            ? 0 
                            : reader.GetDecimal(reader.GetOrdinal("OdenenTutar")),
                        TripFiyati = HasColumn(reader, "TripFiyati")
                            ? (reader.IsDBNull(reader.GetOrdinal("TripFiyati"))
                                ? 0
                                : reader.GetDecimal(reader.GetOrdinal("TripFiyati")))
                            : 0, // Backward compatibility
                        RezervasyonDurumu = reader.GetString(reader.GetOrdinal("RezervasyonDurumu")),
                        PaymentStatus = reader.GetString(reader.GetOrdinal("PaymentStatus")),
                        IslemTarihi = reader.GetDateTime(reader.GetOrdinal("IslemTarihi"))
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Stored procedure hatası: {ex.Message}", ex);
            }

            return results;
        }

        private bool HasColumn(SqlDataReader reader, string columnName)
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (reader.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
