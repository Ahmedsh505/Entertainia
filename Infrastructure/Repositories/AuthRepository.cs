using Core.Interfaces.RepositoriesInterface;
using Core.Models.AuthModels;
using Infrastructure.Data.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly EntertaniaDbcontext _context;

        public AuthRepository(EntertaniaDbcontext context)
        {
            _context = context;
        }

        // ===== REFRESH TOKENS =====

        public async Task AddRefreshTokenAsync(RefreshToken token)
        {
            await _context.RefreshTokens.AddAsync(token);
        }

        public async Task<RefreshToken?> GetRefreshTokenAsync(string token)
        {
            return await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == token);
        }

        public async Task<List<RefreshToken>> GetActiveRefreshTokensAsync(int userId)
        {
            return await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && rt.IsActive)
                .OrderByDescending(rt => rt.CreatedAt)
                .ToListAsync();
        }

        public async Task RevokeRefreshTokenAsync(string token, string ipAddress, string reason)
        {
            var refreshToken = await GetRefreshTokenAsync(token);
            if (refreshToken != null && refreshToken.IsActive)
            {
                refreshToken.IsRevoked = true;
                refreshToken.RevokedAt = DateTime.UtcNow;
                refreshToken.RevokedByIp = ipAddress;
                refreshToken.ReasonRevoked = reason;
            }
        }

        public async Task RevokeAllUserRefreshTokensAsync(int userId, string ipAddress, string reason)
        {
            var tokens = await GetActiveRefreshTokensAsync(userId);
            foreach (var token in tokens)
            {
                token.IsRevoked = true;
                token.RevokedAt = DateTime.UtcNow;
                token.RevokedByIp = ipAddress;
                token.ReasonRevoked = reason;
            }
        }

        public async Task CleanupExpiredRefreshTokensAsync()
        {
            var expiredTokens = await _context.RefreshTokens
                .Where(rt => rt.ExpiresAt < DateTime.UtcNow)
                .ToListAsync();

            _context.RefreshTokens.RemoveRange(expiredTokens);
        }

        // ===== EMAIL CONFIRMATION TOKENS =====

        public async Task AddEmailConfirmationTokenAsync(EmailConfirmationToken token)
        {
            await _context.EmailConfirmationTokens.AddAsync(token);
        }

        public async Task<EmailConfirmationToken?> GetEmailConfirmationTokenAsync(string token)
        {
            // ⚠️ DEPRECATED: Keep for backward compatibility with old URL-based tokens
            return await _context.EmailConfirmationTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Token == token);
        }

        // ✅ NEW: Get confirmation token by CODE (for new 6-digit system)
        public async Task<EmailConfirmationToken?> GetEmailConfirmationTokenByCodeAsync(int userId, string code)
        {
            return await _context.EmailConfirmationTokens
                .Include(t => t.User)
                .Where(t => t.UserId == userId && t.Code == code)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();
        }

        // ✅ NEW: Get latest confirmation token for user (for resend functionality)
        public async Task<EmailConfirmationToken?> GetLatestEmailConfirmationTokenAsync(int userId)
        {
            return await _context.EmailConfirmationTokens
                .Include(t => t.User)
                .Where(t => t.UserId == userId && !t.IsUsed)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task MarkEmailConfirmationTokenAsUsedAsync(string token, string ipAddress)
        {
            var confirmToken = await GetEmailConfirmationTokenAsync(token);
            if (confirmToken != null)
            {
                confirmToken.IsUsed = true;
                confirmToken.UsedAt = DateTime.UtcNow;
                confirmToken.UsedByIp = ipAddress;
            }
        }

        // ✅ NEW: Mark confirmation token as used by CODE
        public async Task MarkEmailConfirmationTokenAsUsedByCodeAsync(int userId, string code, string ipAddress)
        {
            var confirmToken = await GetEmailConfirmationTokenByCodeAsync(userId, code);
            if (confirmToken != null)
            {
                confirmToken.IsUsed = true;
                confirmToken.UsedAt = DateTime.UtcNow;
                confirmToken.UsedByIp = ipAddress;
            }
        }

        // ✅ NEW: Increment failed attempts and lock if necessary
        public async Task IncrementEmailConfirmationFailedAttemptsAsync(int tokenId)
        {
            var token = await _context.EmailConfirmationTokens.FindAsync(tokenId);
            if (token != null)
            {
                token.FailedAttempts++;

                // Lock for 24 hours after 5 failed attempts
                if (token.FailedAttempts >= 5)
                {
                    token.IsLocked = true;
                    token.LockedUntil = DateTime.UtcNow.AddHours(24);
                }
            }
        }

        // ✅ NEW: Increment resend count
        public async Task IncrementEmailConfirmationResendCountAsync(int tokenId)
        {
            var token = await _context.EmailConfirmationTokens.FindAsync(tokenId);
            if (token != null)
            {
                token.ResendCount++;
                token.LastResendAt = DateTime.UtcNow;
            }
        }

        public async Task InvalidateOldEmailConfirmationTokensAsync(int userId)
        {
            var oldTokens = await _context.EmailConfirmationTokens
                .Where(t => t.UserId == userId && !t.IsUsed)
                .ToListAsync();

            foreach (var token in oldTokens)
            {
                token.IsUsed = true;
                token.UsedAt = DateTime.UtcNow;
            }
        }

        // ===== PASSWORD RESET TOKENS =====

        public async Task AddPasswordResetTokenAsync(PasswordResetToken token)
        {
            await _context.PasswordResetTokens.AddAsync(token);
        }

        public async Task<PasswordResetToken?> GetPasswordResetTokenAsync(string token)
        {
            return await _context.PasswordResetTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Token == token);
        }

        public async Task MarkPasswordResetTokenAsUsedAsync(string token, string ipAddress)
        {
            var resetToken = await GetPasswordResetTokenAsync(token);
            if (resetToken != null)
            {
                resetToken.IsUsed = true;
                resetToken.UsedAt = DateTime.UtcNow;
                resetToken.UsedByIp = ipAddress;
            }
        }

        public async Task InvalidateOldPasswordResetTokensAsync(int userId)
        {
            var oldTokens = await _context.PasswordResetTokens
                .Where(t => t.UserId == userId && !t.IsUsed)
                .ToListAsync();

            foreach (var token in oldTokens)
            {
                token.IsUsed = true;
                token.UsedAt = DateTime.UtcNow;
            }
        }

        // ===== TWO FACTOR CODES =====

        public async Task Add2FACodeAsync(TwoFactorCode code)
        {
            await _context.TwoFactorCodes.AddAsync(code);
        }

        public async Task<TwoFactorCode?> Get2FACodeAsync(int userId, string code)
        {
            return await _context.TwoFactorCodes
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.UserId == userId && c.Code == code);
        }

        public async Task<TwoFactorCode?> GetLatest2FACodeAsync(int userId)
        {
            return await _context.TwoFactorCodes
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task Mark2FACodeAsUsedAsync(int codeId, string ipAddress)
        {
            var code = await _context.TwoFactorCodes.FindAsync(codeId);
            if (code != null)
            {
                code.IsUsed = true;
                code.UsedAt = DateTime.UtcNow;
                code.UsedByIp = ipAddress;
            }
        }

        public async Task Increment2FAFailedAttemptsAsync(int codeId)
        {
            var code = await _context.TwoFactorCodes.FindAsync(codeId);
            if (code != null)
            {
                code.FailedAttempts++;
                if (code.FailedAttempts >= 3)
                {
                    code.IsLocked = true;
                }
            }
        }

        public async Task InvalidateOld2FACodesAsync(int userId)
        {
            var oldCodes = await _context.TwoFactorCodes
                .Where(c => c.UserId == userId && !c.IsUsed)
                .ToListAsync();

            foreach (var code in oldCodes)
            {
                code.IsUsed = true;
                code.UsedAt = DateTime.UtcNow;
            }
        }

        // ===== SAVE CHANGES =====

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}