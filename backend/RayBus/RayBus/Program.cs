using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new RayBus.Models.DTOs.TimeSpanConverter());
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        policy =>
        {
            policy.WithOrigins(
                    "http://localhost:3000", 
                    "http://localhost:5173", 
                    "http://127.0.0.1:3000", 
                    "http://127.0.0.1:5173",
                    "https://localhost:7049",
                    "http://localhost:5000",
                    "https://localhost:5001"
                  )
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    
    if (builder.Environment.IsDevelopment())
    {
        options.AddPolicy("AllowAll",
            policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
    }
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RayBus API - Tren ve Otobüs Bilet Rezervasyon Sistemi",
        Version = "v1",
        Description = "Tren ve Otobüs bilet rezervasyon sistemi için Web API",
        Contact = new OpenApiContact
        {
            Name = "RayBus Team",
            Email = "info@raybus.com"
        }
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    c.CustomSchemaIds(type => type.FullName);
    
    c.TagActionsBy(api =>
    {
        var controllerName = api.ActionDescriptor.RouteValues["controller"];
        return new[] { controllerName ?? "Default" };
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddDbContext<RayBus.Data.RayBusDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), 
        sqlOptions => sqlOptions.UseRelationalNulls()));

builder.Services.AddScoped<RayBus.Repositories.ITripRepository, RayBus.Repositories.TripRepository>();
builder.Services.AddScoped<RayBus.Repositories.ITrainRepository, RayBus.Repositories.TrainRepository>();
builder.Services.AddScoped<RayBus.Repositories.IBusRepository, RayBus.Repositories.BusRepository>();
builder.Services.AddScoped<RayBus.Repositories.IReservationRepository, RayBus.Repositories.ReservationRepository>();
builder.Services.AddScoped<RayBus.Repositories.ICityRepository, RayBus.Repositories.CityRepository>();
builder.Services.AddScoped<RayBus.Repositories.IUserRepository, RayBus.Repositories.UserRepository>();

builder.Services.AddScoped<RayBus.Services.ITrainService, RayBus.Services.TrainService>();
builder.Services.AddScoped<RayBus.Services.IBusService, RayBus.Services.BusService>();
builder.Services.AddScoped<RayBus.Services.IReservationService, RayBus.Services.ReservationService>();
builder.Services.AddScoped<RayBus.Services.ITripService, RayBus.Services.TripService>();
builder.Services.AddScoped<RayBus.Services.IUserService, RayBus.Services.UserService>();
builder.Services.AddScoped<RayBus.Services.ICityService, RayBus.Services.CityService>();
builder.Services.AddScoped<RayBus.Services.IPasswordHasher, RayBus.Services.PasswordHasher>();
builder.Services.AddScoped<RayBus.Services.IJwtService, RayBus.Services.JwtService>();
builder.Services.AddScoped<RayBus.Services.IAutoCancellationService, RayBus.Services.AutoCancellationService>();
builder.Services.AddScoped<RayBus.Services.IUserStatisticsService, RayBus.Services.UserStatisticsService>();
builder.Services.AddScoped<RayBus.Services.ITicketService, RayBus.Services.TicketService>();
builder.Services.AddScoped<RayBus.Services.INotificationService, RayBus.Services.NotificationService>();
builder.Services.AddScoped<RayBus.Services.IEmailService, RayBus.Services.EmailService>();
builder.Services.AddHostedService<RayBus.Services.NotificationQueueProcessor>();
builder.Services.AddHostedService<RayBus.Services.DynamicPricingService>();

var jwtSecretKey = builder.Configuration["Jwt:SecretKey"] ?? "RayBus_SecretKey_Minimum_32_Characters_Long_For_Security_Purposes_2024";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "RayBus";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "RayBusUsers";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(jwtSecretKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
        RoleClaimType = ClaimTypes.Role
    };
    
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            
            var allHeaders = string.Join(", ", context.Request.Headers.Select(h => $"{h.Key}={h.Value}"));
            logger.LogInformation("🔍 JWT OnMessageReceived - Tüm Header'lar: {Headers}", allHeaders);
            
            var authHeader = context.Request.Headers["Authorization"].ToString();
            logger.LogInformation("🔍 JWT OnMessageReceived - Authorization Header: {AuthHeader}", 
                string.IsNullOrEmpty(authHeader) ? "NULL" : authHeader.Substring(0, Math.Min(50, authHeader.Length)) + "...");
            
            if (string.IsNullOrEmpty(authHeader))
            {
                logger.LogWarning("⚠️ JWT Message Received. AuthHeader: NULL");
                return Task.CompletedTask;
            }
            
            if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var token = authHeader.Substring("Bearer ".Length).Trim();
                context.Token = token;
                logger.LogInformation("✅ JWT Message Received. Token extracted: {Token}...", 
                    token.Length > 20 ? token.Substring(0, 20) : token);
            }
            else
            {
                logger.LogWarning("⚠️ JWT Message Received. AuthHeader format yanlış: {AuthHeader}", 
                    authHeader.Length > 50 ? authHeader.Substring(0, 50) + "..." : authHeader);
            }
            
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            var authHeader = context.Request.Headers["Authorization"].ToString();
            logger.LogError(context.Exception, "❌ JWT Authentication failed. Exception: {Exception}, AuthHeader: {AuthHeader}", 
                context.Exception?.Message ?? "Unknown error",
                string.IsNullOrEmpty(authHeader) ? "NULL" : authHeader.Substring(0, Math.Min(50, authHeader.Length)) + "...");
            
            if (context.Exception != null)
            {
                logger.LogError("❌ JWT Error Details: {ErrorType}, {Message}", 
                    context.Exception.GetType().Name, 
                    context.Exception.Message);
            }
            
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            var user = context.Principal;
            var role = user?.FindFirst(ClaimTypes.Role)?.Value;
            logger.LogInformation("✅ JWT Token validated. User: {User}, Role: {Role}", 
                user?.Identity?.Name, role);
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            var authHeader = context.Request.Headers["Authorization"].ToString();
            var path = context.Request.Path;
            
            logger.LogWarning("⚠️ JWT Challenge triggered. Path: {Path}, Error: {Error}, ErrorDescription: {ErrorDescription}, AuthHeader: {AuthHeader}", 
                path,
                context.Error ?? "NULL", 
                context.ErrorDescription ?? "NULL",
                string.IsNullOrEmpty(authHeader) ? "NULL" : authHeader.Substring(0, Math.Min(50, authHeader.Length)) + "...");
            
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var token = authHeader.Substring("Bearer ".Length).Trim();
                try
                {
                    var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                    var jsonToken = handler.ReadJwtToken(token);
                    logger.LogWarning("🔍 Token içeriği - Issuer: {Issuer}, Audience: {Audience}, Exp: {Exp}, Role: {Role}", 
                        jsonToken.Issuer,
                        jsonToken.Audiences?.FirstOrDefault(),
                        jsonToken.ValidTo,
                        jsonToken.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "❌ Token parse hatası");
                }
            }
            
            context.HandleResponse();
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            var errorMessage = context.ErrorDescription ?? context.Error ?? "Unauthorized";
            var responseObj = new { 
                Success = false, 
                Message = $"JWT Authentication failed: {errorMessage}",
                Error = context.Error ?? "Unknown",
                ErrorDescription = context.ErrorDescription ?? "Token validation failed"
            };
            var responseText = System.Text.Json.JsonSerializer.Serialize(responseObj);
            logger.LogWarning("⚠️ JWT Challenge - Response gönderiliyor: {Response}", responseText);
            return context.Response.WriteAsync(responseText);
        }
    };
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "RayBus API v1");
    c.RoutePrefix = "swagger";
    c.DisplayRequestDuration();
    c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
    c.DefaultModelsExpandDepth(-1);
});

if (app.Environment.IsDevelopment())
{
    app.UseCors("AllowAll");
}
else
{
    app.UseCors("AllowReactApp");
}

app.UseHttpsRedirection();

app.UseRouting();
app.MapControllers();

app.Run();
