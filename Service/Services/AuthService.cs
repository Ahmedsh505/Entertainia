using Core.Enums;
using Core.Interfaces.RepositoriesInterface;
using Core.Interfaces.ServicesInterface;
using Core.Models;
using Core.Models.AuthModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Service.Services
{
    /// <summary>
    /// Authentication service handling registration, login, password reset, 2FA, and token management
    /// Supports both Cookie Authentication (MVC) and JWT tokens (API)
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ITokenService _tokenService;
        private readonly IAuthRepository _authRepo;
        private readonly IBannedEmailRepo _bannedEmailRepo;
        private readonly IEmailService _emailService;
        private readonly ILogger<AuthService> _logger;
        private readonly IEgyptTimeService _timeService;

        public AuthService(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            ITokenService tokenService,
            IAuthRepository authRepo,
            IBannedEmailRepo bannedEmailRepo,
            IEmailService emailService,
            ILogger<AuthService> logger,
            IEgyptTimeService timeService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _authRepo = authRepo;
            _bannedEmailRepo = bannedEmailRepo;
            _emailService = emailService;
            _logger = logger;
            _timeService = timeService;
        }

        #region REGISTRATION

        public async Task<(bool Success, string Message, User? User)> RegisterAsync(
            string email,
            string username,
            string password,
            string firstName,
            string lastName,
            string role,
            string? address = null,
            string? phoneNumber = null,
            string? ipAddress = null)
        {
            try
            {
                // ✅ CRITICAL: Check if email is banned FIRST
                if (_bannedEmailRepo.IsEmailBanned(email))
                {
                    _logger.LogWarning($"Registration attempt with banned email: {email}");
                    return (false, "This email address is permanently banned and cannot be used for registration. Please contact support if you believe this is an error.", null);
                }

                // Check if email already exists
                var existingUser = await _userManager.FindByEmailAsync(email);
                if (existingUser != null)
                {
                    // If user is deleted (but not banned), they can re-register
                    if (existingUser.IsDeleted && existingUser.DeletionType == DeletionType.SelfDeleted)
                    {
                        _logger.LogInformation($"User {email} re-registering after self-deletion");
                        // Allow them to "re-register" by restoring and updating their account
                        // This is optional - you could also create a completely new account
                    }
                    else
                    {
                        return (false, "Email is already registered.", null);
                    }
                }

                // Check if username already exists (only check active users)
                var existingUsername = await _userManager.FindByNameAsync(username);
                if (existingUsername != null)
                {
                    return (false, "Username is already taken.", null);
                }

                // Create user
                var user = new User
                {
                    FirstName = firstName,
                    LastName = lastName,
                    UserName = username,
                    Email = email,
                    Role = role,
                    Address = address,
                    PhoneNumber = phoneNumber,
                    AccountStatus = role == "Organizer"
                        ? AccountStatus.PendingApproval
                        : AccountStatus.Active,
                    EmailConfirmed = false,
                    CreatedAt = DateTime.UtcNow,
                    DeletionType = DeletionType.None // ✅ Set explicitly
                };

                var result = await _userManager.CreateAsync(user, password);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return (false, errors, null);
                }

                // Add to role
                await _userManager.AddToRoleAsync(user, role);

                _logger.LogInformation($"User registered: {email} as {role}");

                return (true, "Registration successful", user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error during registration for {email}");
                return (false, "An error occurred during registration", null);
            }
        }

        #endregion

        #region LOGIN

        public async Task<(bool Success, string Message, User? User, string? AccessToken, string? RefreshToken)> LoginAsync(
            string emailOrUsername,
            string password,
            string ipAddress,
            string? deviceInfo = null)
        {
            try
            {
                // Find user by email or username
                User? user;
                if (emailOrUsername.Contains("@"))
                    user = await _userManager.FindByEmailAsync(emailOrUsername);
                else
                    user = await _userManager.FindByNameAsync(emailOrUsername);

                if (user == null)
                {
                    return (false, "Invalid credentials.", null, null, null);
                }

                // Check password
                var passwordCheck = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: false);
                if (!passwordCheck.Succeeded)
                {
                    user.FailedLoginAttempts++;
                    user.LastFailedLoginAt = DateTime.UtcNow;
                    await _userManager.UpdateAsync(user);

                    _logger.LogWarning($"Failed login attempt for user {user.Email} from IP {ipAddress}");
                    return (false, "Invalid credentials.", null, null, null);
                }

                // Reset failed attempts
                user.FailedLoginAttempts = 0;
                user.LastLoginAt = DateTime.UtcNow;
                user.LastLoginIp = ipAddress;
                await _userManager.UpdateAsync(user);

                // Check if 2FA is enabled
                if (user.TwoFactorEnabled)
                {
                    _logger.LogInformation($"2FA required for user {user.Email}");
                    return (false, "2FA_REQUIRED", user, null, null);
                }

                // Check organizer status
                if (user.Role == "Organizer")
                {
                    if (user.AccountStatus == AccountStatus.PendingApproval)
                        return (false, "PENDING_APPROVAL", user, null, null);

                    if (user.AccountStatus == AccountStatus.Rejected)
                        return (false, "ACCOUNT_REJECTED", user, null, null);

                    if (user.AccountStatus == AccountStatus.Disabled)
                        return (false, "ACCOUNT_DISABLED", user, null, null);
                }

                // Generate tokens (for API use)
                var roles = await _userManager.GetRolesAsync(user);
                var accessToken = _tokenService.GenerateAccessToken(user, roles);
                var refreshToken = _tokenService.GenerateRefreshToken(user.Id, ipAddress, deviceInfo);

                await _authRepo.AddRefreshTokenAsync(refreshToken);
                await _authRepo.SaveChangesAsync();

                _logger.LogInformation($"User logged in: {user.Email}");

                return (true, "Login successful", user, accessToken, refreshToken.Token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error during login for {emailOrUsername}");
                return (false, "An error occurred during login", null, null, null);
            }
        }

        #endregion

        #region TWO-FACTOR AUTHENTICATION

        public async Task<(bool Success, string Message)> Enable2FAAsync(int userId, string ipAddress)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                    return (false, "User not found.");

                if (user.TwoFactorEnabled)
                    return (false, "2FA is already enabled.");

                user.TwoFactorEnabled = true;
                user.TwoFactorEnabledAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                // Send confirmation email
                try
                {
                    await _emailService.Send2FAEnabledNotificationAsync(
                        user.Email,
                        $"{user.FirstName} {user.LastName}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to send 2FA enabled email to {user.Email}");
                }

                _logger.LogInformation($"2FA enabled for user {user.Email}");
                return (true, "Two-factor authentication enabled successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error enabling 2FA for user {userId}");
                return (false, "An error occurred while enabling 2FA.");
            }
        }

        public async Task<(bool Success, string Message)> Disable2FAAsync(int userId, string ipAddress)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                    return (false, "User not found.");

                if (!user.TwoFactorEnabled)
                    return (false, "2FA is not enabled.");

                user.TwoFactorEnabled = false;
                user.TwoFactorEnabledAt = null;
                await _userManager.UpdateAsync(user);

                // Send notification email
                try
                {
                    await _emailService.Send2FADisabledNotificationAsync(
                        user.Email,
                        $"{user.FirstName} {user.LastName}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to send 2FA disabled email to {user.Email}");
                }

                _logger.LogInformation($"2FA disabled for user {user.Email}");
                return (true, "Two-factor authentication disabled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error disabling 2FA for user {userId}");
                return (false, "An error occurred while disabling 2FA.");
            }
        }

        public async Task<(bool Success, string Message, string? Code)> Send2FACodeAsync(int userId, string ipAddress)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                    return (false, "User not found.", null);

                // Invalidate old codes
                await _authRepo.InvalidateOld2FACodesAsync(userId);

                // Generate new code
                var code = _tokenService.Generate2FACode(userId, ipAddress);
                await _authRepo.Add2FACodeAsync(code);
                await _authRepo.SaveChangesAsync();

                // Send email
                try
                {
                    await _emailService.Send2FACodeAsync(
                        user.Email,
                        $"{user.FirstName} {user.LastName}",
                        code.Code);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to send 2FA code email to {user.Email}");
                    return (false, "Failed to send verification code.", null);
                }

                _logger.LogInformation($"2FA code sent to {user.Email}");
                return (true, "Verification code sent.", code.Code);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending 2FA code for user {userId}");
                return (false, "An error occurred while sending verification code.", null);
            }
        }

        public async Task<(bool Success, string Message)> Verify2FACodeAsync(int userId, string code)
        {
            try
            {
                var twoFactorCode = await _authRepo.Get2FACodeAsync(userId, code);

                if (twoFactorCode == null || !twoFactorCode.IsValid)
                {
                    // Increment failed attempts if code exists
                    if (twoFactorCode != null)
                    {
                        await _authRepo.Increment2FAFailedAttemptsAsync(twoFactorCode.Id);
                        await _authRepo.SaveChangesAsync();
                    }

                    return (false, "Invalid or expired verification code.");
                }

                // Mark code as used
                await _authRepo.Mark2FACodeAsUsedAsync(twoFactorCode.Id, "system");
                await _authRepo.SaveChangesAsync();

                _logger.LogInformation($"2FA code verified for user {userId}");
                return (true, "Verification successful.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error verifying 2FA code for user {userId}");
                return (false, "An error occurred during verification.");
            }
        }

        #endregion

        #region EMAIL CONFIRMATION

        /// <summary>
        /// ✅ NEW: Generate 6-digit email confirmation code (5-minute expiry)
        /// </summary>
        public async Task<(bool Success, string Message, string? Code)> GenerateEmailConfirmationCodeAsync(
            int userId,
            string ipAddress)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                    return (false, "User not found.", null);

                if (user.EmailConfirmed)
                    return (false, "Email is already confirmed.", null);

                // Check if user is in lockout period
                var existingToken = await _authRepo.GetLatestEmailConfirmationTokenAsync(userId);
                if (existingToken != null && existingToken.IsInLockout)
                {
                    var remainingTime = existingToken.LockedUntil!.Value - DateTime.UtcNow;
                    var hours = (int)remainingTime.TotalHours;
                    var minutes = remainingTime.Minutes;
                    return (false, $"Too many failed attempts. Please try again in {hours}h {minutes}m.", null);
                }

                // Invalidate old tokens
                await _authRepo.InvalidateOldEmailConfirmationTokensAsync(userId);

                // Generate new 6-digit code
                var token = _tokenService.GenerateEmailConfirmationToken(userId, user.Email, ipAddress);
                await _authRepo.AddEmailConfirmationTokenAsync(token);
                await _authRepo.SaveChangesAsync();

                _logger.LogInformation($"Email confirmation code generated for {user.Email}");
                return (true, "Verification code generated.", token.Code);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating email confirmation code for user {userId}");
                return (false, "An error occurred while generating verification code.", null);
            }
        }

        /// <summary>
        /// ✅ NEW: Verify 6-digit email confirmation code
        /// </summary>
        public async Task<(bool Success, string Message)> VerifyEmailConfirmationCodeAsync(
            int userId,
            string code,
            string ipAddress)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                    return (false, "User not found.");

                if (user.EmailConfirmed)
                    return (false, "Email is already confirmed.");

                // Get the confirmation token by code
                var confirmToken = await _authRepo.GetEmailConfirmationTokenByCodeAsync(userId, code);

                if (confirmToken == null)
                {
                    _logger.LogWarning($"Invalid confirmation code attempt for user {userId}");
                    return (false, "Invalid verification code.");
                }

                // Check if locked
                if (confirmToken.IsInLockout)
                {
                    var remainingTime = confirmToken.LockedUntil!.Value - DateTime.UtcNow;
                    var hours = (int)remainingTime.TotalHours;
                    var minutes = remainingTime.Minutes;
                    return (false, $"Account locked due to too many failed attempts. Try again in {hours}h {minutes}m.");
                }

                // Check if expired
                if (confirmToken.IsExpired)
                {
                    _logger.LogWarning($"Expired confirmation code used by user {userId}");
                    return (false, "Verification code has expired. Please request a new code.");
                }

                // Check if already used
                if (confirmToken.IsUsed)
                {
                    _logger.LogWarning($"Already used confirmation code by user {userId}");
                    return (false, "This verification code has already been used.");
                }

                // ✅ CODE IS VALID - Confirm email
                user.EmailConfirmed = true;
                user.EmailConfirmedAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                // Mark token as used
                await _authRepo.MarkEmailConfirmationTokenAsUsedByCodeAsync(userId, code, ipAddress);
                await _authRepo.SaveChangesAsync();

                _logger.LogInformation($"Email confirmed successfully for {user.Email}");
                return (true, "Email confirmed successfully! You can now log in.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error verifying email confirmation code for user {userId}");
                return (false, "An error occurred during email confirmation.");
            }
        }

        /// <summary>
        /// ✅ NEW: Resend email confirmation code with rate limiting (1-minute cooldown, max 5 resends)
        /// </summary>
        public async Task<(bool Success, string Message, string? Code)> ResendEmailConfirmationCodeAsync(
            int userId,
            string ipAddress)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                    return (false, "User not found.", null);

                if (user.EmailConfirmed)
                    return (false, "Email is already confirmed.", null);

                // Get latest token to check rate limits
                var existingToken = await _authRepo.GetLatestEmailConfirmationTokenAsync(userId);

                // Check if in lockout period
                if (existingToken != null && existingToken.IsInLockout)
                {
                    var remainingTime = existingToken.LockedUntil!.Value - DateTime.UtcNow;
                    var hours = (int)remainingTime.TotalHours;
                    var minutes = remainingTime.Minutes;
                    return (false, $"Account locked. Please try again in {hours}h {minutes}m.", null);
                }

                // ✅ Rate Limit: Check 1-minute cooldown
                if (existingToken != null && !existingToken.CanResend)
                {
                    var nextResendTime = existingToken.LastResendAt!.Value.AddMinutes(1);
                    var waitSeconds = (int)(nextResendTime - DateTime.UtcNow).TotalSeconds;
                    return (false, $"Please wait {waitSeconds} seconds before requesting a new code.", null);
                }

                // ✅ Rate Limit: Check max 5 resend attempts
                if (existingToken != null && existingToken.ResendCount >= 5)
                {
                    return (false, "Maximum resend attempts reached. Please contact support if you need assistance.", null);
                }

                // Generate new code
                var (success, message, code) = await GenerateEmailConfirmationCodeAsync(userId, ipAddress);

                if (success && !string.IsNullOrEmpty(code))
                {
                    // Increment resend count on the NEW token
                    var newToken = await _authRepo.GetLatestEmailConfirmationTokenAsync(userId);
                    if (newToken != null)
                    {
                        // Carry over resend count from previous token
                        if (existingToken != null)
                        {
                            newToken.ResendCount = existingToken.ResendCount + 1;
                        }
                        else
                        {
                            newToken.ResendCount = 1;
                        }
                        await _authRepo.SaveChangesAsync();
                    }

                    _logger.LogInformation($"Email confirmation code resent to {user.Email}");
                    return (true, "New verification code sent to your email.", code);
                }

                return (success, message, code);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error resending email confirmation code for user {userId}");
                return (false, "An error occurred while resending verification code.", null);
            }
        }

        /// <summary>
        /// ⚠️ DEPRECATED: Keep for backward compatibility with old URL-based tokens
        /// Remove this method after all old tokens expire or in next major version
        /// </summary>
        public async Task<(bool Success, string Message)> ConfirmEmailAsync(string token)
        {
            try
            {
                var confirmToken = await _authRepo.GetEmailConfirmationTokenAsync(token);

                if (confirmToken == null || !confirmToken.IsValid)
                {
                    return (false, "Invalid or expired confirmation link. Please use the new verification code system.");
                }

                var user = await _userManager.FindByIdAsync(confirmToken.UserId.ToString());
                if (user == null)
                    return (false, "User not found.");

                // Mark email as confirmed
                user.EmailConfirmed = true;
                user.EmailConfirmedAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                // Mark token as used
                await _authRepo.MarkEmailConfirmationTokenAsUsedAsync(token, "system");
                await _authRepo.SaveChangesAsync();

                _logger.LogInformation($"Email confirmed for {user.Email} (legacy URL method)");
                return (true, "Email confirmed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming email (legacy method)");
                return (false, "An error occurred during email confirmation.");
            }
        }

        #endregion

        #region PASSWORD RESET

        public async Task<(bool Success, string Message, string? Token)> GeneratePasswordResetTokenAsync(
            string email,
            string ipAddress)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);

                // Don't reveal if user exists (security best practice)
                if (user == null)
                {
                    _logger.LogWarning($"Password reset requested for non-existent email: {email}");
                    return (true, "If that email is registered, you'll receive a reset link.", null);
                }

                // Invalidate old tokens
                await _authRepo.InvalidateOldPasswordResetTokensAsync(user.Id);

                // Generate new token
                var token = _tokenService.GeneratePasswordResetToken(user.Id, user.Email, ipAddress);
                await _authRepo.AddPasswordResetTokenAsync(token);
                await _authRepo.SaveChangesAsync();

                _logger.LogInformation($"Password reset token generated for {user.Email}");
                return (true, "If that email is registered, you'll receive a reset link.", token.Token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating password reset token for {email}");
                return (false, "An error occurred while processing your request.", null);
            }
        }

        public async Task<(bool Success, string Message)> ResetPasswordAsync(
            string email,
            string token,
            string newPassword)
        {
            try
            {
                var resetToken = await _authRepo.GetPasswordResetTokenAsync(token);

                if (resetToken == null || !resetToken.IsValid || resetToken.Email != email)
                {
                    return (false, "Invalid or expired reset link.");
                }

                var user = await _userManager.FindByIdAsync(resetToken.UserId.ToString());
                if (user == null)
                    return (false, "User not found.");

                // Reset password
                var removeResult = await _userManager.RemovePasswordAsync(user);
                if (!removeResult.Succeeded)
                {
                    var errors = string.Join(", ", removeResult.Errors.Select(e => e.Description));
                    return (false, $"Failed to reset password: {errors}");
                }

                var addResult = await _userManager.AddPasswordAsync(user, newPassword);
                if (!addResult.Succeeded)
                {
                    var errors = string.Join(", ", addResult.Errors.Select(e => e.Description));
                    return (false, $"Failed to set new password: {errors}");
                }

                // Mark token as used
                await _authRepo.MarkPasswordResetTokenAsUsedAsync(token, "system");
                await _authRepo.SaveChangesAsync();

                // Send security alert
                try
                {
                    await _emailService.SendSecurityAlertAsync(
                        user.Email,
                        $"{user.FirstName} {user.LastName}",
                        "Your password has been changed successfully.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to send password reset confirmation to {user.Email}");
                }

                _logger.LogInformation($"Password reset successful for {user.Email}");
                return (true, "Password reset successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error resetting password for {email}");
                return (false, "An error occurred while resetting password.");
            }
        }

        public async Task<string?> GetEmailFromPasswordResetTokenAsync(string token)
        {
            var resetToken = await _authRepo.GetPasswordResetTokenAsync(token);
            return resetToken?.Email;
        }

        #endregion

        #region TOKEN MANAGEMENT

        public async Task<(bool Success, string Message, string? AccessToken)> RefreshAccessTokenAsync(
            string refreshToken,
            string ipAddress)
        {
            try
            {
                var token = await _authRepo.GetRefreshTokenAsync(refreshToken);

                if (token == null || !token.IsActive)
                {
                    return (false, "Invalid refresh token.", null);
                }

                var user = await _userManager.FindByIdAsync(token.UserId.ToString());
                if (user == null)
                    return (false, "User not found.", null);

                // Revoke old token
                await _authRepo.RevokeRefreshTokenAsync(refreshToken, ipAddress, "Replaced by new token");

                // Generate new tokens
                var roles = await _userManager.GetRolesAsync(user);
                var newAccessToken = _tokenService.GenerateAccessToken(user, roles);
                var newRefreshToken = _tokenService.GenerateRefreshToken(user.Id, ipAddress);

                // Link tokens
                token.ReplacedByToken = newRefreshToken.Token;
                await _authRepo.AddRefreshTokenAsync(newRefreshToken);
                await _authRepo.SaveChangesAsync();

                _logger.LogInformation($"Access token refreshed for user {user.Email}");
                return (true, "Token refreshed successfully.", newAccessToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing access token");
                return (false, "An error occurred while refreshing token.", null);
            }
        }

        public async Task<(bool Success, string Message)> RevokeRefreshTokenAsync(string refreshToken)
        {
            try
            {
                await _authRepo.RevokeRefreshTokenAsync(refreshToken, "system", "Revoked by user");
                await _authRepo.SaveChangesAsync();

                _logger.LogInformation("Refresh token revoked");
                return (true, "Token revoked successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking refresh token");
                return (false, "An error occurred while revoking token.");
            }
        }

        public async Task<(bool Success, string Message)> RevokeAllUserTokensAsync(int userId)
        {
            try
            {
                await _authRepo.RevokeAllUserRefreshTokensAsync(userId, "system", "All tokens revoked");
                await _authRepo.SaveChangesAsync();

                _logger.LogInformation($"All refresh tokens revoked for user {userId}");
                return (true, "All tokens revoked successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error revoking all tokens for user {userId}");
                return (false, "An error occurred while revoking tokens.");
            }
        }

        #endregion

        #region LOGOUT

        public async Task<(bool Success, string Message)> LogoutAsync(int userId, string? refreshToken = null)
        {
            try
            {
                // Revoke refresh token if provided
                if (!string.IsNullOrEmpty(refreshToken))
                {
                    await _authRepo.RevokeRefreshTokenAsync(refreshToken, "system", "User logged out");
                    await _authRepo.SaveChangesAsync();
                }

                _logger.LogInformation($"User {userId} logged out");
                return (true, "Logged out successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error during logout for user {userId}");
                return (false, "An error occurred during logout.");
            }
        }

        #endregion

        #region VALIDATION

        public async Task<bool> ValidateEmailConfirmationTokenAsync(int userId, string token)
        {
            try
            {
                var confirmToken = await _authRepo.GetEmailConfirmationTokenAsync(token);
                return confirmToken != null && confirmToken.IsValid && confirmToken.UserId == userId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating email confirmation token");
                return false;
            }
        }

        public async Task<bool> ValidatePasswordResetTokenAsync(string email, string token)
        {
            try
            {
                var resetToken = await _authRepo.GetPasswordResetTokenAsync(token);
                return resetToken != null && resetToken.IsValid && resetToken.Email == email;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating password reset token");
                return false;
            }
        }

        public async Task<bool> ValidateRefreshTokenAsync(string refreshToken)
        {
            try
            {
                var token = await _authRepo.GetRefreshTokenAsync(refreshToken);
                return token != null && token.IsActive;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating refresh token");
                return false;
            }
        }

        #endregion

        #region EMAIL CONFIRMATION HELPER METHODS

        /// <summary>
        /// Get latest email confirmation token for a user (helper method)
        /// </summary>
        public async Task<EmailConfirmationToken?> GetLatestEmailConfirmationTokenAsync(int userId)
        {
            try
            {
                return await _authRepo.GetLatestEmailConfirmationTokenAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting latest confirmation token for user {userId}");
                return null;
            }
        }

        /// <summary>
        /// Increment failed confirmation attempts (helper method)
        /// </summary>
        public async Task IncrementEmailConfirmationFailedAttemptsAsync(int tokenId)
        {
            try
            {
                await _authRepo.IncrementEmailConfirmationFailedAttemptsAsync(tokenId);
                await _authRepo.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error incrementing failed attempts for token {tokenId}");
            }
        }

        #endregion

    }
}