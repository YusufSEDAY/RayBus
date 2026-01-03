using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace RayBus.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AuthorizeRoleAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private readonly string[] _allowedRoles;

        public AuthorizeRoleAttribute(params string[] allowedRoles)
        {
            _allowedRoles = allowedRoles;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            // [Authorize] attribute'u authentication'ı zaten yapıyor
            // Burada sadece role kontrolü yapıyoruz
            var user = context.HttpContext.User;
            var isAuthenticated = user.Identity?.IsAuthenticated ?? false;

            // Eğer authentication yapılmamışsa, [Authorize] zaten 401 döndürmüştür
            // Bu durumda buraya gelmemeli, ama yine de kontrol ediyoruz
            if (!isAuthenticated)
            {
                // [Authorize] zaten 401 döndürmüş olmalı, buraya gelmemeli
                return;
            }

            // Role kontrolü
            if (_allowedRoles != null && _allowedRoles.Length > 0)
            {
                var userRole = user.FindFirst(ClaimTypes.Role)?.Value;

                if (string.IsNullOrEmpty(userRole) || !_allowedRoles.Contains(userRole))
                {
                    context.Result = new ObjectResult(new
                    {
                        Success = false,
                        Message = "Bu işlem için yetkiniz yok",
                        Errors = new[] { $"Bu işlem için {string.Join(" veya ", _allowedRoles)} rolü gereklidir. Mevcut rolünüz: {userRole ?? "Belirtilmemiş"}" }
                    })
                    {
                        StatusCode = 403
                    };
                    return;
                }
            }
            
            await Task.CompletedTask;
        }
    }
}

