using System.Threading.Tasks;

namespace Core.Interfaces.ServicesInterface
{
    public interface IEmailService
    {
        // ===== USER REGISTRATION & AUTHENTICATION =====
        Task<bool> SendRegistrationConfirmationAsync(string toEmail, string userName);

        // DEPRECATED - Old URL-based confirmation
        Task<bool> SendEmailConfirmationAsync(string toEmail, string userName, string confirmationLink);

        // New code-based confirmation
        Task<bool> SendEmailConfirmationCodeAsync(string toEmail, string userName, string code);

        Task<bool> SendPasswordResetAsync(string toEmail, string userName, string resetLink);

        // ===== TWO-FACTOR AUTHENTICATION =====
        Task<bool> Send2FACodeAsync(string toEmail, string userName, string code);
        Task<bool> Send2FAEnabledNotificationAsync(string toEmail, string userName);
        Task<bool> Send2FADisabledNotificationAsync(string toEmail, string userName);

        // ===== SECURITY ALERTS =====
        Task<bool> SendSecurityAlertAsync(string toEmail, string userName, string alertMessage);

        // ===== BOOKING MANAGEMENT =====
        Task<bool> SendBookingConfirmationAsync(string toEmail, string userName, string eventName, string bookingDate, string ticketDetails);
        Task<bool> SendBookingCancellationAsync(string toEmail, string userName, string eventName, string refundDetails);
        Task<bool> SendPaymentConfirmationAsync(string toEmail, string userName, string eventName, decimal amount, string currency);
        Task SendRefundCompletedAsync(string toEmail, string userName, string eventName, decimal refundAmount, string currency);
        Task<bool> SendTicketsEmailAsync(string toEmail, string userName, string eventName, int ticketCount, byte[] pdfAttachment);

        // ===== REFUND MANAGEMENT =====
        /// <summary>
        /// Send notification when customer submits refund request
        /// </summary>
        Task<bool> SendRefundRequestedAsync(string toEmail, string userName, string eventName, decimal refundAmount, string currency);

        /// <summary>
        /// Send notification when admin approves refund
        /// </summary>
        Task<bool> SendRefundApprovedAsync(string toEmail, string userName, string eventName, decimal refundAmount, string currency);

        /// <summary>
        /// Send notification when admin rejects refund
        /// </summary>
        Task<bool> SendRefundRejectedAsync(string toEmail, string userName, string eventName, string reason);

        // ===== EVENT MANAGEMENT =====
        Task<bool> SendEventUpdateAsync(string toEmail, string userName, string eventName, string updateMessage);
        Task<bool> SendEventApprovedAsync(string toEmail, string organizerName, string eventName);
        Task<bool> SendEventRejectedAsync(string toEmail, string organizerName, string eventName, string? reason = null);

        // ===== VENUE MANAGEMENT =====
        Task<bool> SendVenueApprovedAsync(string toEmail, string organizerName, string venueName);
        Task<bool> SendVenueRejectedAsync(string toEmail, string organizerName, string venueName, string? reason = null);

        // ===== ORGANIZER APPROVAL =====
        Task<bool> SendOrganizerApprovalAsync(string toEmail, string userName, bool isApproved);

        // User lifecycle
        Task<bool> SendWelcomeEmailAsync(string email, string name);
        Task<bool> SendAccountUpdatedEmailAsync(string email, string name);
        Task<bool> SendAccountDeletedEmailAsync(string email, string name);

        Task<bool> SendOrganizerAccountDeactivatedEmailAsync(string toEmail, string userName, bool deleteVenues, int futureEventsCount);
        //Task<bool> SendAccountRestoredEmailAsync(string email, string name);

        // Password management
        Task<bool> SendPasswordChangedEmailAsync(string email, string name);
        Task<bool> SendPasswordChangedByAdminEmailAsync(string email, string name);

        // Organizer approval workflow
        Task<bool> SendOrganizerApprovedEmailAsync(string email, string name);
        Task<bool> SendOrganizerRejectedEmailAsync(string email, string name, string? reason);

        /// <summary>
        /// Send email when user account is banned by admin
        /// </summary>
        Task SendAccountBannedEmailAsync(string toEmail, string userName, string? reason);

        /// <summary>
        /// Send email when admin deletes user account (restorable)
        /// </summary>
        Task SendAccountDeletedByAdminEmailAsync(string toEmail, string userName, string? reason);

        /// <summary>
        /// Send email when user account is restored
        /// </summary>
        Task SendAccountRestoredEmailAsync(string toEmail, string userName);
        Task SendEmailFailureAsync(string to, string subject, string htmlMessage);



    }
}