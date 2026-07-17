using Core.Models.AuthModels;

namespace Core.Interfaces.RepositoriesInterface
{
    /// <summary>
    /// Repository for all authentication tokens (simplified single repo)
    /// </summary>
    public interface IAuthRepository
    {
        // ===== REFRESH TOKENS =====
        Task AddRefreshTokenAsync(RefreshToken token);
        Task<RefreshToken?> GetRefreshTokenAsync(string token);
        Task<List<RefreshToken>> GetActiveRefreshTokensAsync(int userId);
        Task RevokeRefreshTokenAsync(string token, string ipAddress, string reason);
        Task RevokeAllUserRefreshTokensAsync(int userId, string ipAddress, string reason);
        Task CleanupExpiredRefreshTokensAsync();

        // ===== EMAIL CONFIRMATION TOKENS =====
        Task AddEmailConfirmationTokenAsync(EmailConfirmationToken token);

        // ⚠️ DEPRECATED: For backward compatibility with old URL-based tokens
        Task<EmailConfirmationToken?> GetEmailConfirmationTokenAsync(string token);
        Task MarkEmailConfirmationTokenAsUsedAsync(string token, string ipAddress);

        // ✅ NEW: Code-based email confirmation
        Task<EmailConfirmationToken?> GetEmailConfirmationTokenByCodeAsync(int userId, string code);
        Task<EmailConfirmationToken?> GetLatestEmailConfirmationTokenAsync(int userId);
        Task MarkEmailConfirmationTokenAsUsedByCodeAsync(int userId, string code, string ipAddress);
        Task IncrementEmailConfirmationFailedAttemptsAsync(int tokenId);
        Task IncrementEmailConfirmationResendCountAsync(int tokenId);
        Task InvalidateOldEmailConfirmationTokensAsync(int userId);

        // ===== PASSWORD RESET TOKENS =====
        Task AddPasswordResetTokenAsync(PasswordResetToken token);
        Task<PasswordResetToken?> GetPasswordResetTokenAsync(string token);
        Task MarkPasswordResetTokenAsUsedAsync(string token, string ipAddress);
        Task InvalidateOldPasswordResetTokensAsync(int userId);

        // ===== TWO FACTOR CODES =====
        Task Add2FACodeAsync(TwoFactorCode code);
        Task<TwoFactorCode?> Get2FACodeAsync(int userId, string code);
        Task<TwoFactorCode?> GetLatest2FACodeAsync(int userId);
        Task Mark2FACodeAsUsedAsync(int codeId, string ipAddress);
        Task Increment2FAFailedAttemptsAsync(int codeId);
        Task InvalidateOld2FACodesAsync(int userId);

        // Save changes
        Task SaveChangesAsync();
    }
}