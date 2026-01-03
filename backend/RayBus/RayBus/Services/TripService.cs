using Microsoft.EntityFrameworkCore;
using RayBus.Data;
using RayBus.Models.DTOs;
using RayBus.Models.Entities;
using RayBus.Repositories;

namespace RayBus.Services
{
    public class TripService : ITripService
    {
        private readonly ITripRepository _tripRepository;
        private readonly RayBusDbContext _context;
        private readonly ILogger<TripService> _logger;

        public TripService(ITripRepository tripRepository, RayBusDbContext context, ILogger<TripService> logger)
        {
            _tripRepository = tripRepository;
            _context = context;
            _logger = logger;
        }

        public async Task<ApiResponse<TripDetailDTO>> GetTripDetailAsync(int tripId)
        {
            try
            {
                if (tripId <= 0)
                {
                    return ApiResponse<TripDetailDTO>.ErrorResponse("Geçersiz sefer ID'si");
                }

                var trip = await _tripRepository.GetByIdAsync(tripId);
                if (trip == null || trip.Status != 1)
                {
                    return ApiResponse<TripDetailDTO>.ErrorResponse("Sefer bulunamadı veya aktif değil");
                }

                // Stored procedure kullanarak koltuk durumunu getir (daha optimize ve doğru sıralama)
                var seatStatuses = await _tripRepository.GetSeatStatusUsingStoredProcedureAsync(tripId);
                var availableSeats = seatStatuses.Count(s => !s.IsReserved);
                
                // TripSeatID'leri almak için TripSeats tablosuna bak
                var tripSeats = await _tripRepository.GetAllSeatsAsync(tripId);
                var tripSeatDict = tripSeats.ToDictionary(ts => ts.SeatID, ts => ts.TripSeatID);

                // Model bilgisini al - debug için log ekle
                string? vehicleModel = null;
                if (trip.Vehicle != null)
                {
                    if (trip.Vehicle.VehicleType == "Bus" && trip.Vehicle.Bus != null)
                    {
                        vehicleModel = trip.Vehicle.Bus.BusModel;
                        _logger.LogInformation("Bus model bulundu: {Model} (TripID: {TripID})", vehicleModel, tripId);
                    }
                    else if (trip.Vehicle.VehicleType == "Train" && trip.Vehicle.Train != null)
                    {
                        vehicleModel = trip.Vehicle.Train.TrainModel;
                        _logger.LogInformation("Train model bulundu: {Model} (TripID: {TripID})", vehicleModel, tripId);
                    }
                    else
                    {
                        _logger.LogWarning("Model bilgisi bulunamadı. VehicleType: {Type}, Bus: {Bus}, Train: {Train} (TripID: {TripID})", 
                            trip.Vehicle.VehicleType, trip.Vehicle.Bus != null, trip.Vehicle.Train != null, tripId);
                    }
                }
                else
                {
                    _logger.LogWarning("Vehicle null (TripID: {TripID})", tripId);
                }

                var tripDetail = new TripDetailDTO
                {
                    TripID = trip.TripID,
                    VehicleCode = trip.Vehicle?.PlateOrCode ?? string.Empty,
                    VehicleType = trip.Vehicle?.VehicleType ?? string.Empty,
                    VehicleModel = vehicleModel ?? string.Empty, // Boş string yerine null olabilir ama frontend'de kontrol ediyoruz
                    FromCity = trip.FromCity?.CityName ?? string.Empty,
                    ToCity = trip.ToCity?.CityName ?? string.Empty,
                    DepartureTerminal = trip.DepartureTerminal?.TerminalName,
                    ArrivalTerminal = trip.ArrivalTerminal?.TerminalName,
                    DepartureStation = trip.DepartureStation?.StationName,
                    ArrivalStation = trip.ArrivalStation?.StationName,
                    DepartureDate = trip.DepartureDate,
                    DepartureTime = trip.DepartureTime,
                    ArrivalDate = trip.ArrivalDate,
                    ArrivalTime = trip.ArrivalTime,
                    Price = trip.Price,
                    TotalSeats = trip.Vehicle?.SeatCount ?? 0,
                    AvailableSeats = availableSeats,
                    LayoutType = trip.Vehicle?.VehicleType == "Bus" ? trip.Vehicle.Bus?.LayoutType : null,
                    Seats = seatStatuses.Select(ss => new SeatInfoDTO
                    {
                        SeatID = ss.SeatID,
                        TripSeatID = tripSeatDict.ContainsKey(ss.SeatID) ? tripSeatDict[ss.SeatID] : 0,
                        SeatNo = ss.SeatNo,
                        SeatPosition = ss.SeatPosition,
                        WagonNo = ss.VagonNo,
                        IsReserved = ss.IsReserved,
                        IsActive = true, // Stored procedure'de IsActive yok, varsayılan true
                        PaymentStatus = ss.PaymentStatus // PaymentStatus bilgisini ekle
                    }).ToList()
                };

                return ApiResponse<TripDetailDTO>.SuccessResponse(tripDetail, "Sefer detayı başarıyla getirildi");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sefer detayı getirilirken hata oluştu. TripID: {TripID}", tripId);
                return ApiResponse<TripDetailDTO>.ErrorResponse(
                    "Sefer detayı getirilirken bir hata oluştu",
                    new List<string> { ex.Message }
                );
            }
        }

