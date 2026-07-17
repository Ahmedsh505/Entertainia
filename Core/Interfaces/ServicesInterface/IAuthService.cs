using Core.Models;

namespace Core.Interfaces.ServicesInterface
{
    /// <summary>
    /// Service interface for Authentication operations
    /// </summary>
    public interface IAuthService
    {
        // ===== REGISTRATION =====
        Task<(bool Success, string Message, User? User)> RegisterAsync(
            string email,
            string username,
            string password,
            string firstName,
            string lastName,
            string role,
            string? address = null,
            string? phoneNumber = null,
            string? ipAddress = null);

        // ===== LOGIN =====
        Task<(bool Success, string Message, User? User, string? AccessToken, string? RefreshToken)> LoginAsync(
            string emailOrUsername,
            string password,
            string ipAddress,
            string? deviceInfo = null);

        // ===== 2FA =====
        Task<(bool Success, string Message)> Enable2FAAsync(int userId, string ipAddress);
        Task<(bool Success, string Message)> Disable2FAAsync(int userId, string ipAddress);
        Task<(bool Success, string Message, string? Code)> Send2FACodeAsync(int userId, string ipAddress);
        Task<(bool Success, string Message)> Verify2FACodeAsync(int userId, string code);

        // ===== EMAIL CONFIRMATION =====
        // ✅ NEW: Generate 6-digit verification code instead of URL token
        Task<(bool Success, string Message, string? Code)> GenerateEmailConfirmationCodeAsync(
            int userId,
            string ipAddress);

        // ✅ NEW: Verify 6-digit code
        Task<(bool Success, string Message)> VerifyEmailConfirmationCodeAsync(
            int userId,
            string code,
            string ipAddress);

        // ✅ NEW: Resend confirmation code with rate limiting
        Task<(bool Success, string Message, string? Code)> ResendEmailConfirmationCodeAsync(
            int userId,
            string ipAddress);

        // ⚠️ DEPRECATED: Keep for backward compatibility with old URL-based tokens
        Task<(bool Success, string Message)> ConfirmEmailAsync(string token);

        // ===== PASSWORD RESET =====
        Task<(bool Success, string Message, string? Token)> GeneratePasswordResetTokenAsync(
            string email,
            string ipAddress);

        Task<(bool Success, string Message)> ResetPasswordAsync(
            string email,
            string token,
            string newPassword);
        Task<string?> GetEmailFromPasswordResetTokenAsync(string token);

        // ===== TOKEN MANAGEMENT =====
        Task<(bool Success, string Message, string? AccessToken)> RefreshAccessTokenAsync(
            string refreshToken,
            string ipAddress);

        Task<(bool Success, string Message)> RevokeRefreshTokenAsync(string refreshToken);
        Task<(bool Success, string Message)> RevokeAllUserTokensAsync(int userId);

        // ===== LOGOUT =====
        Task<(bool Success, string Message)> LogoutAsync(int userId, string? refreshToken = null);

        // ===== VALIDATION =====
        Task<bool> ValidatePasswordResetTokenAsync(string email, string token);
        Task<bool> ValidateRefreshTokenAsync(string refreshToken);
    }
}