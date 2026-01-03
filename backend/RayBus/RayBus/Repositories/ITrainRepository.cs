using RayBus.Models.Entities;

namespace RayBus.Repositories
{
    /// <summary>
    /// Tren repository interface'i
    /// Trip tablosundan Train tipindeki seferleri getirir
    /// </summary>
    public interface ITrainRepository
    {
        Task<IEnumerable<Trip>> GetAllAsync();
        Task<Trip?> GetByIdAsync(int id);
        Task<IEnumerable<Trip>> SearchAsync(int fromCityId, int toCityId, DateTime date);
    }
}


