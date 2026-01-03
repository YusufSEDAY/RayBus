using Microsoft.EntityFrameworkCore;
using RayBus.Data;
using RayBus.Models.Entities;

namespace RayBus.Repositories
{
    public class CityRepository : ICityRepository
    {
        private readonly RayBusDbContext _context;

        public CityRepository(RayBusDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<City>> GetAllAsync()
        {
            return await _context.Cities
                .OrderBy(c => c.CityName)
                .ToListAsync();
        }

        public async Task<City?> GetByIdAsync(int id)
        {
            return await _context.Cities.FindAsync(id);
        }

        public async Task<City?> GetByNameAsync(string cityName)
        {
            return await _context.Cities
                .FirstOrDefaultAsync(c => EF.Functions.Like(c.CityName, cityName));
        }
    }
}

