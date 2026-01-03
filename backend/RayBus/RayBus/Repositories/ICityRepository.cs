using RayBus.Models.Entities;

namespace RayBus.Repositories
{
    public interface ICityRepository
    {
        Task<IEnumerable<City>> GetAllAsync();
        Task<City?> GetByIdAsync(int id);
        Task<City?> GetByNameAsync(string cityName);
    }
}

