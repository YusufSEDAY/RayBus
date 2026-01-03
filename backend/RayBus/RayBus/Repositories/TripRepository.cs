using System;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RayBus.Data;
using RayBus.Models.Entities;

namespace RayBus.Repositories
{
    public class TripRepository : ITripRepository
    {
        private readonly RayBusDbContext _context;
        private readonly string _connectionString;

        public TripRepository(RayBusDbContext context)
        {
            _context = context;
            _connectionString = context.Database.GetConnectionString() ?? throw new InvalidOperationException("Connection string not found");
        }

        public async Task<IEnumerable<Trip>> GetAllAsync()
        {
            return await _context.Trips
                .Include(t => t.Vehicle)
                    .ThenInclude(v => v!.Bus)
                .Include(t => t.Vehicle)
                    .ThenInclude(v => v!.Train)
                .Include(t => t.FromCity)
                .Include(t => t.ToCity)
                .Include(t => t.DepartureTerminal)
                .Include(t => t.ArrivalTerminal)
                .Include(t => t.DepartureStation)
                .Include(t => t.ArrivalStation)
                .Include(t => t.TripSeats)
                    .ThenInclude(ts => ts.Seat)
                .Where(t => t.Status == 1)
                .ToListAsync();
        }

        public async Task<Trip?> GetByIdAsync(int id)
        {
            return await _context.Trips
                .Include(t => t.Vehicle)
                    .ThenInclude(v => v!.Bus)
                .Include(t => t.Vehicle)
                    .ThenInclude(v => v!.Train)
                .Include(t => t.FromCity)
                .Include(t => t.ToCity)
                .Include(t => t.DepartureTerminal)
                .Include(t => t.ArrivalTerminal)
                .Include(t => t.DepartureStation)
                .Include(t => t.ArrivalStation)
                .Include(t => t.TripSeats)
                    .ThenInclude(ts => ts.Seat)
                .FirstOrDefaultAsync(t => t.TripID == id && t.Status == 1);
        }

        public async Task<IEnumerable<Trip>> SearchAsync(int fromCityId, int toCityId, DateTime date, string? vehicleType = null)
        {
            var query = _context.Trips
                .Include(t => t.Vehicle)
                    .ThenInclude(v => v!.Bus)
                .Include(t => t.Vehicle)
                    .ThenInclude(v => v!.Train)
                .Include(t => t.FromCity)
                .Include(t => t.ToCity)
                .Include(t => t.DepartureTerminal)
                .Include(t => t.ArrivalTerminal)
                .Include(t => t.DepartureStation)
                .Include(t => t.ArrivalStation)
                .Include(t => t.TripSeats)
                    .ThenInclude(ts => ts.Seat)
                .Where(t => t.FromCityID == fromCityId 
                         && t.ToCityID == toCityId 
                         && t.DepartureDate.Date == date.Date 
                         && t.Status == 1);

            if (!string.IsNullOrEmpty(vehicleType))
            {
                query = query.Where(t => t.Vehicle != null && t.Vehicle.VehicleType == vehicleType);
            }

            return await query.OrderBy(t => t.DepartureTime).ToListAsync();
        }

        public async Task<IEnumerable<Trip>> GetByVehicleTypeAsync(string vehicleType)
        {
            return await _context.Trips
                .Include(t => t.Vehicle)
                    .ThenInclude(v => v!.Bus)
                .Include(t => t.Vehicle)
                    .ThenInclude(v => v!.Train)
                .Include(t => t.FromCity)
                .Include(t => t.ToCity)
                .Include(t => t.DepartureTerminal)
                .Include(t => t.ArrivalTerminal)
                .Include(t => t.DepartureStation)
                .Include(t => t.ArrivalStation)
                .Include(t => t.TripSeats)
                    .ThenInclude(ts => ts.Seat)
                .Where(t => t.Vehicle != null && t.Vehicle.VehicleType == vehicleType && t.Status == 1)
                .ToListAsync();
        }

        public async Task<Trip> AddAsync(Trip trip)
        {
            trip.CreatedAt = DateTime.UtcNow;
            _context.Trips.Add(trip);
            await _context.SaveChangesAsync();
            return trip;
        }

