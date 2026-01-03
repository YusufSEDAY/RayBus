using Microsoft.EntityFrameworkCore;
using RayBus.Data;
using RayBus.Models.DTOs;
using RayBus.Repositories;

namespace RayBus.Services
{
    public class CityService : ICityService
    {
        private readonly ICityRepository _cityRepository;
        private readonly RayBusDbContext _context;
        private readonly ILogger<CityService> _logger;

        public CityService(
            ICityRepository cityRepository,
            RayBusDbContext context,
            ILogger<CityService> logger)
        {
            _cityRepository = cityRepository;
            _context = context;
            _logger = logger;
        }

        public async Task<ApiResponse<IEnumerable<CityDTO>>> GetAllCitiesAsync()
        {
            try
            {
                var cities = await _cityRepository.GetAllAsync();
                var cityDtos = cities.Select(c => new CityDTO
                {
                    CityID = c.CityID,
                    CityName = c.CityName
                }).ToList();

                return ApiResponse<IEnumerable<CityDTO>>.SuccessResponse(
                    cityDtos,
                    "Şehirler başarıyla getirildi"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Şehirler getirilirken hata oluştu");
                return ApiResponse<IEnumerable<CityDTO>>.ErrorResponse(
                    "Şehirler getirilirken bir hata oluştu",
                    new List<string> { ex.Message }
                );
            }
        }

        public async Task<ApiResponse<IEnumerable<StationDTO>>> GetStationsByCityIdAsync(int cityId)
        {
            try
            {
                if (cityId <= 0)
                {
                    return ApiResponse<IEnumerable<StationDTO>>.ErrorResponse("Geçersiz şehir ID'si");
                }

                var stations = await _context.Stations
                    .Include(s => s.City)
                    .Where(s => s.CityID == cityId)
                    .OrderBy(s => s.StationName)
                    .ToListAsync();

                var stationDtos = stations.Select(s => new StationDTO
                {
                    StationID = s.StationID,
                    StationName = s.StationName,
                    CityID = s.CityID,
                    CityName = s.City?.CityName ?? string.Empty
                }).ToList();

                return ApiResponse<IEnumerable<StationDTO>>.SuccessResponse(
                    stationDtos,
                    "İstasyonlar başarıyla getirildi"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İstasyonlar getirilirken hata oluştu. CityID: {CityID}", cityId);
                return ApiResponse<IEnumerable<StationDTO>>.ErrorResponse(
                    "İstasyonlar getirilirken bir hata oluştu",
                    new List<string> { ex.Message }
                );
            }
        }

        public async Task<ApiResponse<IEnumerable<TerminalDTO>>> GetTerminalsByCityIdAsync(int cityId)
        {
            try
            {
                if (cityId <= 0)
                {
                    return ApiResponse<IEnumerable<TerminalDTO>>.ErrorResponse("Geçersiz şehir ID'si");
                }

                var terminals = await _context.Terminals
                    .Include(t => t.City)
                    .Where(t => t.CityID == cityId)
                    .OrderBy(t => t.TerminalName)
                    .ToListAsync();

                var terminalDtos = terminals.Select(t => new TerminalDTO
                {
                    TerminalID = t.TerminalID,
                    TerminalName = t.TerminalName,
                    CityID = t.CityID,
                    CityName = t.City?.CityName ?? string.Empty
                }).ToList();

                return ApiResponse<IEnumerable<TerminalDTO>>.SuccessResponse(
                    terminalDtos,
                    "Terminaller başarıyla getirildi"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Terminaller getirilirken hata oluştu. CityID: {CityID}", cityId);
                return ApiResponse<IEnumerable<TerminalDTO>>.ErrorResponse(
                    "Terminaller getirilirken bir hata oluştu",
                    new List<string> { ex.Message }
                );
            }
        }
    }
}

