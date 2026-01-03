using RayBus.Models.DTOs;

namespace RayBus.Repositories
{
    /// <summary>
    /// Kullanıcı repository interface'i
    /// </summary>
    public interface IUserRepository
    {
        /// <summary>
        /// Stored procedure kullanarak kullanıcı kaydı yapar
        /// </summary>
        Task<(bool Success, int UserID, string ErrorMessage)> RegisterUserUsingStoredProcedureAsync(
            string fullName, string email, string passwordHash, string phone, string roleName);

        /// <summary>
        /// Stored procedure kullanarak kullanıcı girişi yapar
        /// </summary>
        Task<(bool Success, int? UserID, string FullName, int? RoleID, string? RoleName, string? PasswordHash, string? Phone, DateTime? CreatedAt, string ErrorMessage)> LoginUserUsingStoredProcedureAsync(
            string email);

        /// <summary>
        /// Stored procedure kullanarak admin panel için kullanıcıları getirir
        /// </summary>
        Task<List<AdminUserDTO>> GetAdminUsersUsingStoredProcedureAsync(string? aramaMetni = null, int? rolID = null);

        /// <summary>
        /// Stored procedure kullanarak kullanıcı durumunu değiştirir
        /// </summary>
        Task<(bool Success, string Message)> UpdateUserStatusUsingStoredProcedureAsync(int userID, bool yeniDurum, string? sebep = null);
    }
}