        public async Task<Trip> UpdateAsync(Trip trip)
        {
            _context.Trips.Update(trip);
            await _context.SaveChangesAsync();
            return trip;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var trip = await _context.Trips.FindAsync(id);
            if (trip == null) return false;
            trip.Status = 0; // Cancelled
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<TripSeat>> GetAvailableSeatsAsync(int tripId)
        {
            return await _context.TripSeats
                .Include(ts => ts.Seat)
                    .ThenInclude(s => s!.Wagon)
                .Where(ts => ts.TripID == tripId && !ts.IsReserved)
                .ToListAsync();
        }

        public async Task<IEnumerable<TripSeat>> GetAllSeatsAsync(int tripId)
        {
            return await _context.TripSeats
                .Include(ts => ts.Seat)
                    .ThenInclude(s => s!.Wagon)
                .Where(ts => ts.TripID == tripId)
                .OrderBy(ts => ts.Seat!.SeatNo)
                .ToListAsync();
        }

        /// <summary>
        /// Kolonun var olup olmadığını kontrol eder
        /// </summary>
        private bool HasColumn(SqlDataReader reader, string columnName)
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (reader.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Güvenli bir şekilde string kolon okur (kolon yoksa null döner)
        /// </summary>
        private string? GetStringSafe(SqlDataReader reader, string columnName)
        {
            if (!HasColumn(reader, columnName))
                return null;
            
            var ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
        }

        public async Task<IEnumerable<TripSearchResultDTO>> SearchUsingStoredProcedureAsync(int fromCityId, int toCityId, DateTime date)
        {
            var results = new List<TripSearchResultDTO>();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                using var command = new SqlCommand("[proc].sp_Seferleri_Listele", connection)
                {
                    CommandType = System.Data.CommandType.StoredProcedure
                };

                command.Parameters.AddWithValue("@NeredenID", fromCityId);
                command.Parameters.AddWithValue("@NereyeID", toCityId);
                command.Parameters.AddWithValue("@Tarih", date.Date);

                using var reader = await command.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    results.Add(new TripSearchResultDTO
                    {
                        TripID = reader.GetInt32(reader.GetOrdinal("TripID")),
                        KalkisSehri = reader.GetString(reader.GetOrdinal("KalkisSehri")),
                        VarisSehri = reader.GetString(reader.GetOrdinal("VarisSehri")),
                        DepartureDate = reader.GetDateTime(reader.GetOrdinal("DepartureDate")),
                        KalkisSaati = reader.GetString(reader.GetOrdinal("KalkisSaati")),
                        Price = reader.GetDecimal(reader.GetOrdinal("Price")),
                        VehicleType = reader.GetString(reader.GetOrdinal("VehicleType")),
                        AracPlakaNo = reader.IsDBNull(reader.GetOrdinal("AracPlakaNo")) 
                            ? string.Empty 
                            : reader.GetString(reader.GetOrdinal("AracPlakaNo")),
                        // Model bilgisi optional - stored procedure güncellenmemiş olabilir
                        AracModeli = GetStringSafe(reader, "AracModeli"),
                        KoltukDuzeni = GetStringSafe(reader, "KoltukDuzeni"),
                        BosKoltukSayisi = reader.GetInt32(reader.GetOrdinal("BosKoltukSayisi")),
                        KalkisNoktasi = reader.IsDBNull(reader.GetOrdinal("KalkisNoktasi")) 
                            ? null 
                            : reader.GetString(reader.GetOrdinal("KalkisNoktasi")),
                        VarisNoktasi = reader.IsDBNull(reader.GetOrdinal("VarisNoktasi")) 
                            ? null 
                            : reader.GetString(reader.GetOrdinal("VarisNoktasi"))
                    });
                }
            }
            catch (Exception ex)
            {
                // Log error
                throw new Exception($"Stored procedure hatası: {ex.Message}", ex);
            }

            return results;
        }

        public async Task<IEnumerable<SeatStatusDTO>> GetSeatStatusUsingStoredProcedureAsync(int tripId)
        {
            var results = new List<SeatStatusDTO>();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                using var command = new SqlCommand("[proc].sp_Sefer_Koltuk_Durumu", connection)
                {
                    CommandType = System.Data.CommandType.StoredProcedure
                };

                command.Parameters.AddWithValue("@SeferID", tripId);

                using var reader = await command.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    var seatStatus = new SeatStatusDTO
                    {
                        SeatID = reader.GetInt32(reader.GetOrdinal("SeatID")),
                        SeatNo = reader.GetString(reader.GetOrdinal("SeatNo")),
                        SeatPosition = reader.IsDBNull(reader.GetOrdinal("SeatPosition")) 
                            ? null 
                            : reader.GetString(reader.GetOrdinal("SeatPosition")),
                        IsReserved = reader.GetBoolean(reader.GetOrdinal("IsReserved")),
                        VagonNo = reader.IsDBNull(reader.GetOrdinal("VagonNo")) 
                            ? null 
                            : reader.GetInt32(reader.GetOrdinal("VagonNo"))
                    };

                    // PaymentStatus varsa ekle (stored procedure'de yeni eklendi)
                    if (HasColumn(reader, "PaymentStatus") && !reader.IsDBNull(reader.GetOrdinal("PaymentStatus")))
                    {
                        seatStatus.PaymentStatus = reader.GetString(reader.GetOrdinal("PaymentStatus"));
                    }
                    else
                    {
                        // PaymentStatus kolonu yoksa veya NULL ise, null olarak ayarla
                        seatStatus.PaymentStatus = null;
                    }

                    results.Add(seatStatus);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Stored procedure hatası: {ex.Message}", ex);
            }

            return results;
        }
    }
}

