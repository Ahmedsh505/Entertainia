using Core.Enums;
using Core.Interfaces.ServicesInterface;
using Core.Models;
using Entertania1.ViewModels.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Entertania1.Controllers
{
    /// <summary>
    /// Controller for user management
    /// Handles both admin actions (CRUD) and user self-service (profile, password)
    /// </summary>
    public class UsersController : Controller
    {
        private readonly IUserService _userService;
        private readonly SignInManager<User> _signInManager;

        public UsersController(IUserService userService, SignInManager<User> signInManager)
        {
            _userService = userService;
            _signInManager = signInManager;
        }

        // ==================== ADMIN ACTIONS ====================

        /// <summary>
        /// Display all users with search, filter, sort, and pagination
        /// </summary>
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index(
            string? search,
            string? filterValue,
            string? sortColumn,
            string? sortDirection,
            int page = 1)
        {
            const int pageSize = 10;
            List<User> users;

            // ✅ Updated filter options
            if (filterValue == "Deleted")
                users = await _userService.GetDeletedUsersAsync();
            else if (filterValue == "Banned")
                users = await _userService.GetBannedUsersAsync();
            else
                users = await _userService.GetAllUsersAsync();

            var usersQuery = users.AsQueryable();

            // Search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower().Trim();
                usersQuery = usersQuery.Where(u =>
                    (!string.IsNullOrEmpty(u.FirstName) && u.FirstName.ToLower().Contains(searchLower)) ||
                    (!string.IsNullOrEmpty(u.LastName) && u.LastName.ToLower().Contains(searchLower)) ||
                    (!string.IsNullOrEmpty(u.Email) && u.Email.ToLower().Contains(searchLower)) ||
                    (!string.IsNullOrEmpty(u.OriginalUsername) && u.OriginalUsername.ToLower().Contains(searchLower))
                );
            }

            // Role / Status / Deleted filter
            if (!string.IsNullOrWhiteSpace(filterValue))
            {
                if (filterValue == "Admin" || filterValue == "Organizer" || filterValue == "Customer")
                {
                    usersQuery = usersQuery.Where(u => u.Role == filterValue && !u.IsDeleted);
                }
                else if (filterValue == "Deleted")
                {
                    usersQuery = usersQuery.Where(u => u.IsDeleted && u.DeletionType != DeletionType.AdminBanned);
                }
                else if (filterValue == "Banned")
                {
                    usersQuery = usersQuery.Where(u => u.IsDeleted && u.DeletionType == DeletionType.AdminBanned);
                }
                else
                {
                    usersQuery = usersQuery.Where(u => u.AccountStatus.ToString() == filterValue && !u.IsDeleted);
                }
            }
            else
            {
                // Default: exclude deleted and banned users
                usersQuery = usersQuery.Where(u => !u.IsDeleted);
            }

            // Sorting
            bool ascending = sortDirection?.ToLower() != "desc";
            usersQuery = sortColumn switch
            {
                "Name" => ascending
                    ? usersQuery.OrderBy(u => u.FirstName ?? "").ThenBy(u => u.LastName ?? "")
                    : usersQuery.OrderByDescending(u => u.FirstName ?? "").ThenByDescending(u => u.LastName ?? ""),
                "Email" => ascending
                    ? usersQuery.OrderBy(u => u.Email ?? "")
                    : usersQuery.OrderByDescending(u => u.Email ?? ""),
                "Role" => ascending
                    ? usersQuery.OrderBy(u => u.Role ?? "")
                    : usersQuery.OrderByDescending(u => u.Role ?? ""),
                "Status" => ascending
                    ? usersQuery.OrderBy(u => u.AccountStatus)
                    : usersQuery.OrderByDescending(u => u.AccountStatus),
                _ => usersQuery.OrderBy(u => u.FirstName ?? "").ThenBy(u => u.LastName ?? "")
            };

            // Pagination
            var totalUsers = usersQuery.Count();
            var totalPages = (int)Math.Ceiling(totalUsers / (double)pageSize);
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            var pagedUsers = usersQuery.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            // ViewBag for maintaining state
            ViewBag.Search = search;
            ViewBag.FilterValue = filterValue;
            ViewBag.SortColumn = sortColumn;
            ViewBag.SortDirection = sortDirection;
            ViewBag.Page = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalUsers = totalUsers;

            return View(pagedUsers);
        }

        /// <summary>
        /// Display user details
        /// </summary>
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(user);
        }

        /// <summary>
        /// Display create user form
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        /// <summary>
        /// Create a new user
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(CreateUserVM model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _userService.CreateUserAsync(
                email: model.Email!,
                username: model.UserName!,
                password: model.Password!,
                firstName: model.FirstName!,
                lastName: model.LastName!,
                role: model.Role!,
                address: model.Address,
                phoneNumber: model.PhoneNumber
            );

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError("", result.Message);
            return View(model);
        }

        /// <summary>
        /// Display edit user form
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(Index));
            }

            var vm = new EditUserVM
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Address = user.Address,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role!,
                AccountStatus = user.AccountStatus
            };

            return View(vm);
        }

        /// <summary>
        /// Update user information
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(EditUserVM model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _userService.UpdateUserAsync(
                userId: model.Id,
                email: model.Email!,
                username: model.UserName!,
                firstName: model.FirstName!,
                lastName: model.LastName!,
                role: model.Role!,
                accountStatus: model.AccountStatus,
                address: model.Address,
                phoneNumber: model.PhoneNumber
            );

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError("", result.Message);
            return View(model);
        }

        /// <summary>
        /// Display delete confirmation
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(user);
        }

        // ==================== CUSTOMER SELF-DELETION ====================

        /// <summary>
        /// Customer deletes their own account
        /// </summary>
        /// 
        [HttpGet]
        [Authorize(Roles = "Customer")]
        public IActionResult DeleteCustomerAccount()
        {
            return View();
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> DeleteMyCustomerAccount()
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

            if (userId == 0)
            {
                TempData["ErrorMessage"] = "Unable to identify user.";
                return RedirectToAction("Index", "Home");
            }

            var result = await _userService.CustomerSelfDeleteAsync(userId);

            if (result.Success)
            {
                // Sign out the user
                await _signInManager.SignOutAsync();
                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction("Index", "Home");
            }

            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(MyProfile));
        }

        // ==================== ORGANIZER SELF-DELETION ====================

        /// <summary>
        /// Display organizer deletion options page
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Organizer")]
        public IActionResult DeleteOrganizerAccount()
        {
            return View();
        }

        /// <summary>
        /// Organizer deletes their account with venue choice
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Organizer")]
        public async Task<IActionResult> DeleteOrganizerAccount(bool deleteVenues)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

            if (userId == 0)
            {
                TempData["ErrorMessage"] = "Unable to identify user.";
                return RedirectToAction("Index", "Home");
            }

            var result = await _userService.OrganizerSelfDeleteAsync(userId, deleteVenues);

            if (result.Success)
            {
                // Sign out the user
                await _signInManager.SignOutAsync();
                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction("Index", "Home");
            }

            TempData["ErrorMessage"] = result.Message;
            return View();
        }

        // ==================== ADMIN DELETION ACTIONS ====================

        /// <summary>
        /// Admin soft deletes user (can be restored)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminDelete(int id, string? reason)
        {
            var adminUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

            var result = await _userService.AdminDeleteUserAsync(id, adminUserId, reason);

            if (result.Success)
                TempData["SuccessMessage"] = result.Message;
            else
                TempData["ErrorMessage"] = result.Message;

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Admin permanently bans user
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminBan(int id, string? reason)
        {
            var adminUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

            var result = await _userService.AdminBanUserAsync(id, adminUserId, reason);

            if (result.Success)
                TempData["SuccessMessage"] = result.Message;
            else
                TempData["ErrorMessage"] = result.Message;

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Admin restores a deleted user
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(int id)
        {
            var result = await _userService.RestoreUserAsync(id);

            if (result.Success)
                TempData["SuccessMessage"] = result.Message;
            else
                TempData["ErrorMessage"] = result.Message;

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// View all banned users
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> BannedUsers()
        {
            var bannedUsers = await _userService.GetBannedUsersAsync();
            return View(bannedUsers);
        }

        // ==================== USER SELF-MANAGEMENT ====================

        /// <summary>
        /// Display current user's profile
        /// </summary>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> MyProfile()
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Index", "Home");
            }

            return View(user);
        }

        // Add these methods to your existing UsersController.cs

        /// <summary>
        /// Display edit profile form
        /// </summary>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> EditProfile()
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Index", "Home");
            }

            var vm = new EditProfileVM
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Address = user.Address,
                PhoneNumber = user.PhoneNumber,
                Email = user.Email,
                CurrentProfilePicturePath = user.ProfilePicturePath // ✅ NEW
            };

            return View(vm);
        }

        /// <summary>
        /// Update user's own profile
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> EditProfile(EditProfileVM model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

            // ✅ NEW: Handle profile picture upload
            if (model.ProfilePicture != null)
            {
                var pictureResult = await _userService.UpdateProfilePictureAsync(userId, model.ProfilePicture);
                if (!pictureResult.Success)
                {
                    ModelState.AddModelError("ProfilePicture", pictureResult.Message);
                    return View(model);
                }
            }

            var result = await _userService.UpdateUserProfileAsync(
                userId: userId,
                email: model.Email!,
                firstName: model.FirstName!,
                lastName: model.LastName!,
                address: model.Address,
                phoneNumber: model.PhoneNumber
            );

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction(nameof(MyProfile));
            }

            ModelState.AddModelError("", result.Message);
            return View(model);
        }

        /// <summary>
        /// Remove user's profile picture
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> RemoveProfilePicture()
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

            var result = await _userService.RemoveProfilePictureAsync(userId);

            if (result.Success)
                TempData["SuccessMessage"] = result.Message;
            else
                TempData["ErrorMessage"] = result.Message;

            return RedirectToAction(nameof(EditProfile));
        }

        /// <summary>
        /// Display change password form
        /// </summary>
        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View();
        }

        /// <summary>
        /// Change user's password
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> ChangePassword(ChangePasswordVM model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

            var result = await _userService.ChangePasswordAsync(
                userId: userId,
                currentPassword: model.CurrentPassword!,
                newPassword: model.NewPassword!
            );

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction(nameof(MyProfile));
            }

            ModelState.AddModelError("", result.Message);
            return View(model);
        }

        /// <summary>
        /// Display admin change password form
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminChangePassword(int id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(Index));
            }

            var vm = new AdminChangePasswordVM
            {
                UserId = user.Id,
                UserName = user.UserName
            };

            return View(vm);
        }

        /// <summary>
        /// Admin changes any user's password
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminChangePassword(AdminChangePasswordVM model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _userService.AdminChangePasswordAsync(
                userId: model.UserId,
                newPassword: model.NewPassword!
            );

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError("", result.Message);
            return View(model);
        }
    }
}