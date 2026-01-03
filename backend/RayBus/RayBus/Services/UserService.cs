using Microsoft.EntityFrameworkCore;
using RayBus.Data;
using RayBus.Models.DTOs;
using RayBus.Models.Entities;
using RayBus.Repositories;

namespace RayBus.Services
{
    public class UserService : IUserService
    {
        private readonly RayBusDbContext _context;
        private readonly ILogger<UserService> _logger;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtService _jwtService;
        private readonly IUserRepository _userRepository;

        public UserService(
            RayBusDbContext context, 
            ILogger<UserService> logger,
            IPasswordHasher passwordHasher,
            IJwtService jwtService,
            IUserRepository userRepository)
        {
            _context = context;
            _logger = logger;
            _passwordHasher = passwordHasher;
            _jwtService = jwtService;
            _userRepository = userRepository;
        }

        public async Task<ApiResponse<UserDTO>> LoginAsync(LoginDTO loginDto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(loginDto.Email))
                {
                    return ApiResponse<UserDTO>.ErrorResponse("Email gereklidir");
                }

                if (string.IsNullOrWhiteSpace(loginDto.Password))
                {
                    return ApiResponse<UserDTO>.ErrorResponse("Şifre gereklidir");
                }

                // Stored procedure ile kullanıcıyı bul
                var (success, userID, fullName, roleID, roleName, passwordHash, phone, createdAt, errorMessage) = 
                    await _userRepository.LoginUserUsingStoredProcedureAsync(loginDto.Email.Trim().ToLower());

                if (!success || userID == null)
                {
                    return ApiResponse<UserDTO>.ErrorResponse(errorMessage ?? "Kullanıcı bulunamadı");
                }

                // Şifre kontrolü (BCrypt ile)
                if (string.IsNullOrWhiteSpace(passwordHash) || !_passwordHasher.VerifyPassword(loginDto.Password, passwordHash))
                {
                    return ApiResponse<UserDTO>.ErrorResponse("Email veya şifre hatalı");
                }

                var userDto = new UserDTO
                {
                    UserID = userID.Value,
                    RoleID = roleID ?? 0,
                    RoleName = roleName ?? string.Empty,
                    FullName = fullName,
                    Email = loginDto.Email.Trim().ToLower(),
                    Phone = phone ?? string.Empty,
                    CreatedAt = createdAt ?? DateTime.UtcNow,
                    Token = _jwtService.GenerateToken(new UserDTO
                    {
                        UserID = userID.Value,
                        RoleID = roleID ?? 0,
                        RoleName = roleName ?? string.Empty,
                        Email = loginDto.Email.Trim().ToLower(),
                        FullName = fullName
                    })
                };

