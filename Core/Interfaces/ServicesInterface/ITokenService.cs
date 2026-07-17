using Core.Models;
using Core.Models.AuthModels;
using System.Security.Claims;

namespace Core.Interfaces.ServicesInterface
{
    /// <summary>
    /// Service for generating and validating JWT tokens
    /// </summary>
    public interface ITokenService
    {
        /// <summary>
        /// Generate JWT access token for a user
        /// </summary>
        string GenerateAccessToken(User user, IList<string> roles);

        /// <summary>
        /// Generate refresh token (cryptographically secure random string)
        /// </summary>
        RefreshToken GenerateRefreshToken(int userId, string ipAddress, string? deviceInfo = null);

        /// <summary>
        /// Validate JWT token and extract claims
        /// </summary>
        ClaimsPrincipal? ValidateToken(string token);

        /// <summary>
        /// Generate email confirmation token (URL-safe)
        /// </summary>
        EmailConfirmationToken GenerateEmailConfirmationToken(int userId, string email, string ipAddress);

        /// <summary>
        /// Generate password reset token (URL-safe)
        /// </summary>
        PasswordResetToken GeneratePasswordResetToken(int userId, string email, string ipAddress);

        /// <summary>
        /// Generate 6-digit 2FA code
        /// </summary>
        TwoFactorCode Generate2FACode(int userId, string ipAddress);

        /// <summary>
        /// Get user ID from JWT token
        /// </summary>
        int? GetUserIdFromToken(string token);
    }
}