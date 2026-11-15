using System.Text;
using System.Threading.RateLimiting;
using Bl;
using Bl.Api;
using Bl.Models;
using Dal;
using Dal.Api;
using Dal.Models;
using Dal.Seed;
using Dal.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Server.Auth;
using Server.Errors;

namespace Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddDbContext<DataManager>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
            builder.Services.AddScoped<IRentals, RentalService>();
            builder.Services.AddScoped<IRentalDetails, RentalDetailsService>();
            builder.Services.AddScoped<IGownModel, GownModelService>();
            builder.Services.AddScoped<IGownDetails, GownDetailsService>();
            builder.Services.AddScoped<IDal, DalManager>();
            builder.Services.AddScoped<IBl, BLManager>();
            builder.Services.AddScoped<Dal.Api.IAppointment, Dal.Services.AppointmentService>();
            builder.Services.AddScoped<Dal.Api.ICustomer, Dal.Services.CustomerService>();
            builder.Services.AddScoped<Dal.Api.IUser, Dal.Services.UserService>();

            // Authentication: clients sign in once and then send the returned JWT as "Authorization: Bearer <token>".
            var jwt = builder.Configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();
            if (Encoding.UTF8.GetByteCount(jwt.Key ?? string.Empty) < JwtSettings.MinKeyBytes)
            {
                throw new InvalidOperationException(
                    $"Jwt:Key is missing or shorter than {JwtSettings.MinKeyBytes} bytes. " +
                    "Set it with 'dotnet user-secrets' or the Jwt__Key environment variable.");
            }
            builder.Services.AddSingleton(jwt);
            builder.Services.AddSingleton<ITokenService, TokenService>();
            builder.Services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    // Keep claim names as written in the token ("name", "role") instead of remapping them to long URIs.
                    options.MapInboundClaims = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwt.Issuer,
                        ValidateAudience = true,
                        ValidAudience = jwt.Audience,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key!)),
                        ClockSkew = TimeSpan.FromMinutes(1),
                        NameClaimType = ClaimNames.Name,
                        RoleClaimType = ClaimNames.Role,
                    };
                });

            // Secure by default: every endpoint requires a signed-in secretary unless it is marked [AllowAnonymous].
            builder.Services.AddAuthorization(options =>
            {
                options.FallbackPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .RequireRole(Roles.Secretary)
                    .Build();
            });

            // Slow down password guessing: a limited number of login attempts per minute per client address.
            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.AddPolicy("login", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        _ => new FixedWindowRateLimiterOptions
                        {
                            // Read when the client's counter is created, so the limit follows the configuration.
                            PermitLimit = httpContext.RequestServices.GetRequiredService<IConfiguration>()
                                .GetValue("RateLimit:LoginPerMinute", 5),
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0,
                        }));
            });

            // Every error leaves the API as the same standard JSON shape (see ApiExceptionHandler).
            builder.Services.AddProblemDetails();
            builder.Services.AddExceptionHandler<ApiExceptionHandler>();

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "Sign in with POST /api/Auth/login, then paste the returned token here.",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                });
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
                        },
                        Array.Empty<string>()
                    },
                });
            });

            var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                                 ?? new[] { "http://localhost:3000" };
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowReact", policy =>
                {
                    policy.WithOrigins(allowedOrigins)
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });

            var app = builder.Build();

            // In development the database is created and filled with demo data on startup.
            if (app.Environment.IsDevelopment())
            {
                using var scope = app.Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<DataManager>();
                db.Database.Migrate();
                DemoDataSeeder.Seed(db, DateOnly.FromDateTime(DateTime.Today));

                // Demo secretary account, defined under "Seed:Secretary" in appsettings.Development.json.
                var seedUsername = builder.Configuration["Seed:Secretary:Username"];
                var seedPassword = builder.Configuration["Seed:Secretary:Password"];
                if (!string.IsNullOrWhiteSpace(seedUsername) && !string.IsNullOrEmpty(seedPassword))
                {
                    scope.ServiceProvider.GetRequiredService<IBl>().BLAuth
                        .EnsureUser(seedUsername, seedPassword, Roles.Secretary);
                }
            }

            // Configure the HTTP request pipeline.
            app.UseExceptionHandler();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseCors("AllowReact");

            app.UseHttpsRedirection();

            app.UseRateLimiter();

            // Authentication reads the token and works out who the caller is; authorization decides what they may do.
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
