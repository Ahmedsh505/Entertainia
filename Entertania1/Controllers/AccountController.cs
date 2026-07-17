using Core.Interfaces.ServicesInterface;
using Core.Models;
using Core.Models.AuthModels;
using Entertainia.ViewModels.Account;
using Entertania1.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Entertainia1.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<User> _signInManager;
        private readonly IAuthService _authService;
        private readonly IEmailService _emailService;
        private readonly IFileService _fileService;
        private readonly UserManager<User> _userManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AccountController> _logger;

        // File upload constraints
        private const long MaxFileSize = 100 * 1024 * 1024; // 100 MB
        private const long MaxTotalSize = 2L * 1024 * 1024 * 1024; // 2 GB
        private const int MaxFileCount = 25;

        private readonly string[] _allowedExtensions = new[]
        {
            ".pdf", ".doc", ".docx", ".ppt", ".pptx", ".xls", ".xlsx",
            ".jpg", ".jpeg", ".png", ".bmp", ".webp"
        };

        public AccountController(
            SignInManager<User> signInManager,
            IAuthService authService,
            IEmailService emailService,
            IFileService fileService,
            UserManager<User> userManager,
            IConfiguration configuration,
            ILogger<AccountController> logger)
        {
            _signInManager = signInManager;
            _authService = authService;
            _emailService = emailService;
            _fileService = fileService;
            _userManager = userManager;
            _configuration = configuration;
            _logger = logger;
        }

        #region REGISTRATION

        [HttpGet]
        public IActionResult Register()
        {
            ViewBag.Roles = new List<string> { "Customer", "Organizer" };
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterVM model)
        {
            ViewBag.Roles = new List<string> { "Customer", "Organizer" };

            if (!ModelState.IsValid)
                return View(model);

            // Get IP address
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            // Register user via service
            var (success, message, user) = await _authService.RegisterAsync(
                model.Email!,
                model.UserName!,
                model.Password!,
                model.FirstName!,
                model.LastName!,
                model.Role!,
                model.Address,
                model.PhoneNumber,
                ipAddress);

            if (!success)
            {
                ModelState.AddModelError("", message);
                return View(model);
            }

            // Handle file uploads for organizers
            if (model.Role == "Organizer" && model.VerificationDocuments?.Any() == true)
            {
                if (model.VerificationDocuments.Count > MaxFileCount)
                {
                    ModelState.AddModelError("", $"Maximum {MaxFileCount} documents allowed.");
                    return View(model);
                }

                var totalSize = model.VerificationDocuments.Sum(f => f.Length);
                if (totalSize > MaxTotalSize)
                {
                    ModelState.AddModelError("", $"Total file size exceeds {MaxTotalSize / (1024 * 1024 * 1024)} GB.");
                    return View(model);
                }

                foreach (var file in model.VerificationDocuments)
                {
                    if (!_fileService.IsValidFile(file, _allowedExtensions, MaxFileSize))
                    {
                        ModelState.AddModelError("", $"Invalid file: {file.FileName}");
                        return View(model);
                    }

                    var relativePath = await _fileService.SaveFileAsync(file, "verification");

                    user!.VerificationDocs.Add(new VerificationDoc
                    {
                        UserId = user.Id,
                        FileName = file.FileName,
                        FilePath = relativePath,
                        FileSize = file.Length,
                        ContentType = file.ContentType,
                        UploadedAt = DateTime.UtcNow
                    });
                }
            }

            // ✅ NEW: Generate 6-digit email confirmation code
            var (codeSuccess, codeMessage, code) = await _authService.GenerateEmailConfirmationCodeAsync(
                user!.Id,
                ipAddress
            );

            if (codeSuccess && !string.IsNullOrEmpty(code))
            {
                // Send verification code via email
                try
                {
                    await _emailService.SendEmailConfirmationCodeAsync(
                        user.Email,
                        $"{user.FirstName} {user.LastName}",
                        code
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to send confirmation code: {ex.Message}");
                }
            }

            // Send registration welcome email (optional)
            try
            {
                await _emailService.SendRegistrationConfirmationAsync(
                    user.Email,
                    $"{user.FirstName} {user.LastName}"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send registration email: {ex.Message}");
            }

            // ✅ NEW: Store user info in TempData and redirect to confirmation page
            TempData["PendingConfirmationUserId"] = user.Id;
            TempData["PendingConfirmationEmail"] = user.Email;
            TempData["Success"] = "Registration successful! Please check your email for a verification code.";

            return RedirectToAction("ConfirmEmail");
        }

        #endregion

        #region EMAIL CONFIRMATION

        /// <summary>
        /// ✅ STEP 1: Show 6-digit code entry page
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ConfirmEmail()
        {
            // Check if user just registered
            var userId = TempData["PendingConfirmationUserId"] as int?;
            var email = TempData["PendingConfirmationEmail"] as string;

            if (userId == null || string.IsNullOrEmpty(email))
            {
                // If accessed directly without registration context, redirect to login
                TempData["Error"] = "Please log in to verify your email.";
                return RedirectToAction("Login");
            }

            // Pass data to view
            ViewBag.UserId = userId;
            ViewBag.Email = email;

            // Keep data in TempData for POST request
            TempData.Keep("PendingConfirmationUserId");
            TempData.Keep("PendingConfirmationEmail");

            return View(new ConfirmEmailVM());
        }

        /// <summary>
        /// ✅ STEP 2: Verify 6-digit code (POST)
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmEmail(ConfirmEmailVM model)
        {
            if (!ModelState.IsValid)
            {
                TempData.Keep("PendingConfirmationUserId");
                TempData.Keep("PendingConfirmationEmail");

                // Get email for display
                var emailForDisplay = TempData["PendingConfirmationEmail"] as string;
                ViewBag.Email = emailForDisplay;

                return View(model);
            }

            var userId = TempData["PendingConfirmationUserId"] as int?;
            var email = TempData["PendingConfirmationEmail"] as string;

            if (userId == null)
            {
                TempData["Error"] = "Session expired. Please try logging in again.";
                return RedirectToAction("Login");
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            // Verify the code
            var (success, message) = await _authService.VerifyEmailConfirmationCodeAsync(
                userId.Value,
                model.Code,
                ipAddress
            );

            if (!success)
            {
                ModelState.AddModelError("", message);
                TempData.Keep("PendingConfirmationUserId");
                TempData.Keep("PendingConfirmationEmail");

                // Pass email back to view
                ViewBag.Email = email;

                return View(model);
            }

            // ✅ SUCCESS - Email confirmed!
            TempData["Success"] = message;

            // If organizer, check approval status
            var confirmedUser = await _userManager.FindByIdAsync(userId.ToString()!);
            if (confirmedUser?.Role == "Organizer")
            {
                return RedirectToAction("PendingApproval");
            }

            return RedirectToAction("Login");
        }

        /// <summary>
        /// ✅ STEP 3: Resend 6-digit verification code
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendConfirmationCode()
        {
            var userId = TempData["PendingConfirmationUserId"] as int?;
            var email = TempData["PendingConfirmationEmail"] as string;

            if (userId == null)
            {
                TempData["Error"] = "Session expired. Please try registering again.";
                return RedirectToAction("Register");
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            // Request new code
            var (success, message, code) = await _authService.ResendEmailConfirmationCodeAsync(
                userId.Value,
                ipAddress
            );

            if (!success)
            {
                TempData["Error"] = message;
                TempData.Keep("PendingConfirmationUserId");
                TempData.Keep("PendingConfirmationEmail");
                return RedirectToAction("ConfirmEmail");
            }

            // Send new code via email
            var user = await _userManager.FindByIdAsync(userId.ToString()!);
            if (user != null && !string.IsNullOrEmpty(code))
            {
                try
                {
                    await _emailService.SendEmailConfirmationCodeAsync(
                        user.Email,
                        $"{user.FirstName} {user.LastName}",
                        code
                    );

                    TempData["Success"] = "A new verification code has been sent to your email.";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to send confirmation code to {user.Email}");
                    TempData["Error"] = "Failed to send verification code. Please try again.";
                }
            }

            TempData.Keep("PendingConfirmationUserId");
            TempData.Keep("PendingConfirmationEmail");
            return RedirectToAction("ConfirmEmail");
        }

        /// <summary>
        /// ⚠️ DEPRECATED: Old URL-based confirmation (backward compatibility)
        /// Keep for users with old confirmation links in their email
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        [Route("Account/ConfirmEmailLegacy")]
        public async Task<IActionResult> ConfirmEmailLegacy(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                TempData["Error"] = "Invalid confirmation link.";
                return RedirectToAction("Login");
            }

            var (success, message) = await _authService.ConfirmEmailAsync(token);

            if (!success)
            {
                TempData["Error"] = "This confirmation method is no longer supported. Please request a new verification code.";
                return RedirectToAction("Login");
            }

            TempData["Success"] = "Email confirmed successfully! You can now log in.";
            return RedirectToAction("Login");
        }

        #endregion

        #region LOGIN

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVM model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var deviceInfo = Request.Headers["User-Agent"].ToString();

            // Find user first
            User? user;
            if (model.Identifier.Contains("@"))
                user = await _userManager.FindByEmailAsync(model.Identifier);
            else
                user = await _userManager.FindByNameAsync(model.Identifier);

            if (user != null)
            {
                // ✅ CHECK 1: Email confirmation (HIGHEST PRIORITY - BLOCKS ACCESS)
                if (!user.EmailConfirmed)
                {
                    // Generate new code
                    var (codeSuccess, codeMsg, code) = await _authService.GenerateEmailConfirmationCodeAsync(
                        user.Id,
                        ipAddress
                    );

                    if (codeSuccess && !string.IsNullOrEmpty(code))
                    {
                        try
                        {
                            await _emailService.SendEmailConfirmationCodeAsync(
                                user.Email,
                                $"{user.FirstName} {user.LastName}",
                                code
                            );
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to send confirmation code");
                        }
                    }

                    TempData["Error"] = "Please confirm your email address before logging in. We've sent you a new verification code.";
                    TempData["PendingConfirmationUserId"] = user.Id;
                    TempData["PendingConfirmationEmail"] = user.Email;
                    return RedirectToAction("ConfirmEmail");
                }

                // ✅ CHECK 2: Password validation
                var passwordCheck = await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: false);
                if (!passwordCheck.Succeeded)
                {
                    user.FailedLoginAttempts++;
                    user.LastFailedLoginAt = DateTime.UtcNow;
                    await _userManager.UpdateAsync(user);

                    ModelState.AddModelError("", "Invalid credentials.");
                    return View(model);
                }

                // Reset failed attempts
                user.FailedLoginAttempts = 0;
                user.LastLoginAt = DateTime.UtcNow;
                user.LastLoginIp = ipAddress;
                await _userManager.UpdateAsync(user);

                // ✅ CHECK 3: Two-Factor Authentication
                if (user.TwoFactorEnabled)
                {
                    TempData["2FAUserId"] = user.Id;
                    await _authService.Send2FACodeAsync(user.Id, ipAddress);
                    TempData["Info"] = "A verification code has been sent to your email.";
                    return RedirectToAction("Verify2FA");
                }

                // ✅ CHECK 4: Organizer approval status (ONLY for organizers)
                if (user.Role == "Organizer")
                {
                    if (user.AccountStatus == Core.Enums.AccountStatus.PendingApproval)
                    {
                        await _signInManager.SignInAsync(user, model.RememberMe);
                        TempData["Info"] = "Your account is pending admin approval.";
                        return RedirectToAction("PendingApproval");
                    }

                    if (user.AccountStatus == Core.Enums.AccountStatus.Rejected)
                    {
                        TempData["Error"] = "Your organizer application was rejected. Please contact support.";
                        return RedirectToAction("Rejected");
                    }

                    if (user.AccountStatus == Core.Enums.AccountStatus.Disabled)
                    {
                        TempData["Error"] = "Your account has been disabled. Please contact support.";
                        return View(model);
                    }
                }

                // ✅ ALL CHECKS PASSED - Sign in user
                await _signInManager.SignInAsync(user, model.RememberMe);

                TempData["Success"] = "Login successful!";
                return RedirectToAction("Index", "Home");
            }

            // User not found
            ModelState.AddModelError("", "Invalid credentials.");
            return View(model);
        }

        #endregion

        #region LOGOUT

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

            if (userId > 0)
            {
                await _authService.LogoutAsync(userId);
            }

            await _signInManager.SignOutAsync();

            TempData["Info"] = "You have been logged out.";
            return RedirectToAction("Index", "Home");
        }

        #endregion

        #region PASSWORD RESET

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError("", "Please enter your email address.");
                return View();
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            var (success, message, token) = await _authService.GeneratePasswordResetTokenAsync(
                email,
                ipAddress);

            if (success && !string.IsNullOrEmpty(token))
            {
                var baseUrl = _configuration["AppSettings:BaseUrl"];
                var resetLink = $"{baseUrl}/Account/ResetPassword?token={token}";

                try
                {
                    var user = await _signInManager.UserManager.FindByEmailAsync(email);
                    if (user != null)
                    {
                        await _emailService.SendPasswordResetAsync(
                            user.Email,
                            $"{user.FirstName} {user.LastName}",
                            resetLink!);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to send password reset email: {ex.Message}");
                }
            }

            TempData["Success"] = "If that email is registered, you'll receive a password reset link.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                TempData["Error"] = "Invalid reset link.";
                return RedirectToAction("Login");
            }

            ViewBag.Token = token;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string token, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(newPassword))
            {
                TempData["Error"] = "Invalid request.";
                return RedirectToAction("Login");
            }

            if (newPassword != confirmPassword)
            {
                TempData["Error"] = "Passwords do not match.";
                ViewBag.Token = token;
                return View();
            }

            // Get email from token
            var email = await _authService.GetEmailFromPasswordResetTokenAsync(token);

            if (string.IsNullOrEmpty(email))
            {
                TempData["Error"] = "Invalid or expired reset link.";
                return RedirectToAction("ForgotPassword");
            }

            var (success, message) = await _authService.ResetPasswordAsync(
                email,
                token,
                newPassword);

            if (!success)
            {
                TempData["Error"] = message;
                ViewBag.Token = token;
                return View();
            }

            TempData["Success"] = "Password reset successfully! You can now log in.";
            return RedirectToAction("Login");
        }

        #endregion

        #region TWO-FACTOR AUTHENTICATION

        [HttpGet]
        [Authorize]
        public IActionResult Enable2FA() => View();

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendEnable2FACode()
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

            if (userId == 0)
                return RedirectToAction("Login");

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            var (success, message, code) = await _authService.Send2FACodeAsync(userId, ipAddress);

            if (!success)
            {
                TempData["Error"] = message;
                return RedirectToAction("MyProfile", "Users");
            }

            TempData["Success"] = "Verification code sent to your email!";
            return RedirectToAction("VerifyEnable2FA");
        }

        [HttpGet]
        [Authorize]
        public IActionResult VerifyEnable2FA() => View();

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyEnable2FA(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                TempData["Error"] = "Please enter the verification code.";
                return View();
            }

            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

            if (userId == 0)
                return RedirectToAction("Login");

            var (verifySuccess, verifyMessage) = await _authService.Verify2FACodeAsync(userId, code);

            if (!verifySuccess)
            {
                TempData["Error"] = verifyMessage;
                return View();
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var (enableSuccess, enableMessage) = await _authService.Enable2FAAsync(userId, ipAddress);

            if (!enableSuccess)
            {
                TempData["Error"] = enableMessage;
                return View();
            }

            TempData["Success"] = "Two-factor authentication enabled successfully!";
            return RedirectToAction("MyProfile", "Users");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Disable2FA()
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

            if (userId == 0)
                return RedirectToAction("Login");

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var (success, message) = await _authService.Disable2FAAsync(userId, ipAddress);

            if (!success)
            {
                TempData["Error"] = message;
                return RedirectToAction("MyProfile", "Users");
            }

            TempData["Success"] = message;
            return RedirectToAction("MyProfile", "Users");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Verify2FA(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Verify2FA(string code, string? returnUrl = null)
        {
            if (string.IsNullOrEmpty(code))
            {
                TempData["Error"] = "Please enter the verification code.";
                return View();
            }

            var userId = TempData["2FAUserId"] as int?;
            if (userId == null)
            {
                TempData["Error"] = "Session expired. Please log in again.";
                return RedirectToAction("Login");
            }

            var (success, message) = await _authService.Verify2FACodeAsync(userId.Value, code);

            if (!success)
            {
                TempData["Error"] = message;
                TempData["2FAUserId"] = userId; // Preserve for retry
                return View();
            }

            // Sign in user
            var user = await _signInManager.UserManager.FindByIdAsync(userId.ToString()!);
            if (user != null)
            {
                await _signInManager.SignInAsync(user, false);

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                return RedirectToAction("Index", "Home");
            }

            TempData["Error"] = "An error occurred. Please try again.";
            return RedirectToAction("Login");
        }

        #endregion

        #region STATUS PAGES

        [HttpGet]
        [Authorize(Roles = "Organizer")]
        public IActionResult PendingApproval() => View();

        [HttpGet]
        public IActionResult Rejected() => View();

        [HttpGet]
        public IActionResult AccessDenied() => View();

        #endregion
    }
}