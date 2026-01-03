using RayBus.Models.Entities;

namespace RayBus.Repositories
{
    /// <summary>
    /// Otobüs repository implementation
    /// Trip tablosundan Bus tipindeki seferleri getirir
    /// </summary>
    public class BusRepository : IBusRepository
    {
        private readonly ITripRepository _tripRepository;

        public BusRepository(ITripRepository tripRepository)
        {
            _tripRepository = tripRepository;
        }

        public async Task<IEnumerable<Trip>> GetAllAsync()
        {
            return await _tripRepository.GetByVehicleTypeAsync("Bus");
        }

        public async Task<Trip?> GetByIdAsync(int id)
        {
            var trip = await _tripRepository.GetByIdAsync(id);
            if (trip?.Vehicle?.VehicleType == "Bus")
                return trip;
            return null;
        }

        public async Task<IEnumerable<Trip>> SearchAsync(int fromCityId, int toCityId, DateTime date)
        {
            return await _tripRepository.SearchAsync(fromCityId, toCityId, date, "Bus");
        }
    }
}
