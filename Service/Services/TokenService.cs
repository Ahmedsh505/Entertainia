using Core.Interfaces.ServicesInterface;
using Core.Models;
using Core.Models.AuthModels;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Service.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        private readonly IEgyptTimeService _timeService;

        public TokenService(IConfiguration configuration, IEgyptTimeService timeService)
        {
            _configuration = configuration;
            _timeService = timeService;
        }

        /// <summary>
        /// Generate JWT access token (15 min expiry)
        /// </summary>
        public string GenerateAccessToken(User user, IList<string> roles)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName ?? user.Email),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("FirstName", user.FirstName ?? ""),
                new Claim("LastName", user.LastName ?? ""),
                new Claim("AccountStatus", user.AccountStatus.ToString()),
                new Claim("EmailConfirmed", user.EmailConfirmed.ToString()),
                new Claim("TwoFactorEnabled", user.TwoFactorEnabled.ToString())
            };

            // Add roles as claims
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]!));

            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(15), // 15 min expiry
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Generate cryptographically secure refresh token (7 days expiry)
        /// </summary>
        public RefreshToken GenerateRefreshToken(int userId, string ipAddress, string? deviceInfo = null)
        {
            using var rng = RandomNumberGenerator.Create();
            var randomBytes = new byte[64];
            rng.GetBytes(randomBytes);

            return new RefreshToken
            {
                Token = Convert.ToBase64String(randomBytes),
                UserId = userId,
                ExpiresAt = DateTime.UtcNow.AddDays(7), // 7 days expiry
                CreatedAt = DateTime.UtcNow,
                CreatedByIp = ipAddress,
                DeviceInfo = deviceInfo
            };
        }

        /// <summary>
        /// Validate JWT token and return ClaimsPrincipal
        /// </summary>
        public ClaimsPrincipal? ValidateToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]!);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = _configuration["Jwt:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
                return principal;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// ✅ UPDATED: Generate email confirmation token with 6-digit code (5 min expiry)
        /// </summary>
        public EmailConfirmationToken GenerateEmailConfirmationToken(int userId, string email, string ipAddress)
        {
            // Generate 6-digit code
            var code = GenerateNumericCode(6);

            return new EmailConfirmationToken
            {
                Code = code,
                Token = null, // No longer using URL tokens
                UserId = userId,
                Email = email,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5), // ✅ 5 min expiry as requested
                CreatedByIp = ipAddress,
                FailedAttempts = 0,
                IsLocked = false,
                ResendCount = 0
            };
        }

        /// <summary>
        /// Generate password reset token (URL-safe, 20 min expiry)
        /// </summary>
        public PasswordResetToken GeneratePasswordResetToken(int userId, string email, string ipAddress)
        {
            var token = GenerateUrlSafeToken();

            return new PasswordResetToken
            {
                Token = token,
                UserId = userId,
                Email = email,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(20), // 20 min expiry
                CreatedByIp = ipAddress
            };
        }

        /// <summary>
        /// Generate 6-digit 2FA code (10 min expiry)
        /// </summary>
        public TwoFactorCode Generate2FACode(int userId, string ipAddress)
        {
            var code = GenerateNumericCode(6);

            return new TwoFactorCode
            {
                Code = code,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10), // 10 min expiry
                CreatedByIp = ipAddress
            };
        }

        /// <summary>
        /// Get user ID from JWT token
        /// </summary>
        public int? GetUserIdFromToken(string token)
        {
            var principal = ValidateToken(token);
            var userIdClaim = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (int.TryParse(userIdClaim, out var userId))
                return userId;

            return null;
        }

        // ===== HELPER METHODS =====

        /// <summary>
        /// Generate URL-safe random token
        /// </summary>
        private string GenerateUrlSafeToken()
        {
            using var rng = RandomNumberGenerator.Create();
            var randomBytes = new byte[32];
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }

        /// <summary>
        /// Generate numeric code (e.g., 6-digit verification code)
        /// </summary>
        private string GenerateNumericCode(int length)
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            var random = BitConverter.ToUInt32(bytes, 0);
            var code = (random % (int)Math.Pow(10, length)).ToString($"D{length}");
            return code;
        }
    }
}