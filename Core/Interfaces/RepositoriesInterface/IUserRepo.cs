using Core.Enums;
using Core.Interfaces.ServicesInterface;
using Core.Models;

namespace Core.Interfaces.RepositoriesInterface
{
    public interface IUserRepo
    {
        // Basic CRUD
        void Add(User user);
        void Update(User user);
        void Delete(User user);
        List<User> GetAll();
        List<User> GetOrganizer();
        User GetById(int id);
        List<User> GetDeletedUsers();
        bool RestoreUser(int userId);
        List<User> GetAllIncludingDeleted();

        void Save();

        // --- Approval workflow methods ---

        /// <summary>
        /// Returns all organizers whose AccountStatus is PendingApproval.
        /// </summary>
        List<User> GetPendingOrganizers();

        /// <summary>
        /// Returns all active organizers (AccountStatus = Active).
        /// </summary>
        List<User> GetActiveOrganizers();

        /// <summary>
        /// Updates the account status (PendingApproval, Active, Rejected, Disabled).
        /// </summary>
        void SetAccountStatus(int userId, AccountStatus status);

        /// <summary>
        /// Gets a user by email or username (for login or checks).
        /// </summary>
        User? GetByEmail(string email);

        /// <summary>
        /// Delete user and their documents physically
        /// </summary>
        void DeleteUserAndDocs(int userId, IFileService fileService);

        // ✅ NEW: Enhanced deletion methods

        /// <summary>
        /// Soft delete user (customer self-deletion)
        /// Frees username, keeps bookings
        /// </summary>
        void SoftDeleteCustomer(int userId);

        /// <summary>
        /// Soft delete organizer with optional venue deletion
        /// </summary>
        void SoftDeleteOrganizer(int userId, bool deleteVenues);

        /// <summary>
        /// Admin bans user (email permanently banned)
        /// </summary>
        void AdminBanUser(int userId, int adminUserId, string? reason = null);

        /// <summary>
        /// Admin deletes user (can be restored)
        /// </summary>
        void AdminDeleteUser(int userId, int adminUserId, string? reason = null);

        /// <summary>
        /// Restore a deleted user (admin action)
        /// </summary>
        bool RestoreDeletedUser(int userId);

        /// <summary>
        /// Get all banned users
        /// </summary>
        List<User> GetBannedUsers();

        /// <summary>
        /// Free up username for reuse
        /// </summary>
        void FreeUsername(int userId);
    }
}