using AspNetCoreRateLimit;
using Core.Interfaces.RepositoriesInterface;
using Core.Interfaces.ServicesInterface;
using Core.Interfaces.UnitOfWorkInterface;
using Core.Models;
using Entertania1.Services;
using Infrastructure.Data.DbContexts;
using Infrastructure.Data.SeedData;
using Infrastructure.Data.UnitOfWork;
using Infrastructure.Repositories;
using Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Service.BackgroundServices;
using Service.Services;
using System.Text;

namespace Entertania1
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // 1. Add DbContext
            builder.Services.AddDbContext<EntertaniaDbcontext>(options =>
            {
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("DefaultConnection"),
                    sqlOptions =>
                    {
                        sqlOptions.CommandTimeout(60); // 60 second timeout
                        sqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 3,
                            maxRetryDelay: TimeSpan.FromSeconds(5),
                            errorNumbersToAdd: null);
                    });

                // Only enable for debugging (impacts performance)
                // options.EnableSensitiveDataLogging();

                // Only log warnings and errors (not all SQL queries)
                options.LogTo(Console.WriteLine, LogLevel.Warning);
            });

            // 2. Add Identity
            builder.Services.AddIdentity<User, IdentityRole<int>>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 6;
                options.SignIn.RequireConfirmedEmail = false;
            })
            .AddEntityFrameworkStores<EntertaniaDbcontext>()
            .AddDefaultTokenProviders();

            // 3. Add JWT Authentication
            var jwtSettings = builder.Configuration.GetSection("Jwt");
            var secretKey = jwtSettings["SecretKey"];

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
                options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidateAudience = true,
                    ValidAudience = jwtSettings["Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

            // Add HttpClient for external APIs
            builder.Services.AddHttpClient();

            // Add Memory Cache
            builder.Services.AddMemoryCache();

            builder.Services.Configure<IpRateLimitOptions>(options =>
            {
                options.GeneralRules = new List<RateLimitRule>
                {
                    new RateLimitRule
                    {
                        Endpoint = "*/Account/ConfirmEmail",
                        Limit = 5,
                        Period = "1h"
                    },
                    new RateLimitRule
                    {
                        Endpoint = "*/Account/ResetPassword",
                        Limit = 3,
                        Period = "15m"
                    }
                };
            });

            // 4. Configure SendGrid Settings
            builder.Services.Configure<SendGridSettings>(
                builder.Configuration.GetSection("SendGridSettings"));

            // ✅ 5. Register SERVICE LAYER Services (Business Logic)
            // ⚠️ IMPORTANT: Order matters! Register dependencies before services that use them

            // Core services without dependencies
            builder.Services.AddScoped<ITokenService, TokenService>();
            builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Services.AddSingleton<IEgyptTimeService, EgyptTimeService>();
            builder.Services.AddScoped<ICurrencyConversionService, CurrencyConversionService>();
            builder.Services.AddScoped<IFileStorageService, FileStorageService>();
            builder.Services.AddScoped<IPhotoGalleryService, PhotoGalleryService>();
            builder.Services.AddScoped<IEventPhotoRepo, EventPhotoRepository>();
            builder.Services.AddScoped<IVenuePhotoRepo, VenuePhotoRepository>();
            builder.Services.AddScoped<ITicketRepo, TicketRepository>();
            builder.Services.AddScoped<IReviewRepo, ReviewRepository>();

            // ===== NEW DASHBOARD SERVICES =====
            builder.Services.AddScoped<IAdminDashboardService, AdminDashboardService>();
            builder.Services.AddScoped<IOrganizerDashboardService, OrganizerDashboardService>();

            // ===== ✅ NEW: TRANSACTION AND REFUND SERVICES =====
            // Transaction service (no dependencies on other business services)
            builder.Services.AddScoped<ITransactionService, TransactionService>();

            // Paymob refund service (no dependencies on other business services)
            builder.Services.AddScoped<IPaymobRefundService, PaymobRefundService>();

            // Payment history service (deprecated but kept for compatibility)
            builder.Services.AddScoped<IPaymentHistoryService, PaymentHistoryService>();

            // Payment service (depends on TransactionService)
            builder.Services.AddScoped<IPaymentService, PaymentService>();

            // Booking service (depends on EmailService, TransactionService)
            builder.Services.AddScoped<IBookingService, BookingService>();

            // Refund service (depends on EmailService, TransactionService, PaymobRefundService, PaymentService)
            builder.Services.AddScoped<IRefundService, RefundService>();
            builder.Services.AddScoped<ITicketService, TicketService>();

            // Other services
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IEventService, EventService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IAdminService, AdminService>();
            builder.Services.AddScoped<IVenueService, VenueService>();

            // ✅ 6. Register UI LAYER Services
            builder.Services.AddScoped<IFileService, Entertania1.Services.FileService>();

            // 7. Repositories + Unit of Work
            builder.Services.AddScoped<IBannedEmailRepo, BannedEmailRepository>();
            builder.Services.AddScoped<IUserRepo, UserRepository>();
            builder.Services.AddScoped<IEventRepo, EventRepository>();
            builder.Services.AddScoped<IVenueRepo, VenueRepository>();
            builder.Services.AddScoped<IConcertRepo, ConcertRepository>();
            builder.Services.AddScoped<ICulturalEventRepo, CulturalEventRepository>();
            builder.Services.AddScoped<ISportEventRepo, SportEventRepository>();
            builder.Services.AddScoped<ITheaterPlayRepo, TheaterPlayRepository>();
            builder.Services.AddScoped<ITourismActivityRepo, TourismActivityRepository>();
            builder.Services.AddScoped<IBookingRepo, BookingRepository>();
            builder.Services.AddScoped<IPaymentRepo, PaymentRepository>();
            builder.Services.AddScoped<IRefundRepo, RefundRepository>();
            builder.Services.AddScoped<IPaymentHistoryRepository, PaymentHistoryRepository>();
            builder.Services.AddScoped<IEventTicketTierRepo, EventTicketTierRepository>();
            builder.Services.AddScoped<IDiscountCodeRepo, DiscountCodeRepository>();
            builder.Services.AddScoped<IAuthRepository, AuthRepository>();
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

            // ✅ 8. Background Services
            builder.Services.AddHostedService<Service.BackgroundServices.AbandonedBookingCleanupService>();
            builder.Services.AddHostedService<TokenCleanupService>();

            // 9. Logging
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();

            // 10. Configure cookie authentication
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Account/Login";
                options.AccessDeniedPath = "/Account/AccessDenied";
                options.ExpireTimeSpan = TimeSpan.FromDays(7);
                options.SlidingExpiration = true;
            });

            // 11. Authorization Policies
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("ActiveOrganizerOrAdmin", policy =>
                {
                    policy.Requirements.Add(new ActiveOrganizerRequirement());
                });

                options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
                options.AddPolicy("OrganizerOnly", policy => policy.RequireRole("Organizer"));
                options.AddPolicy("CustomerOnly", policy => policy.RequireRole("Customer"));
                options.AddPolicy("EmailConfirmed", policy =>
                    policy.RequireClaim("EmailConfirmed", "True"));
            });

            builder.Services.AddScoped<IAuthorizationHandler, ActiveOrganizerHandler>();

            var app = builder.Build();

            // --- Seed Roles + Admins ---
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var config = services.GetRequiredService<IConfiguration>();
                await RoleSeeder.SeedRolesAndAdminsAsync(services, config);
            }

            // Middleware
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            // MVC routes
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}