using RayBus.Models.DTOs;

namespace RayBus.Services
{
    public interface IJwtService
    {
        string GenerateToken(UserDTO user);
        bool ValidateToken(string token);
    }
}