                return ApiResponse<UserDTO>.SuccessResponse(userDto, "Giriş başarılı");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Giriş yapılırken hata oluştu");
                return ApiResponse<UserDTO>.ErrorResponse(
                    "Giriş yapılırken bir hata oluştu",
                    new List<string> { ex.Message }
                );
            }
        }

        public async Task<ApiResponse<UserDTO>> RegisterAsync(RegisterDTO registerDto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(registerDto.FullName))
                {
                    return ApiResponse<UserDTO>.ErrorResponse("Ad Soyad gereklidir");
                }

                if (string.IsNullOrWhiteSpace(registerDto.Email))
                {
                    return ApiResponse<UserDTO>.ErrorResponse("Email gereklidir");
                }

                if (string.IsNullOrWhiteSpace(registerDto.Password) || registerDto.Password.Length < 6)
                {
                    return ApiResponse<UserDTO>.ErrorResponse("Şifre en az 6 karakter olmalıdır");
                }

                // Telefon zorunlu kontrolü
                if (string.IsNullOrWhiteSpace(registerDto.Phone))
                {
                    return ApiResponse<UserDTO>.ErrorResponse("Telefon numarası gereklidir");
                }

                // Rol belirleme
                string roleName = "Müşteri"; // Varsayılan
                if (!string.IsNullOrWhiteSpace(registerDto.RoleName))
                {
                    var roleNameInput = registerDto.RoleName.Trim();
                    if (roleNameInput == "Kullanıcı" || roleNameInput == "Müşteri")
                    {
                        roleName = "Müşteri";
                    }
                    else if (roleNameInput == "Şirket")
                    {
                        roleName = "Şirket";
                    }
                }

                // Şifreyi BCrypt ile hashle
                var passwordHash = _passwordHasher.HashPassword(registerDto.Password);

                // Stored procedure ile kullanıcı kaydı
                var (success, userID, errorMessage) = await _userRepository.RegisterUserUsingStoredProcedureAsync(
                    registerDto.FullName.Trim(),
                    registerDto.Email.Trim().ToLower(),
                    passwordHash,
                    registerDto.Phone.Trim(),
                    roleName
                );

                if (!success || userID <= 0)
                {
                    return ApiResponse<UserDTO>.ErrorResponse(errorMessage ?? "Kayıt işlemi başarısız");
                }

                // Rol bilgisini al (JWT token için)
                var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName);
                if (role == null)
                {
                    _logger.LogWarning("Kullanıcı kaydedildi ama rol bulunamadı: {RoleName}", roleName);
                }

                var userDto = new UserDTO
                {
                    UserID = userID,
                    RoleID = role?.RoleID ?? 0,
                    RoleName = role?.RoleName ?? roleName,
                    FullName = registerDto.FullName.Trim(),
                    Email = registerDto.Email.Trim().ToLower(),
                    Phone = registerDto.Phone.Trim(),
                    CreatedAt = DateTime.UtcNow,
                    Token = _jwtService.GenerateToken(new UserDTO
                    {
                        UserID = userID,
                        RoleID = role?.RoleID ?? 0,
                        RoleName = role?.RoleName ?? roleName,
                        Email = registerDto.Email.Trim().ToLower(),
                        FullName = registerDto.FullName.Trim()
                    })
                };

                return ApiResponse<UserDTO>.SuccessResponse(userDto, "Kayıt başarılı");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kayıt yapılırken hata oluştu");
                return ApiResponse<UserDTO>.ErrorResponse(
                    "Kayıt yapılırken bir hata oluştu",
                    new List<string> { ex.Message }
                );
            }
        }

        public async Task<ApiResponse<UserDTO>> GetUserByIdAsync(int id)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.UserID == id && u.Status == 1);

                if (user == null)
                {
                    return ApiResponse<UserDTO>.ErrorResponse("Kullanıcı bulunamadı");
                }

                var userDto = new UserDTO
                {
                    UserID = user.UserID,
                    RoleID = user.RoleID,
                    RoleName = user.Role?.RoleName ?? string.Empty,
                    FullName = user.FullName,
                    Email = user.Email,
                    Phone = user.Phone ?? string.Empty,
                    CreatedAt = user.CreatedAt
                };

                return ApiResponse<UserDTO>.SuccessResponse(userDto, "Kullanıcı bilgileri getirildi");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı bilgileri getirilirken hata oluştu");
                return ApiResponse<UserDTO>.ErrorResponse(
                    "Kullanıcı bilgileri getirilirken bir hata oluştu",
                    new List<string> { ex.Message }
                );
            }
        }

        public async Task<ApiResponse<UserDTO>> UpdateProfileAsync(int userId, UpdateProfileDTO updateDto)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.UserID == userId && u.Status == 1);

                if (user == null)
                {
                    return ApiResponse<UserDTO>.ErrorResponse("Kullanıcı bulunamadı");
                }

                // Email değişikliği kontrolü
                if (!string.IsNullOrWhiteSpace(updateDto.Email) && updateDto.Email.Trim().ToLower() != user.Email.ToLower())
                {
                    var emailExists = await _context.Users
                        .AnyAsync(u => u.Email.ToLower() == updateDto.Email.Trim().ToLower() && u.UserID != userId);

                    if (emailExists)
                    {
                        return ApiResponse<UserDTO>.ErrorResponse("Bu email adresi zaten kullanılıyor");
                    }
                }

                // Şifre değişikliği
                if (!string.IsNullOrWhiteSpace(updateDto.NewPassword))
                {
                    if (string.IsNullOrWhiteSpace(updateDto.CurrentPassword))
                    {
                        return ApiResponse<UserDTO>.ErrorResponse("Mevcut şifrenizi girmelisiniz");
                    }

                    if (!_passwordHasher.VerifyPassword(updateDto.CurrentPassword, user.PasswordHash))
                    {
                        return ApiResponse<UserDTO>.ErrorResponse("Mevcut şifre hatalı");
                    }

                    if (updateDto.NewPassword.Length < 6)
                    {
                        return ApiResponse<UserDTO>.ErrorResponse("Yeni şifre en az 6 karakter olmalıdır");
                    }

                    if (_passwordHasher.VerifyPassword(updateDto.NewPassword, user.PasswordHash))
                    {
                        return ApiResponse<UserDTO>.ErrorResponse("Yeni şifre mevcut şifre ile aynı olamaz");
                    }

                    user.PasswordHash = _passwordHasher.HashPassword(updateDto.NewPassword);
                }

                // Profil bilgilerini güncelle
                if (!string.IsNullOrWhiteSpace(updateDto.FullName))
                {
                    user.FullName = updateDto.FullName.Trim();
                }

                if (!string.IsNullOrWhiteSpace(updateDto.Email))
                {
                    user.Email = updateDto.Email.Trim().ToLower();
                }

                if (!string.IsNullOrWhiteSpace(updateDto.Phone))
                {
                    user.Phone = updateDto.Phone.Trim();
                }

                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                // Rol bilgisini yükle
                await _context.Entry(user).Reference(u => u.Role).LoadAsync();

                var userDto = new UserDTO
                {
                    UserID = user.UserID,
                    RoleID = user.RoleID,
                    RoleName = user.Role?.RoleName ?? string.Empty,
                    FullName = user.FullName,
                    Email = user.Email,
                    Phone = user.Phone ?? string.Empty,
                    CreatedAt = user.CreatedAt,
                    Token = _jwtService.GenerateToken(new UserDTO
                    {
                        UserID = user.UserID,
                        RoleID = user.RoleID,
                        RoleName = user.Role?.RoleName ?? string.Empty,
                        Email = user.Email,
                        FullName = user.FullName
                    })
                };

                return ApiResponse<UserDTO>.SuccessResponse(userDto, "Profil başarıyla güncellendi");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Profil güncellenirken hata oluştu");
                return ApiResponse<UserDTO>.ErrorResponse(
                    "Profil güncellenirken bir hata oluştu",
                    new List<string> { ex.Message }
                );
            }
        }
    }
}

