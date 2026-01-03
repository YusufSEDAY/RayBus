using RayBus.Models.DTOs;

namespace RayBus.Services
{
    public interface IUserService
    {
        Task<ApiResponse<UserDTO>> LoginAsync(LoginDTO loginDto);
        Task<ApiResponse<UserDTO>> RegisterAsync(RegisterDTO registerDto);
        Task<ApiResponse<UserDTO>> GetUserByIdAsync(int id);
        Task<ApiResponse<UserDTO>> UpdateProfileAsync(int userId, UpdateProfileDTO updateDto);
    }
}

