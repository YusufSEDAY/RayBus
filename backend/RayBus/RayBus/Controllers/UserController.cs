using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RayBus.Data;
using RayBus.Models.DTOs;
using RayBus.Models.Entities;
using RayBus.Services;

namespace RayBus.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly RayBusDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtService _jwtService;
        private readonly ILogger<UserController> _logger;

        public UserController(
            IUserService userService,
            RayBusDbContext context,
            IPasswordHasher passwordHasher,
            IJwtService jwtService,
            ILogger<UserController> logger)
        {
            _userService = userService;
            _context = context;
            _passwordHasher = passwordHasher;
            _jwtService = jwtService;
            _logger = logger;
        }

        /// <summary>
        /// Kullanıcı girişi
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO loginDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var response = await _userService.LoginAsync(loginDto);
            
            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        /// <summary>
        /// Admin kullanıcısı oluşturur (Sadece Swagger'da görünür)
        /// </summary>
        [HttpPost("create-admin")]
        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        public async Task<IActionResult> CreateAdmin([FromBody] RegisterDTO registerDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Telefon kontrolü
            if (string.IsNullOrWhiteSpace(registerDto.Phone))
            {
                return BadRequest(new ApiResponse<UserDTO>
                {
                    Success = false,
                    Message = "Telefon numarası gereklidir"
                });
            }

            // Admin rolünü bul
            var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName.ToLower() == "admin");
            if (adminRole == null)
            {
                return BadRequest(new ApiResponse<UserDTO>
                {
                    Success = false,
                    Message = "Admin rolü bulunamadı"
                });
            }

            // Email kontrolü
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == registerDto.Email.Trim().ToLower());

            if (existingUser != null)
            {
                return BadRequest(new ApiResponse<UserDTO>
                {
                    Success = false,
                    Message = "Bu email adresi zaten kullanılıyor"
                });
            }

            // Admin kullanıcısı oluştur
            var user = new User
            {
                RoleID = adminRole.RoleID,
                FullName = registerDto.FullName.Trim(),
                Email = registerDto.Email.Trim().ToLower(),
                PasswordHash = _passwordHasher.HashPassword(registerDto.Password),
                Phone = registerDto.Phone.Trim(),
                Status = 1,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var userDto = new UserDTO
            {
                UserID = user.UserID,
                RoleID = user.RoleID,
                RoleName = adminRole.RoleName,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone ?? string.Empty,
                CreatedAt = user.CreatedAt,
                Token = _jwtService.GenerateToken(new UserDTO
                {
                    UserID = user.UserID,
                    RoleID = user.RoleID,
                    RoleName = adminRole.RoleName,
                    Email = user.Email,
                    FullName = user.FullName
                })
            };

            _logger.LogInformation("Admin kullanıcısı oluşturuldu: {Email}", user.Email);

            return Ok(new ApiResponse<UserDTO>
            {
                Success = true,
                Message = "Admin kullanıcısı başarıyla oluşturuldu",
                Data = userDto
            });
        }

        /// <summary>
        /// Kullanıcı kaydı
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDTO registerDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var response = await _userService.RegisterAsync(registerDto);
            
            if (!response.Success)
            {
                return BadRequest(response);
            }

            return CreatedAtAction(nameof(GetUser), new { id = response.Data?.UserID }, response);
        }

        /// <summary>
        /// Kullanıcı bilgilerini getirir
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var response = await _userService.GetUserByIdAsync(id);
            
            if (!response.Success)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        /// <summary>
        /// Kullanıcı profilini günceller
        /// </summary>
        [HttpPut("{id:int}/profile")]
        // JWT kaldırıldı - Test projesi için güvenlik devre dışı
        // [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> UpdateProfile(int id, [FromBody] UpdateProfileDTO updateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var response = await _userService.UpdateProfileAsync(id, updateDto);
            
            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
    }
}

