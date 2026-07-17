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
    /// Enhanced UserService with proper deletion handling and no duplicate methods
    /// </summary>
    public class UserService : IUserService
    {
        private readonly IUserRepo _userRepo;
        private readonly IBannedEmailRepo _bannedEmailRepo;
        private readonly IFileService _fileService;
        private readonly IEmailService _emailService;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole<int>> _roleManager;
        private readonly ILogger<UserService> _logger;
        private readonly IEgyptTimeService _timeService;

        public UserService(
            IUserRepo userRepo,
            IBannedEmailRepo bannedEmailRepo,
            IFileService fileService,
            IEmailService emailService,
            UserManager<User> userManager,
            RoleManager<IdentityRole<int>> roleManager,
            ILogger<UserService> logger,
            IEgyptTimeService timeService)
        {
            _userRepo = userRepo;
            _bannedEmailRepo = bannedEmailRepo;
            _fileService = fileService;
            _emailService = emailService;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
            _timeService = timeService;
        }

        // ==================== USER RETRIEVAL ====================

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            try
            {
                var user = _userRepo.GetById(userId);
                return await Task.FromResult(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user {UserId}", userId);
                return null;
            }
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            try
            {
                return await _userManager.FindByEmailAsync(email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user by email {Email}", email);
                return null;
            }
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            try
            {
                return await _userManager.FindByNameAsync(username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user by username {Username}", username);
                return null;
            }
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            try
            {
                var users = _userRepo.GetAll();
                return await Task.FromResult(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all users");
                return new List<User>();
            }
        }

        public async Task<List<User>> GetOrganizersAsync()
        {
            try
            {
                var organizers = _userRepo.GetOrganizer();
                return await Task.FromResult(organizers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving organizers");
                return new List<User>();
            }
        }

        public async Task<List<User>> GetPendingOrganizersAsync()
        {
            try
            {
                var pendingOrganizers = _userRepo.GetPendingOrganizers();
                return await Task.FromResult(pendingOrganizers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pending organizers");
                return new List<User>();
            }
        }

        public async Task<List<User>> GetActiveOrganizersAsync()
        {
            try
            {
                var activeOrganizers = _userRepo.GetActiveOrganizers();
                return await Task.FromResult(activeOrganizers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active organizers");
                return new List<User>();
            }
        }

        public async Task<List<User>> GetDeletedUsersAsync()
        {
            try
            {
                var deletedUsers = _userRepo.GetDeletedUsers();
                return await Task.FromResult(deletedUsers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving deleted users");
                return new List<User>();
            }
        }

        public async Task<List<User>> GetBannedUsersAsync()
        {
            try
            {
                var bannedUsers = _userRepo.GetBannedUsers();
                return await Task.FromResult(bannedUsers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving banned users");
                return new List<User>();
            }
        }

        // ==================== USER CREATION ====================

        public async Task<(bool Success, string Message, User? User)> CreateUserAsync(
            string email,
            string username,
            string password,
            string firstName,
            string lastName,
            string role,
            string? address = null,
            string? phoneNumber = null)
        {
            try
            {
                // Validation
                if (string.IsNullOrWhiteSpace(email))
                    return (false, "Email is required", null);

                if (string.IsNullOrWhiteSpace(username))
                    return (false, "Username is required", null);

                if (string.IsNullOrWhiteSpace(password))
                    return (false, "Password is required", null);

                if (string.IsNullOrWhiteSpace(role))
                    return (false, "Role is required", null);

                // Check if role exists
                if (!await _roleManager.RoleExistsAsync(role))
                    return (false, $"Role '{role}' does not exist", null);

                // ✅ Check if email is banned
                if (await IsEmailBannedAsync(email))
                    return (false, "This email address is permanently banned and cannot be used for registration.", null);

                // Check for existing email (including deleted users)
                var existingUser = _userRepo
                    .GetAllIncludingDeleted()
                    .FirstOrDefault(u => u.Email.ToLower() == email.ToLower());

                if (existingUser != null)
                {
                    if (existingUser.IsDeleted)
                        return (false, "This email is linked to a deleted account. Please contact support to restore it.", null);

                    return (false, "Email is already registered", null);
                }

                // Check for existing username (only active users)
                var existingUsername = await _userManager.FindByNameAsync(username);
                if (existingUsername != null)
                    return (false, "Username is already taken", null);

                // Create user
                var user = new User
                {
                    UserName = username,
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    Address = address,
                    PhoneNumber = phoneNumber,
                    Role = role,
                    AccountStatus = AccountStatus.Active,
                    EmailConfirmed = true,
                    EmailConfirmedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    DeletionType = DeletionType.None
                };

                var result = await _userManager.CreateAsync(user, password);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogWarning("Failed to create user {Username}: {Errors}", username, errors);
                    return (false, $"Failed to create user: {errors}", null);
                }

                // Add user to role
                await _userManager.AddToRoleAsync(user, role);

                _logger.LogInformation("User {Username} created successfully by admin with role {Role}", username, role);

                // Send welcome email
                try
                {
                    await _emailService.SendWelcomeEmailAsync(user.Email, user.FirstName ?? username);
                }
                catch (Exception emailEx)
                {
                    _logger.LogWarning(emailEx, "Failed to send welcome email to {Email}", user.Email);
                }

                return (true, "User created successfully", user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user {Username}", username);
                return (false, "An error occurred while creating the user", null);
            }
        }

        // ==================== PROFILE PICTURE MANAGEMENT ====================

        public async Task<(bool Success, string Message)> UpdateProfilePictureAsync(
            int userId,
            object profilePicture)
        {
            try
            {
                var user = _userRepo.GetById(userId);
                if (user == null)
                    return (false, "User not found");

                // Validate file is an image
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif", ".bmp" };
                var maxFileSize = 10 * 1024 * 1024; // 10 MB

                if (!_fileService.IsValidFile(profilePicture, allowedExtensions, maxFileSize))
                {
                    return (false, "Invalid image file. Please upload a JPG, PNG, or WEBP file under 10MB.");
                }

                // Delete old profile picture if exists
                if (!string.IsNullOrEmpty(user.ProfilePicturePath))
                {
                    _fileService.DeleteFile(user.ProfilePicturePath);
                }

                // Save new profile picture
                var profilePicturePath = await _fileService.SaveFileAsync(profilePicture, "profiles");

                // Update user
                user.ProfilePicturePath = profilePicturePath;
                user.UpdatedAt = DateTime.UtcNow;

                _userRepo.Update(user);
                _userRepo.Save();

                _logger.LogInformation("Profile picture updated for user {UserId}", userId);

                return (true, "Profile picture updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile picture for user {UserId}", userId);
                return (false, "An error occurred while updating your profile picture");
            }
        }

        public async Task<(bool Success, string Message)> RemoveProfilePictureAsync(int userId)
        {
            try
            {
                var user = _userRepo.GetById(userId);
                if (user == null)
                    return (false, "User not found");

                if (string.IsNullOrEmpty(user.ProfilePicturePath))
                    return (false, "No profile picture to remove");

                // Delete file
                _fileService.DeleteFile(user.ProfilePicturePath);

                // Update user
                user.ProfilePicturePath = null;
                user.UpdatedAt = DateTime.UtcNow;

                _userRepo.Update(user);
                _userRepo.Save();

                _logger.LogInformation("Profile picture removed for user {UserId}", userId);

                return await Task.FromResult((true, "Profile picture removed successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing profile picture for user {UserId}", userId);
                return (false, "An error occurred while removing your profile picture");
            }
        }


        // ==================== USER UPDATES ====================

        public async Task<(bool Success, string Message)> UpdateUserAsync(
            int userId,
            string email,
            string username,
            string firstName,
            string lastName,
            string role,
            AccountStatus accountStatus,
            string? address = null,
            string? phoneNumber = null)
        {
            try
            {
                var user = _userRepo.GetById(userId);
                if (user == null)
                    return (false, "User not found");

                // Validation
                if (string.IsNullOrWhiteSpace(email))
                    return (false, "Email is required");

                if (string.IsNullOrWhiteSpace(username))
                    return (false, "Username is required");

                if (string.IsNullOrWhiteSpace(role))
                    return (false, "Role is required");

                // Check if role exists
                if (!await _roleManager.RoleExistsAsync(role))
                    return (false, $"Role '{role}' does not exist");

                // Check email uniqueness
                if (!await IsEmailUniqueAsync(email, userId))
                    return (false, "Email is already in use by another user");

                // Check username uniqueness
                if (!await IsUsernameUniqueAsync(username, userId))
                    return (false, "Username is already taken by another user");

                // Update user properties
                user.UserName = username;
                user.Email = email;
                user.FirstName = firstName;
                user.LastName = lastName;
                user.Address = address;
                user.PhoneNumber = phoneNumber;
                user.AccountStatus = accountStatus;
                user.UpdatedAt = DateTime.UtcNow;

                // Update role if changed
                var currentRoles = await _userManager.GetRolesAsync(user);
                if (!currentRoles.Contains(role))
                {
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                    await _userManager.AddToRoleAsync(user, role);
                }
                user.Role = role;

                // Update via Identity (to sync email/username)
                var result = await _userManager.UpdateAsync(user);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogWarning("Failed to update user {UserId}: {Errors}", userId, errors);
                    return (false, $"Failed to update user: {errors}");
                }

                // Update via repository (for other fields)
                _userRepo.Update(user);
                _userRepo.Save();

                _logger.LogInformation("User {UserId} updated successfully by admin", userId);

                // Send notification email
                try
                {
                    await _emailService.SendAccountUpdatedEmailAsync(user.Email, user.FirstName ?? username);
                }
                catch (Exception emailEx)
                {
                    _logger.LogWarning(emailEx, "Failed to send account update email to {Email}", user.Email);
                }

                return (true, "User updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", userId);
                return (false, "An error occurred while updating the user");
            }
        }

        public async Task<(bool Success, string Message)> UpdateUserProfileAsync(
            int userId,
            string email,
            string firstName,
            string lastName,
            string? address = null,
            string? phoneNumber = null)
        {
            try
            {
                var user = _userRepo.GetById(userId);
                if (user == null)
                    return (false, "User not found");

                // Validation
                if (string.IsNullOrWhiteSpace(email))
                    return (false, "Email is required");

                // Check email uniqueness
                if (!await IsEmailUniqueAsync(email, userId))
                    return (false, "Email is already in use");

                // Update user properties (users can't change role or status)
                user.Email = email;
                user.FirstName = firstName;
                user.LastName = lastName;
                user.Address = address;
                user.PhoneNumber = phoneNumber;
                user.UpdatedAt = DateTime.UtcNow;

                // Update via Identity (to sync email)
                var result = await _userManager.UpdateAsync(user);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogWarning("Failed to update profile for user {UserId}: {Errors}", userId, errors);
                    return (false, $"Failed to update profile: {errors}");
                }

                // Update via repository
                _userRepo.Update(user);
                _userRepo.Save();

                _logger.LogInformation("User {UserId} updated their profile successfully", userId);

                return (true, "Profile updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile for user {UserId}", userId);
                return (false, "An error occurred while updating your profile");
            }
        }

        // ==================== PASSWORD MANAGEMENT ====================

        public async Task<(bool Success, string Message)> ChangePasswordAsync(
            int userId,
            string currentPassword,
            string newPassword)
        {
            try
            {
                var user = _userRepo.GetById(userId);
                if (user == null)
                    return (false, "User not found");

                // Validation
                if (string.IsNullOrWhiteSpace(currentPassword))
                    return (false, "Current password is required");

                if (string.IsNullOrWhiteSpace(newPassword))
                    return (false, "New password is required");

                // Change password via Identity
                var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogWarning("Failed to change password for user {UserId}: {Errors}", userId, errors);
                    return (false, $"Failed to change password: {errors}");
                }

                _logger.LogInformation("User {UserId} changed their password successfully", userId);

                // Send notification email
                try
                {
                    await _emailService.SendPasswordChangedEmailAsync(user.Email, user.FirstName ?? user.UserName);
                }
                catch (Exception emailEx)
                {
                    _logger.LogWarning(emailEx, "Failed to send password changed email to {Email}", user.Email);
                }

                return (true, "Password changed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user {UserId}", userId);
                return (false, "An error occurred while changing your password");
            }
        }

        public async Task<(bool Success, string Message)> AdminChangePasswordAsync(
            int userId,
            string newPassword)
        {
            try
            {
                var user = _userRepo.GetById(userId);
                if (user == null)
                    return (false, "User not found");

                // Validation
                if (string.IsNullOrWhiteSpace(newPassword))
                    return (false, "New password is required");

                // Generate password reset token and reset password
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogWarning("Admin failed to change password for user {UserId}: {Errors}", userId, errors);
                    return (false, $"Failed to change password: {errors}");
                }

                _logger.LogInformation("Admin changed password for user {UserId}", userId);

                // Send notification email
                try
                {
                    await _emailService.SendPasswordChangedByAdminEmailAsync(user.Email, user.FirstName ?? user.UserName);
                }
                catch (Exception emailEx)
                {
                    _logger.LogWarning(emailEx, "Failed to send password changed email to {Email}", user.Email);
                }

                return (true, "Password changed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user {UserId} by admin", userId);
                return (false, "An error occurred while changing the password");
            }
        }

        // ==================== ENHANCED DELETION METHODS ====================

        public async Task<(bool Success, string Message)> CustomerSelfDeleteAsync(int userId)
        {
            try
            {
                var user = _userRepo.GetById(userId);
                if (user == null)
                    return (false, "User not found");

                if (user.Role != "Customer")
                    return (false, "This method is only for customer accounts");

                if (user.IsDeleted)
                    return (false, "User is already deleted");

                var userName = user.UserName ?? "Unknown";
                var userEmail = user.Email ?? "N/A";

                // Perform soft delete (frees username)
                _userRepo.SoftDeleteCustomer(userId);

                _logger.LogInformation("Customer {UserId} ({Username}) self-deleted successfully",
                    userId, userName);

                // Send confirmation email
                try
                {
                    await _emailService.SendAccountDeletedEmailAsync(userEmail, userName);
                }
                catch (Exception emailEx)
                {
                    _logger.LogWarning(emailEx, "Failed to send deletion confirmation to {Email}", userEmail);
                }

                return (true, "Your account has been deleted successfully. You can re-register anytime with the same email.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting customer account {UserId}", userId);
                return (false, "An error occurred while deleting your account.");
            }
        }

        public async Task<(bool Success, string Message)> OrganizerSelfDeleteAsync(
         int userId,
         bool deleteVenues)
        {
            try
            {
                var user = _userRepo.GetById(userId);
                if (user == null)
                    return (false, "User not found");
                if (user.Role != "Organizer")
                    return (false, "This method is only for organizer accounts");
                if (user.IsDeleted)
                    return (false, "User is already deleted");

                // ✅ FIX: Use ApprovalStatus.Approved (not AccountStatus)
                var futureEvents = user.OrganizedEvents
                    .Where(e => e.StartDate > DateTime.UtcNow
                             && e.ApprovalStatus == ApprovalStatus.Approved) // ✅ Changed here
                    .ToList();

                var hasActiveBookings = futureEvents.Any(e =>
                    e.Bookings.Any(b => b.Status != "Cancelled"));

                if (hasActiveBookings && deleteVenues)
                {
                    return (false,
                        "Cannot delete venues with active future bookings. " +
                        "Either wait for events to complete or keep venues active.");
                }

                var userName = user.UserName ?? "Unknown";
                var userEmail = user.Email ?? "N/A";

                // Perform soft delete (deactivation)
                _userRepo.SoftDeleteOrganizer(userId, deleteVenues);

                var venueMessage = deleteVenues
                    ? "Your venues have been marked as deleted. " +
                      "Existing events remain visible and bookable, " +
                      "but no new events can be created in these venues."
                    : "Your venues remain active. Other organizers can continue using them.";

                _logger.LogInformation(
                    "Organizer {UserId} ({Username}) deactivated account. Venues deleted: {DeleteVenues}",
                    userId, userName, deleteVenues);

                // Send confirmation email
                try
                {
                    await _emailService.SendOrganizerAccountDeactivatedEmailAsync(
                        userEmail,
                        userName,
                        deleteVenues,
                        futureEvents.Count);
                }
                catch (Exception emailEx)
                {
                    _logger.LogWarning(emailEx,
                        "Failed to send deactivation confirmation to {Email}", userEmail);
                }

                return (true,
                    $"Your account has been deactivated. " +
                    $"{venueMessage} " +
                    $"Your {futureEvents.Count} future event(s) will continue as scheduled. " +
                    $"Contact an administrator to reactivate your account.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating organizer account {UserId}", userId);
                return (false, "An error occurred while deactivating your account.");
            }
        }

        public async Task<(bool Success, string Message)> AdminBanUserAsync(
            int userId,
            int adminUserId,
            string? reason = null)
        {
            try
            {
                var user = _userRepo.GetById(userId);
                if (user == null)
                    return (false, "User not found");

                if (user.IsDeleted && user.DeletionType == DeletionType.AdminBanned)
                    return (false, "User is already banned");

                var userName = user.UserName ?? "Unknown";
                var userEmail = user.Email ?? "N/A";

                // Perform ban
                _userRepo.AdminBanUser(userId, adminUserId, reason);

                // Add email to banned list
                var bannedEmail = new BannedEmail
                {
                    Email = userEmail.ToLower().Trim(),
                    BannedAt = DateTime.UtcNow,
                    BannedByUserId = adminUserId,
                    Reason = reason,
                    OriginalUserId = userId,
                    OriginalUsername = userName
                };

                _bannedEmailRepo.Add(bannedEmail);
                _bannedEmailRepo.Save();

                _logger.LogWarning(
                    "Admin {AdminId} permanently banned user {UserId} ({Username}). Reason: {Reason}",
                    adminUserId, userId, userName, reason ?? "No reason provided");

                // Send ban notification
                try
                {
                    await _emailService.SendAccountBannedEmailAsync(userEmail, userName, reason);
                }
                catch (Exception emailEx)
                {
                    _logger.LogWarning(emailEx, "Failed to send ban notification to {Email}", userEmail);
                }

                return (true, $"User '{userName}' has been permanently banned. Email '{userEmail}' is now blacklisted.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error banning user {UserId}", userId);
                return (false, "An error occurred while banning the user.");
            }
        }

        public async Task<(bool Success, string Message)> AdminDeleteUserAsync(
            int userId,
            int adminUserId,
            string? reason = null)
        {
            try
            {
                var user = _userRepo.GetById(userId);
                if (user == null)
                    return (false, "User not found");

                if (user.IsDeleted)
                    return (false, "User is already deleted");

                var userName = user.UserName ?? "Unknown";
                var userEmail = user.Email ?? "N/A";

                // Perform admin deletion (can be restored)
                _userRepo.AdminDeleteUser(userId, adminUserId, reason);

                _logger.LogInformation(
                    "Admin {AdminId} deleted user {UserId} ({Username}). Reason: {Reason}",
                    adminUserId, userId, userName, reason ?? "No reason provided");

                // Send notification
                try
                {
                    await _emailService.SendAccountDeletedByAdminEmailAsync(userEmail, userName, reason);
                }
                catch (Exception emailEx)
                {
                    _logger.LogWarning(emailEx, "Failed to send deletion notification to {Email}", userEmail);
                }

                return (true, $"User '{userName}' has been deleted. This action can be reversed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId} by admin", userId);
                return (false, "An error occurred while deleting the user.");
            }
        }

        public async Task<(bool Success, string Message)> RestoreUserAsync(int userId)
        {
            try
            {
                var restored = _userRepo.RestoreDeletedUser(userId);

                if (!restored)
                    return (false, "User not found, already active, or permanently banned");

                var user = await GetUserByIdAsync(userId);

                // Send restoration email
                try
                {
                    await _emailService.SendAccountRestoredEmailAsync(
                        user!.Email!,
                        user.FirstName ?? user.UserName!);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send restore email for user {UserId}", userId);
                }

                _logger.LogInformation("User {UserId} restored successfully", userId);
                return (true, "User restored successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring user {UserId}", userId);
                return (false, "An error occurred while restoring the user");
            }
        }

        // ==================== ACCOUNT STATUS MANAGEMENT ====================

        public async Task<(bool Success, string Message)> ApproveOrganizerAsync(int userId, int adminUserId)
        {
            try
            {
                var user = _userRepo.GetById(userId);
                if (user == null)
                    return (false, "User not found");

                if (user.Role != "Organizer")
                    return (false, "User is not an organizer");

                if (user.AccountStatus == AccountStatus.Active)
                    return (false, "Organizer is already approved");

                user.AccountStatus = AccountStatus.Active;
                user.UpdatedAt = DateTime.UtcNow;

                _userRepo.Update(user);
                _userRepo.Save();

                _logger.LogInformation("Organizer {UserId} approved by admin {AdminId}", userId, adminUserId);

                // Send approval email
                try
                {
                    await _emailService.SendOrganizerApprovedEmailAsync(user.Email, user.FirstName ?? user.UserName);
                }
                catch (Exception emailEx)
                {
                    _logger.LogWarning(emailEx, "Failed to send approval email to {Email}", user.Email);
                }

                return (true, "Organizer approved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving organizer {UserId}", userId);
                return (false, "An error occurred while approving the organizer");
            }
        }

        public async Task<(bool Success, string Message)> RejectOrganizerAsync(int userId, int adminUserId, string? reason = null)
        {
            try
            {
                var user = _userRepo.GetById(userId);
                if (user == null)
                    return (false, "User not found");

                if (user.Role != "Organizer")
                    return (false, "User is not an organizer");

                user.AccountStatus = AccountStatus.Rejected;
                user.UpdatedAt = DateTime.UtcNow;

                _userRepo.Update(user);
                _userRepo.Save();

                _logger.LogInformation("Organizer {UserId} rejected by admin {AdminId}", userId, adminUserId);

                // Send rejection email
                try
                {
                    await _emailService.SendOrganizerRejectedEmailAsync(user.Email, user.FirstName ?? user.UserName, reason);
                }
                catch (Exception emailEx)
                {
                    _logger.LogWarning(emailEx, "Failed to send rejection email to {Email}", user.Email);
                }

                return (true, "Organizer rejected successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting organizer {UserId}", userId);
                return (false, "An error occurred while rejecting the organizer");
            }
        }

        public async Task<(bool Success, string Message)> SetAccountStatusAsync(int userId, AccountStatus status)
        {
            try
            {
                var user = _userRepo.GetById(userId);
                if (user == null)
                    return (false, "User not found");

                user.AccountStatus = status;
                user.UpdatedAt = DateTime.UtcNow;

                _userRepo.Update(user);
                _userRepo.Save();

                _logger.LogInformation("User {UserId} account status set to {Status}", userId, status);

                return await Task.FromResult((true, $"Account status set to {status}"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting account status for user {UserId}", userId);
                return (false, "An error occurred while updating account status");
            }
        }

        public async Task<(bool Success, string Message)> DisableUserAccountAsync(int userId)
        {
            return await SetAccountStatusAsync(userId, AccountStatus.Disabled);
        }

        public async Task<(bool Success, string Message)> EnableUserAccountAsync(int userId)
        {
            return await SetAccountStatusAsync(userId, AccountStatus.Active);
        }

        // ==================== VALIDATION ====================

        public async Task<bool> IsEmailUniqueAsync(string email, int? excludeUserId = null)
        {
            try
            {
                var existingUser = await _userManager.FindByEmailAsync(email);

                if (existingUser == null)
                    return true;

                if (excludeUserId.HasValue && existingUser.Id == excludeUserId.Value)
                    return true;

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking email uniqueness for {Email}", email);
                return false;
            }
        }

        public async Task<bool> IsUsernameUniqueAsync(string username, int? excludeUserId = null)
        {
            try
            {
                var existingUser = await _userManager.FindByNameAsync(username);

                if (existingUser == null)
                    return true;

                if (excludeUserId.HasValue && existingUser.Id == excludeUserId.Value)
                    return true;

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking username uniqueness for {Username}", username);
                return false;
            }
        }

        public async Task<bool> IsEmailBannedAsync(string email)
        {
            try
            {
                return await Task.FromResult(_bannedEmailRepo.IsEmailBanned(email));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if email is banned: {Email}", email);
                return false;
            }
        }

        // ==================== SECURITY ====================

        public async Task<bool> ValidatePasswordAsync(User user, string password)
        {
            try
            {
                if (user == null || string.IsNullOrWhiteSpace(password))
                    return false;

                return await _userManager.CheckPasswordAsync(user, password);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating password for user {UserId}", user?.Id);
                return false;
            }
        }

        public async Task RecordLoginAttemptAsync(int userId, bool success, string ipAddress)
        {
            try
            {
                var user = _userRepo.GetById(userId);
                if (user == null)
                    return;

                if (success)
                {
                    user.LastLoginAt = DateTime.UtcNow;
                    user.LastLoginIp = ipAddress;
                    user.FailedLoginAttempts = 0;
                    user.LastFailedLoginAt = null;
                }
                else
                {
                    user.FailedLoginAttempts++;
                    user.LastFailedLoginAt = DateTime.UtcNow;

                    // Lock account after 5 failed attempts
                    if (user.FailedLoginAttempts >= 5)
                    {
                        user.AccountStatus = AccountStatus.Disabled;
                        _logger.LogWarning("User {UserId} account locked due to failed login attempts", userId);
                    }
                }

                _userRepo.Update(user);
                _userRepo.Save();

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording login attempt for user {UserId}", userId);
            }
        }

        public async Task<bool> IsAccountLockedAsync(int userId)
        {
            try
            {
                var user = _userRepo.GetById(userId);
                if (user == null)
                    return true; // Treat non-existent users as locked

                return await Task.FromResult(
                    user.AccountStatus == AccountStatus.Disabled &&
                    user.FailedLoginAttempts >= 5
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if account is locked for user {UserId}", userId);
                return true; // Err on the side of caution
            }
        }

        public async Task UnlockAccountAsync(int userId)
        {
            try
            {
                var user = _userRepo.GetById(userId);
                if (user == null)
                    return;

                user.FailedLoginAttempts = 0;
                user.LastFailedLoginAt = null;
                user.AccountStatus = AccountStatus.Active;
                user.UpdatedAt = DateTime.UtcNow;

                _userRepo.Update(user);
                _userRepo.Save();

                _logger.LogInformation("User {UserId} account unlocked", userId);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unlocking account for user {UserId}", userId);
            }
        }
    }
}