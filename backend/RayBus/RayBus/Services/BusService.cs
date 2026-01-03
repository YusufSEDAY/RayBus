using RayBus.Models.DTOs;
using RayBus.Models.Entities;
using RayBus.Repositories;

namespace RayBus.Services
{
    public class BusService : IBusService
    {
        private readonly IBusRepository _busRepository;
        private readonly ITripRepository _tripRepository;
        private readonly ICityRepository _cityRepository;
        private readonly ILogger<BusService> _logger;

        public BusService(
            IBusRepository busRepository,
            ITripRepository tripRepository,
            ICityRepository cityRepository,
            ILogger<BusService> logger)
        {
            _busRepository = busRepository;
            _tripRepository = tripRepository;
            _cityRepository = cityRepository;
            _logger = logger;
        }

        public async Task<ApiResponse<IEnumerable<BusDTO>>> GetAllBusesAsync()
        {
            try
            {
                var trips = await _busRepository.GetAllAsync();
                var busDtos = trips.Select(MapToDTO).ToList();

                return ApiResponse<IEnumerable<BusDTO>>.SuccessResponse(
                    busDtos,
                    "Otobüs seferleri başarıyla getirildi"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Otobüs seferleri getirilirken hata oluştu");
                return ApiResponse<IEnumerable<BusDTO>>.ErrorResponse(
                    "Otobüs seferleri getirilirken bir hata oluştu",
                    new List<string> { ex.Message }
                );
            }
        }

        public async Task<ApiResponse<BusDTO>> GetBusByIdAsync(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return ApiResponse<BusDTO>.ErrorResponse("Geçersiz otobüs ID'si");
                }

                var trip = await _busRepository.GetByIdAsync(id);

                if (trip == null)
                {
                    return ApiResponse<BusDTO>.ErrorResponse("Otobüs seferi bulunamadı");
                }

                var busDto = MapToDTO(trip);
                return ApiResponse<BusDTO>.SuccessResponse(busDto, "Otobüs seferi başarıyla getirildi");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Otobüs seferi getirilirken hata oluştu. ID: {Id}", id);
                return ApiResponse<BusDTO>.ErrorResponse(
                    "Otobüs seferi getirilirken bir hata oluştu",
                    new List<string> { ex.Message }
                );
            }
        }

        public async Task<ApiResponse<IEnumerable<BusDTO>>> SearchBusesAsync(BusSearchDTO searchDto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchDto.From))
                {
                    return ApiResponse<IEnumerable<BusDTO>>.ErrorResponse("Kalkış şehri belirtilmelidir");
                }

                if (string.IsNullOrWhiteSpace(searchDto.To))
                {
                    return ApiResponse<IEnumerable<BusDTO>>.ErrorResponse("Varış şehri belirtilmelidir");
                }

                if (searchDto.Date < DateTime.Today)
                {
                    return ApiResponse<IEnumerable<BusDTO>>.ErrorResponse("Geçmiş tarih seçilemez");
                }

                // Şehir isimlerinden ID'ye dönüştür
                var fromCity = await _cityRepository.GetByNameAsync(searchDto.From);
                var toCity = await _cityRepository.GetByNameAsync(searchDto.To);

                if (fromCity == null)
                {
                    return ApiResponse<IEnumerable<BusDTO>>.ErrorResponse($"Kalkış şehri bulunamadı: {searchDto.From}");
                }

                if (toCity == null)
                {
                    return ApiResponse<IEnumerable<BusDTO>>.ErrorResponse($"Varış şehri bulunamadı: {searchDto.To}");
                }

                // Stored procedure kullanarak arama yap
                var searchResults = await _tripRepository.SearchUsingStoredProcedureAsync(
                    fromCity.CityID, 
                    toCity.CityID, 
                    searchDto.Date
                );

                // Sadece otobüs seferlerini filtrele ve DTO'ya dönüştür
                var busDtos = searchResults
                    .Where(r => r.VehicleType == "Bus")
                    .Select(MapFromSearchResult)
                    .ToList();

                return ApiResponse<IEnumerable<BusDTO>>.SuccessResponse(
                    busDtos,
                    $"{busDtos.Count} otobüs seferi bulundu"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Otobüs arama yapılırken hata oluştu");
                return ApiResponse<IEnumerable<BusDTO>>.ErrorResponse(
                    "Otobüs arama yapılırken bir hata oluştu",
                    new List<string> { ex.Message }
                );
            }
        }

        private BusDTO MapToDTO(Trip trip)
        {
            var availableSeats = trip.TripSeats?.Count(ts => !ts.IsReserved) ?? 0;

            return new BusDTO
            {
                TripID = trip.TripID,
                VehicleCode = trip.Vehicle?.PlateOrCode ?? string.Empty,
                FromCity = trip.FromCity?.CityName ?? string.Empty,
                ToCity = trip.ToCity?.CityName ?? string.Empty,
                DepartureTerminal = trip.DepartureTerminal?.TerminalName,
                ArrivalTerminal = trip.ArrivalTerminal?.TerminalName,
                DepartureDate = trip.DepartureDate,
                DepartureTime = trip.DepartureTime,
                ArrivalDate = trip.ArrivalDate,
                ArrivalTime = trip.ArrivalTime,
                Price = trip.Price,
                AvailableSeats = availableSeats,
                BusModel = trip.Vehicle?.Bus?.BusModel ?? string.Empty,
                LayoutType = trip.Vehicle?.Bus?.LayoutType
            };
        }

        private BusDTO MapFromSearchResult(TripSearchResultDTO result)
        {
            // KalkisSaati string formatından TimeSpan'e çevir (HH:mm formatında)
            TimeSpan departureTime = TimeSpan.Zero;
            if (TimeSpan.TryParse(result.KalkisSaati, out var parsedTime))
            {
                departureTime = parsedTime;
            }

            return new BusDTO
            {
                TripID = result.TripID,
                VehicleCode = result.AracPlakaNo,
                FromCity = result.KalkisSehri,
                ToCity = result.VarisSehri,
                DepartureTerminal = result.KalkisNoktasi, // SP'den gelen KalkisNoktasi terminal veya istasyon olabilir
                ArrivalTerminal = result.VarisNoktasi,
                DepartureDate = result.DepartureDate,
                DepartureTime = departureTime,
                Price = result.Price,
                AvailableSeats = result.BosKoltukSayisi,
                BusModel = result.AracModeli ?? string.Empty,
                LayoutType = result.KoltukDuzeni
            };
        }
    }
}