        public async Task<ApiResponse<TripDetailDTO>> CreateTripAsync(CreateTripDTO createDto)
        {
            try
            {
                // Validasyonlar
                if (createDto.VehicleID <= 0)
                {
                    return ApiResponse<TripDetailDTO>.ErrorResponse("Geçersiz araç ID'si");
                }

                if (createDto.FromCityID == createDto.ToCityID)
                {
                    return ApiResponse<TripDetailDTO>.ErrorResponse("Kalkış ve varış şehirleri aynı olamaz");
                }

                if (createDto.Price < 0)
                {
                    return ApiResponse<TripDetailDTO>.ErrorResponse("Fiyat negatif olamaz");
                }

                // Araç kontrolü
                var vehicle = await _context.Vehicles
                    .Include(v => v.Seats)
                    .FirstOrDefaultAsync(v => v.VehicleID == createDto.VehicleID && v.Active);

                if (vehicle == null)
                {
                    return ApiResponse<TripDetailDTO>.ErrorResponse("Araç bulunamadı veya aktif değil");
                }

                // Sefer oluştur
                var trip = new Trip
                {
                    VehicleID = createDto.VehicleID,
                    FromCityID = createDto.FromCityID,
                    ToCityID = createDto.ToCityID,
                    DepartureTerminalID = createDto.DepartureTerminalID,
                    ArrivalTerminalID = createDto.ArrivalTerminalID,
                    DepartureStationID = createDto.DepartureStationID,
                    ArrivalStationID = createDto.ArrivalStationID,
                    DepartureDate = createDto.DepartureDate,
                    DepartureTime = createDto.DepartureTime,
                    ArrivalDate = createDto.ArrivalDate,
                    ArrivalTime = createDto.ArrivalTime,
                    Price = createDto.Price,
                    Status = 1,
                    CreatedAt = DateTime.UtcNow
                };

                var createdTrip = await _tripRepository.AddAsync(trip);

                // TripSeat kayıtlarını oluştur (aracın tüm koltukları için)
                var seats = await _context.Seats
                    .Where(s => s.VehicleID == createDto.VehicleID && s.IsActive)
                    .ToListAsync();

                var tripSeats = seats.Select(seat => new TripSeat
                {
                    TripID = createdTrip.TripID,
                    SeatID = seat.SeatID,
                    IsReserved = false
                }).ToList();

                _context.TripSeats.AddRange(tripSeats);
                await _context.SaveChangesAsync();

                // Oluşturulan seferin detayını getir
                return await GetTripDetailAsync(createdTrip.TripID);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sefer oluşturulurken hata oluştu");
                return ApiResponse<TripDetailDTO>.ErrorResponse(
                    "Sefer oluşturulurken bir hata oluştu",
                    new List<string> { ex.Message }
                );
            }
        }
    }
}

