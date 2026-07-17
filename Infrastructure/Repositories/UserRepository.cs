using Core.Enums;
using Core.Interfaces.RepositoriesInterface;
using Core.Interfaces.ServicesInterface;
using Core.Models;
using Infrastructure.Data.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class UserRepository : IUserRepo
    {
        private readonly EntertaniaDbcontext _context;

        public UserRepository(EntertaniaDbcontext context)
        {
            _context = context;
        }

        public void Add(User user)
        {
            _context.Users.Add(user);
        }

        public void Update(User user)
        {
            if (!_context.Entry(user).Collection(u => u.VerificationDocs).IsLoaded)
            {
                _context.Entry(user).Collection(u => u.VerificationDocs).Load();
            }

            _context.Users.Update(user);
        }

        public void Delete(User user)
        {
            if (user != null)
            {
                user.IsDeleted = true;
                user.DeletedAt = DateTime.UtcNow;
                _context.Users.Update(user);
            }
        }

        public List<User> GetAll()
        {
            return _context.Users
                .Where(u => !u.IsDeleted)
                .AsNoTracking()
                .Include(u => u.VerificationDocs)
                .ToList();
        }

        public List<User> GetOrganizer()
        {
            return _context.Users
                .Where(u => u.Role == "Organizer" && !u.IsDeleted)
                .Include(u => u.VerificationDocs)
                .ToList();
        }

        public User GetById(int id)
        {
            return _context.Users
                .Include(u => u.VerificationDocs)
                .FirstOrDefault(u => u.Id == id && !u.IsDeleted);
        }

        public List<User> GetDeletedUsers()
        {
            return _context.Users
                .IgnoreQueryFilters()
                .Where(u => u.IsDeleted && u.DeletionType != DeletionType.AdminBanned)
                .Include(u => u.VerificationDocs)
                .Include(u => u.DeletedBy)
                .ToList();
        }

        public void Save()
        {
            _context.SaveChanges();
        }

        public List<User> GetPendingOrganizers()
        {
            return _context.Users
                .Where(u => u.Role == "Organizer" &&
                           u.AccountStatus == AccountStatus.PendingApproval &&
                           !u.IsDeleted)
                .Include(u => u.VerificationDocs)
                .ToList();
        }

        public List<User> GetActiveOrganizers()
        {
            return _context.Users
                .Where(u => u.Role == "Organizer" &&
                           u.AccountStatus == AccountStatus.Active &&
                           !u.IsDeleted)
                .Include(u => u.VerificationDocs)
                .ToList();
        }

        public void SetAccountStatus(int userId, AccountStatus status)
        {
            var user = _context.Users.Find(userId);
            if (user != null && !user.IsDeleted)
            {
                user.AccountStatus = status;
                user.UpdatedAt = DateTime.UtcNow;
                _context.SaveChanges();
            }
        }

        public User? GetByEmail(string email)
        {
            return _context.Users
                .Include(u => u.VerificationDocs)
                .FirstOrDefault(u => u.Email == email && !u.IsDeleted);
        }

        public void DeleteUserAndDocs(int userId, IFileService fileService)
        {
            var user = _context.Users
                .Include(u => u.VerificationDocs)
                .FirstOrDefault(u => u.Id == userId && !u.IsDeleted);

            if (user == null)
                return;

            // Delete files physically
            foreach (var doc in user.VerificationDocs)
            {
                if (!string.IsNullOrEmpty(doc.FilePath))
                {
                    fileService.DeleteFile(doc.FilePath);
                }
            }

            // Mark docs deleted
            _context.VerificationDocs.RemoveRange(user.VerificationDocs);

            // Soft delete user
            user.IsDeleted = true;
            user.DeletedAt = DateTime.UtcNow;

            _context.Users.Update(user);
            _context.SaveChanges();
        }

        public bool RestoreUser(int userId)
        {
            var user = _context.Users
                .IgnoreQueryFilters()
                .FirstOrDefault(u => u.Id == userId && u.IsDeleted);

            if (user == null || user.DeletionType == DeletionType.AdminBanned)
                return false;

            user.IsDeleted = false;
            user.DeletedAt = null;
            user.DeletionType = DeletionType.None;
            user.DeletedByUserId = null;
            user.DeletionReason = null;
            user.UpdatedAt = DateTime.UtcNow;

            // Restore username if it was freed
            if (!string.IsNullOrEmpty(user.OriginalUsername))
            {
                user.UserName = user.OriginalUsername;
                user.OriginalUsername = null;
            }

            _context.Users.Update(user);
            _context.SaveChanges();

            return true;
        }

        public List<User> GetAllIncludingDeleted()
        {
            return _context.Users
                .IgnoreQueryFilters()
                .Include(u => u.VerificationDocs)
                .Include(u => u.DeletedBy)
                .ToList();
        }

        // ✅ NEW: Enhanced deletion methods

        public void SoftDeleteCustomer(int userId)
        {
            var user = _context.Users
                .Include(u => u.Bookings)
                .FirstOrDefault(u => u.Id == userId && !u.IsDeleted);

            if (user == null) return;

            // Mark as deleted
            user.IsDeleted = true;
            user.DeletedAt = DateTime.UtcNow;
            user.DeletionType = DeletionType.SelfDeleted;
            user.DeletedByUserId = null; // Self-deletion

            // Free up username
            FreeUsername(userId);

            // Bookings stay intact (no changes)
            // Payment history stays intact (for admin/organizer insights)

            _context.Users.Update(user);
            _context.SaveChanges();
        }

        public void SoftDeleteOrganizer(int userId, bool deleteVenues)
        {
            var user = _context.Users
                .Include(u => u.OrganizedEvents)
                    .ThenInclude(e => e.Venue)
                .Include(u => u.OrganizedEvents)
                    .ThenInclude(e => e.Bookings)
                .FirstOrDefault(u => u.Id == userId && !u.IsDeleted);

            if (user == null) return;

            // 1. Mark organizer as deleted (DEACTIVATED, not deleted)
            user.IsDeleted = true;
            user.DeletedAt = DateTime.UtcNow;
            user.DeletionType = DeletionType.SelfDeleted;
            user.DeletedByUserId = null; // Self-deletion
            user.AccountStatus = AccountStatus.Disabled;

            // 2. Free username for reuse
            FreeUsername(userId);

            // 3. Handle venues if requested
            if (deleteVenues)
            {
                var venues = _context.Venues
                    .Where(v => v.CreatedByUserId == userId && !v.IsDeleted)
                    .ToList();

                foreach (var venue in venues)
                {
                    // Soft delete venue
                    venue.IsDeleted = true;
                    venue.DeletedAt = DateTime.UtcNow;
                    venue.DeletedByUserId = userId;
                    venue.OriginalName = venue.Name;
                    venue.Name = $"[Deleted] {venue.Name}";

                    _context.Venues.Update(venue);
                }
            }

            // 4. CRITICAL: DO NOT TOUCH EVENTS
            // Events remain in database with all relationships intact
            // EF Core will NOT cascade delete because:
            // - OnDelete(DeleteBehavior.Restrict) prevents cascade
            // - We're only soft-deleting the user, not removing from DB

            _context.Users.Update(user);
            _context.SaveChanges();
        }

        public void AdminBanUser(int userId, int adminUserId, string? reason = null)
        {
            var user = _context.Users
                .Include(u => u.VerificationDocs)
                .FirstOrDefault(u => u.Id == userId);

            if (user == null) return;

            // Mark as banned
            user.IsDeleted = true;
            user.DeletedAt = DateTime.UtcNow;
            user.DeletionType = DeletionType.AdminBanned;
            user.DeletedByUserId = adminUserId;
            user.DeletionReason = reason;
            user.AccountStatus = AccountStatus.Disabled;

            // Free username (can be reused)
            FreeUsername(userId);

            _context.Users.Update(user);
            _context.SaveChanges();

            // Email will be added to BannedEmails table by service layer
        }

        public void AdminDeleteUser(int userId, int adminUserId, string? reason = null)
        {
            var user = _context.Users
                .FirstOrDefault(u => u.Id == userId && !u.IsDeleted);

            if (user == null) return;

            user.IsDeleted = true;
            user.DeletedAt = DateTime.UtcNow;
            user.DeletionType = DeletionType.AdminDeleted;
            user.DeletedByUserId = adminUserId;
            user.DeletionReason = reason;
            user.UpdatedAt = DateTime.UtcNow;

            // Don't free username for admin deletions (can be restored)

            _context.Users.Update(user);
            _context.SaveChanges();
        }

        public bool RestoreDeletedUser(int userId)
        {
            var user = _context.Users
                .IgnoreQueryFilters()
                .FirstOrDefault(u => u.Id == userId && u.IsDeleted);

            // Can't restore banned users
            if (user == null || user.DeletionType == DeletionType.AdminBanned)
                return false;

            user.IsDeleted = false;
            user.DeletedAt = null;
            user.DeletionType = DeletionType.None;
            user.DeletedByUserId = null;
            user.DeletionReason = null;
            user.UpdatedAt = DateTime.UtcNow;

            // Restore username if it was freed
            if (!string.IsNullOrEmpty(user.OriginalUsername))
            {
                user.UserName = user.OriginalUsername;
                user.OriginalUsername = null;
            }

            // Restore account status for organizers
            if (user.Role == "Organizer")
            {
                user.AccountStatus = AccountStatus.PendingApproval; // Needs re-approval
            }
            else
            {
                user.AccountStatus = AccountStatus.Active;
            }

            _context.Users.Update(user);
            _context.SaveChanges();

            return true;
        }

        public List<User> GetBannedUsers()
        {
            return _context.Users
                .IgnoreQueryFilters()
                .Where(u => u.IsDeleted && u.DeletionType == DeletionType.AdminBanned)
                .Include(u => u.DeletedBy)
                .OrderByDescending(u => u.DeletedAt)
                .ToList();
        }

        public void FreeUsername(int userId)
        {
            var user = _context.Users
                .IgnoreQueryFilters()
                .FirstOrDefault(u => u.Id == userId);

            if (user == null || string.IsNullOrEmpty(user.UserName)) return;

            // Store original username
            user.OriginalUsername = user.UserName;

            // Set username to something unique that won't conflict
            user.UserName = $"deleted_user_{userId}_{Guid.NewGuid().ToString("N").Substring(0, 8)}";

            _context.Users.Update(user);
            _context.SaveChanges();
        }
    }
}