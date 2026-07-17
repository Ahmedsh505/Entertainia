using Core.Enums;
using Core.Models;

namespace Core.Interfaces.ServicesInterface
{
    public interface IUserService
    {
        // ==================== USER RETRIEVAL ====================
        Task<User?> GetUserByIdAsync(int userId);
        Task<User?> GetUserByEmailAsync(string email);
        Task<User?> GetUserByUsernameAsync(string username);
        Task<List<User>> GetAllUsersAsync();
        Task<List<User>> GetOrganizersAsync();
        Task<List<User>> GetPendingOrganizersAsync();
        Task<List<User>> GetActiveOrganizersAsync();

        // ==================== USER CREATION ====================
        Task<(bool Success, string Message, User? User)> CreateUserAsync(
            string email,
            string username,
            string password,
            string firstName,
            string lastName,
            string role,
            string? address = null,
            string? phoneNumber = null);

        // ==================== USER UPDATES ====================
        Task<(bool Success, string Message)> UpdateUserAsync(
            int userId,
            string email,
            string username,
            string firstName,
            string lastName,
            string role,
            AccountStatus accountStatus,
            string? address = null,
            string? phoneNumber = null);

        Task<(bool Success, string Message)> UpdateUserProfileAsync(
            int userId,
            string email,
            string firstName,
            string lastName,
            string? address = null,
            string? phoneNumber = null);

        // ==================== PASSWORD MANAGEMENT ====================
        Task<(bool Success, string Message)> ChangePasswordAsync(
            int userId,
            string currentPassword,
            string newPassword);

        Task<(bool Success, string Message)> AdminChangePasswordAsync(
            int userId,
            string newPassword);

        // ==================== PROFILE PICTURE MANAGEMENT ====================

        /// <summary>
        /// Upload or update user's profile picture
        /// </summary>
        Task<(bool Success, string Message)> UpdateProfilePictureAsync(
            int userId,
            object profilePicture);

        /// <summary>
        /// Remove user's profile picture
        /// </summary>
        Task<(bool Success, string Message)> RemoveProfilePictureAsync(int userId);

        // ==================== USER DELETION (Enhanced) ====================
        Task<(bool Success, string Message)> CustomerSelfDeleteAsync(int userId);
        Task<(bool Success, string Message)> OrganizerSelfDeleteAsync(int userId, bool deleteVenues);
        Task<(bool Success, string Message)> AdminBanUserAsync(int userId, int adminUserId, string? reason = null);
        Task<(bool Success, string Message)> AdminDeleteUserAsync(int userId, int adminUserId, string? reason = null);
        Task<List<User>> GetDeletedUsersAsync();
        Task<(bool Success, string Message)> RestoreUserAsync(int userId);
        Task<List<User>> GetBannedUsersAsync();

        // ==================== ACCOUNT STATUS MANAGEMENT ====================
        Task<(bool Success, string Message)> ApproveOrganizerAsync(int userId, int adminUserId);
        Task<(bool Success, string Message)> RejectOrganizerAsync(int userId, int adminUserId, string? reason = null);
        Task<(bool Success, string Message)> SetAccountStatusAsync(int userId, AccountStatus status);
        Task<(bool Success, string Message)> DisableUserAccountAsync(int userId);
        Task<(bool Success, string Message)> EnableUserAccountAsync(int userId);

        // ==================== VALIDATION ====================
        Task<bool> IsEmailUniqueAsync(string email, int? excludeUserId = null);
        Task<bool> IsUsernameUniqueAsync(string username, int? excludeUserId = null);
        Task<bool> IsEmailBannedAsync(string email);

        // ==================== SECURITY ====================
        Task<bool> ValidatePasswordAsync(User user, string password);
        Task RecordLoginAttemptAsync(int userId, bool success, string ipAddress);
        Task<bool> IsAccountLockedAsync(int userId);
        Task UnlockAccountAsync(int userId);
    }
}