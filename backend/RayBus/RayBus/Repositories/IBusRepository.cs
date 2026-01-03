using RayBus.Models.Entities;

namespace RayBus.Repositories
{
    /// <summary>
    /// Otobüs repository interface'i
    /// Trip tablosundan Bus tipindeki seferleri getirir
    /// </summary>
    public interface IBusRepository
    {
        Task<IEnumerable<Trip>> GetAllAsync();
        Task<Trip?> GetByIdAsync(int id);
        Task<IEnumerable<Trip>> SearchAsync(int fromCityId, int toCityId, DateTime date);
    }
}


