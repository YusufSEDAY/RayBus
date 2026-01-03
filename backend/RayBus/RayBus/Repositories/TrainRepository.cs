using RayBus.Data;
using RayBus.Models.Entities;

namespace RayBus.Repositories
{
    /// <summary>
    /// Tren repository implementation
    /// Trip tablosundan Train tipindeki seferleri getirir
    /// </summary>
    public class TrainRepository : ITrainRepository
    {
        private readonly ITripRepository _tripRepository;

        public TrainRepository(ITripRepository tripRepository)
        {
            _tripRepository = tripRepository;
        }

        public async Task<IEnumerable<Trip>> GetAllAsync()
        {
            return await _tripRepository.GetByVehicleTypeAsync("Train");
        }

        public async Task<Trip?> GetByIdAsync(int id)
        {
            var trip = await _tripRepository.GetByIdAsync(id);
            if (trip?.Vehicle?.VehicleType == "Train")
                return trip;
            return null;
        }

        public async Task<IEnumerable<Trip>> SearchAsync(int fromCityId, int toCityId, DateTime date)
        {
            return await _tripRepository.SearchAsync(fromCityId, toCityId, date, "Train");
        }
    }
}
