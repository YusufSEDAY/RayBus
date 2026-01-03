using RayBus.Models.DTOs;
using RayBus.Models.Entities;
using RayBus.Repositories;

namespace RayBus.Services
{
    public class TrainService : ITrainService
    {
        private readonly ITrainRepository _trainRepository;
        private readonly ITripRepository _tripRepository;
        private readonly ICityRepository _cityRepository;
        private readonly ILogger<TrainService> _logger;

        public TrainService(
            ITrainRepository trainRepository,
            ITripRepository tripRepository,
            ICityRepository cityRepository,
            ILogger<TrainService> logger)
        {
            _trainRepository = trainRepository;
            _tripRepository = tripRepository;
            _cityRepository = cityRepository;
            _logger = logger;
        }

        public async Task<ApiResponse<IEnumerable<TrainDTO>>> GetAllTrainsAsync()
        {
            try
            {
                var trips = await _trainRepository.GetAllAsync();
                var trainDtos = trips.Select(MapToDTO).ToList();
                
                return ApiResponse<IEnumerable<TrainDTO>>.SuccessResponse(
                    trainDtos,
                    "Tren seferleri başarıyla getirildi"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tren seferleri getirilirken hata oluştu");
                return ApiResponse<IEnumerable<TrainDTO>>.ErrorResponse(
                    "Tren seferleri getirilirken bir hata oluştu",
                    new List<string> { ex.Message }
                );
            }
        }

        public async Task<ApiResponse<TrainDTO>> GetTrainByIdAsync(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return ApiResponse<TrainDTO>.ErrorResponse("Geçersiz tren ID'si");
                }

                var trip = await _trainRepository.GetByIdAsync(id);
                
                if (trip == null)
                {
                    return ApiResponse<TrainDTO>.ErrorResponse("Tren seferi bulunamadı");
                }

                var trainDto = MapToDTO(trip);
                return ApiResponse<TrainDTO>.SuccessResponse(trainDto, "Tren seferi başarıyla getirildi");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tren seferi getirilirken hata oluştu. ID: {Id}", id);
                return ApiResponse<TrainDTO>.ErrorResponse(
                    "Tren seferi getirilirken bir hata oluştu",
                    new List<string> { ex.Message }
                );
            }
        }

        public async Task<ApiResponse<IEnumerable<TrainDTO>>> SearchTrainsAsync(TrainSearchDTO searchDto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchDto.From))
                {
                    return ApiResponse<IEnumerable<TrainDTO>>.ErrorResponse("Kalkış şehri belirtilmelidir");
                }

                if (string.IsNullOrWhiteSpace(searchDto.To))
                {
                    return ApiResponse<IEnumerable<TrainDTO>>.ErrorResponse("Varış şehri belirtilmelidir");
                }

                if (searchDto.Date < DateTime.Today)
                {
                    return ApiResponse<IEnumerable<TrainDTO>>.ErrorResponse("Geçmiş tarih seçilemez");
                }

                // Şehir isimlerinden ID'ye dönüştür
                var fromCity = await _cityRepository.GetByNameAsync(searchDto.From);
                var toCity = await _cityRepository.GetByNameAsync(searchDto.To);

                if (fromCity == null)
                {
                    return ApiResponse<IEnumerable<TrainDTO>>.ErrorResponse($"Kalkış şehri bulunamadı: {searchDto.From}");
                }

                if (toCity == null)
                {
                    return ApiResponse<IEnumerable<TrainDTO>>.ErrorResponse($"Varış şehri bulunamadı: {searchDto.To}");
                }

                // Stored procedure kullanarak arama yap
                var searchResults = await _tripRepository.SearchUsingStoredProcedureAsync(
                    fromCity.CityID, 
                    toCity.CityID, 
                    searchDto.Date
                );

                // Sadece tren seferlerini filtrele ve DTO'ya dönüştür
                var trainDtos = searchResults
                    .Where(r => r.VehicleType == "Train")
                    .Select(MapFromSearchResult)
                    .ToList();

                return ApiResponse<IEnumerable<TrainDTO>>.SuccessResponse(
                    trainDtos,
                    $"{trainDtos.Count} tren seferi bulundu"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tren arama yapılırken hata oluştu");
                return ApiResponse<IEnumerable<TrainDTO>>.ErrorResponse(
                    "Tren arama yapılırken bir hata oluştu",
                    new List<string> { ex.Message }
                );
            }
        }

        private TrainDTO MapToDTO(Trip trip)
        {
            var availableSeats = trip.TripSeats?.Count(ts => !ts.IsReserved) ?? 0;
            var departureDateTime = trip.DepartureDate.Date.Add(trip.DepartureTime);
            var arrivalDateTime = trip.ArrivalDate.HasValue && trip.ArrivalTime.HasValue
                ? trip.ArrivalDate.Value.Date.Add(trip.ArrivalTime.Value)
                : (DateTime?)null;

            return new TrainDTO
            {
                TripID = trip.TripID,
                VehicleCode = trip.Vehicle?.PlateOrCode ?? string.Empty,
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
                AvailableSeats = availableSeats,
                TrainModel = trip.Vehicle?.Train?.TrainModel ?? string.Empty
            };
        }

        private TrainDTO MapFromSearchResult(TripSearchResultDTO result)
        {
            // KalkisSaati string formatından TimeSpan'e çevir (HH:mm formatında)
            TimeSpan departureTime = TimeSpan.Zero;
            if (TimeSpan.TryParse(result.KalkisSaati, out var parsedTime))
            {
                departureTime = parsedTime;
            }

            return new TrainDTO
            {
                TripID = result.TripID,
                VehicleCode = result.AracPlakaNo,
                FromCity = result.KalkisSehri,
                ToCity = result.VarisSehri,
                DepartureStation = result.KalkisNoktasi, // SP'den gelen KalkisNoktasi terminal veya istasyon olabilir
                ArrivalStation = result.VarisNoktasi,
                DepartureDate = result.DepartureDate,
                DepartureTime = departureTime,
                Price = result.Price,
                AvailableSeats = result.BosKoltukSayisi,
                TrainModel = result.AracModeli ?? string.Empty
            };
        }
    }
}
