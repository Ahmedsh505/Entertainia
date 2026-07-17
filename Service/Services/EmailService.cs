using Core.Interfaces.ServicesInterface;
using Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Threading.Tasks;

namespace Service.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly string _sendGridApiKey;
        private readonly string _fromEmail;
        private readonly string _fromName;
        private readonly string _baseUrl;
        private readonly IEgyptTimeService _timeService;

        public EmailService(
            IConfiguration configuration,
            ILogger<EmailService> logger,
            IEgyptTimeService timeService)
        {
            _configuration = configuration;
            _logger = logger;

            // The API key must come from configuration (user secrets in Development,
            // environment/host configuration in Production) — never hardcoded.
            _sendGridApiKey = _configuration["SendGrid:ApiKey"]
                ?? _configuration["SendGridSettings:ApiKey"]
                ?? string.Empty;
            _fromEmail = _configuration["SendGrid:FromEmail"] ?? _configuration["SendGridSettings:FromEmail"] ?? "entertaniateam@gmail.com";
            _fromName = _configuration["SendGrid:FromName"] ?? _configuration["SendGridSettings:FromName"] ?? "Entertania Team";
            _baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://entertainia.runasp.net";
            _timeService = timeService;
        }

        #region Private Helper Methods

        private async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlContent, string? plainTextContent = null)
        {
            try
            {
                if (string.IsNullOrEmpty(_sendGridApiKey))
                {
                    _logger.LogWarning("SendGrid API key not configured. Email not sent.");
                    return false;
                }

                var client = new SendGridClient(_sendGridApiKey);
                var from = new EmailAddress(_fromEmail, _fromName);
                var to = new EmailAddress(toEmail);
                var msg = MailHelper.CreateSingleEmail(
                    from,
                    to,
                    subject,
                    plainTextContent ?? StripHtml(htmlContent),
                    htmlContent
                );

                var response = await client.SendEmailAsync(msg);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Email sent successfully to {toEmail}");
                    return true;
                }
                else
                {
                    _logger.LogError($"Failed to send email. Status: {response.StatusCode}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending email to {toEmail}");
                return false;
            }
        }

        private string StripHtml(string html)
        {
            return System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", string.Empty);
        }

        private string GetEmailTemplate(string title, string content)
        {
            return $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>{title}</title>
    <style>
        @import url('https://fonts.googleapis.com/css2?family=Inter:wght@300;400;600;700;800&display=swap');
        
        * {{
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }}
        
        body {{
            font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
            background: #FBFAF7;
            color: #0b2235;
            line-height: 1.6;
            padding: 20px;
        }}
        
        .email-wrapper {{
            max-width: 650px;
            margin: 0 auto;
            background: #FFFFFF;
            border-radius: 16px;
            overflow: hidden;
            box-shadow: 0 12px 40px rgba(11, 34, 53, 0.12);
        }}
        
        .email-header {{
            background: linear-gradient(135deg, #C59D3B 0%, #E0B95A 100%);
            padding: 40px 30px;
            text-align: center;
            position: relative;
            overflow: hidden;
        }}
        
        .email-header::before {{
            content: '';
            position: absolute;
            top: -50%;
            right: -20%;
            width: 300px;
            height: 300px;
            background: rgba(255, 255, 255, 0.1);
            border-radius: 50%;
        }}
        
        .email-header::after {{
            content: '';
            position: absolute;
            bottom: -30%;
            left: -10%;
            width: 200px;
            height: 200px;
            background: rgba(255, 255, 255, 0.08);
            border-radius: 50%;
        }}
        
        .logo {{
            font-size: 32px;
            font-weight: 900;
            color: #FFFFFF;
            letter-spacing: 0.5px;
            margin-bottom: 10px;
            position: relative;
            z-index: 1;
            text-shadow: 0 2px 10px rgba(0, 0, 0, 0.15);
        }}
        
        .logo-icon {{
            display: inline-block;
            margin-right: 8px;
            font-size: 36px;
        }}
        
        .header-subtitle {{
            color: rgba(255, 255, 255, 0.95);
            font-size: 14px;
            font-weight: 500;
            letter-spacing: 0.3px;
            position: relative;
            z-index: 1;
        }}
        
        .email-body {{
            padding: 40px 35px;
        }}
        
        h1, h2, h3 {{
            color: #0b2235;
            margin-bottom: 20px;
            font-weight: 700;
        }}
        
        h2 {{
            font-size: 26px;
            background: linear-gradient(90deg, #C59D3B, #E0B95A);
            -webkit-background-clip: text;
            -webkit-text-fill-color: transparent;
            background-clip: text;
        }}
        
        p {{
            color: #3b4f61;
            margin-bottom: 16px;
            font-size: 15px;
            line-height: 1.7;
        }}
        
        .greeting {{
            font-size: 18px;
            color: #0b2235;
            font-weight: 600;
            margin-bottom: 20px;
        }}
        
        /* Alert Boxes */
        .alert {{
            padding: 20px;
            border-radius: 12px;
            margin: 25px 0;
            border-left: 4px solid;
            background: #f8f9fa;
        }}
        
        .alert-success {{
            background: linear-gradient(135deg, #d4edda 0%, #e8f5e9 100%);
            border-left-color: #28a745;
            color: #155724;
        }}
        
        .alert-info {{
            background: linear-gradient(135deg, #d1ecf1 0%, #e3f2fd 100%);
            border-left-color: #17a2b8;
            color: #0c5460;
        }}
        
        .alert-warning {{
            background: linear-gradient(135deg, #fff3cd 0%, #fff8e1 100%);
            border-left-color: #ffc107;
            color: #856404;
        }}
        
        .alert-danger {{
            background: linear-gradient(135deg, #f8d7da 0%, #ffebee 100%);
            border-left-color: #dc3545;
            color: #721c24;
        }}
        
        .alert strong {{
            display: block;
            font-size: 16px;
            margin-bottom: 8px;
            font-weight: 700;
        }}
        
        /* Details Box */
        .details {{
            background: linear-gradient(135deg, #FBFAF7 0%, #f8f9fa 100%);
            padding: 25px;
            border-radius: 12px;
            margin: 25px 0;
            border: 1px solid #e6e2db;
        }}
        
        .details h3 {{
            font-size: 18px;
            margin-bottom: 18px;
            color: #0b2235;
            padding-bottom: 12px;
            border-bottom: 2px solid #C59D3B;
        }}
        
        .details-row {{
            display: flex;
            justify-content: space-between;
            padding: 12px 0;
            border-bottom: 1px solid #e6e2db;
        }}
        
        .details-row:last-child {{
            border-bottom: none;
        }}
        
        .details-row strong {{
            color: #0b2235;
            font-weight: 600;
        }}
        
        .details-row span:last-child {{
            color: #3b4f61;
            text-align: right;
        }}
        
        /* Highlight Box */
        .highlight {{
            background: linear-gradient(135deg, rgba(197, 157, 59, 0.1) 0%, rgba(224, 185, 90, 0.1) 100%);
            border: 2px solid #C59D3B;
            border-radius: 12px;
            padding: 30px;
            text-align: center;
            margin: 30px 0;
        }}
        
        .highlight-code {{
            font-size: 42px;
            font-weight: 800;
            color: #C59D3B;
            letter-spacing: 8px;
            font-family: 'Courier New', monospace;
            text-shadow: 0 2px 4px rgba(197, 157, 59, 0.2);
        }}
        
        /* Button Styles */
        .btn {{
            display: inline-block;
            padding: 16px 40px;
            background: linear-gradient(135deg, #C59D3B 0%, #E0B95A 100%);
            color: #FFFFFF !important;
            text-decoration: none;
            border-radius: 10px;
            font-weight: 700;
            font-size: 15px;
            margin: 20px 0;
            transition: all 0.3s ease;
            box-shadow: 0 8px 20px rgba(197, 157, 59, 0.25);
            text-align: center;
            letter-spacing: 0.3px;
        }}
        
        .btn:hover {{
            transform: translateY(-2px);
            box-shadow: 0 12px 30px rgba(197, 157, 59, 0.35);
        }}
        
        .btn-secondary {{
            background: linear-gradient(135deg, #3b4f61 0%, #0b2235 100%);
            box-shadow: 0 8px 20px rgba(11, 34, 53, 0.2);
        }}
        
        /* List Styles */
        ul {{
            margin: 20px 0;
            padding-left: 25px;
        }}
        
        ul li {{
            color: #3b4f61;
            margin-bottom: 12px;
            line-height: 1.6;
            font-size: 15px;
        }}
        
        ul li strong {{
            color: #0b2235;
            font-weight: 600;
        }}
        
        /* Footer */
        .email-footer {{
            background: linear-gradient(135deg, #0b2235 0%, #1e3a52 100%);
            padding: 35px 30px;
            text-align: center;
            color: rgba(255, 255, 255, 0.9);
        }}
        
        .footer-border {{
            height: 4px;
            background: linear-gradient(90deg, #C59D3B 0%, #E0B95A 100%);
            margin-bottom: 25px;
        }}
        
        .email-footer p {{
            color: rgba(255, 255, 255, 0.8);
            font-size: 13px;
            margin-bottom: 10px;
        }}
        
        .email-footer a {{
            color: #E0B95A;
            text-decoration: none;
            font-weight: 600;
            transition: color 0.3s ease;
        }}
        
        .email-footer a:hover {{
            color: #C59D3B;
        }}
        
        .social-links {{
            margin: 25px 0 15px;
        }}
        
        .social-link {{
            display: inline-block;
            width: 40px;
            height: 40px;
            background: rgba(255, 255, 255, 0.1);
            border-radius: 50%;
            margin: 0 6px;
            line-height: 40px;
            text-align: center;
            color: #FFFFFF;
            text-decoration: none;
            font-size: 18px;
            transition: all 0.3s ease;
        }}
        
        .social-link:hover {{
            background: linear-gradient(135deg, #C59D3B 0%, #E0B95A 100%);
            transform: translateY(-3px);
        }}
        
        .copyright {{
            font-size: 12px;
            color: rgba(255, 255, 255, 0.6);
            margin-top: 20px;
            padding-top: 20px;
            border-top: 1px solid rgba(255, 255, 255, 0.1);
        }}
        
        /* Responsive */
        @media only screen and (max-width: 600px) {{
            .email-wrapper {{
                border-radius: 0;
            }}
            
            .email-header {{
                padding: 30px 20px;
            }}
            
            .email-body {{
                padding: 30px 20px;
            }}
            
            .logo {{
                font-size: 26px;
            }}
            
            h2 {{
                font-size: 22px;
            }}
            
            .btn {{
                display: block;
                padding: 14px 30px;
            }}
            
            .details-row {{
                flex-direction: column;
                gap: 8px;
            }}
            
            .details-row span:last-child {{
                text-align: left;
            }}
        }}
    </style>
</head>
<body>
    <div class='email-wrapper'>
        <div class='email-header'>
            <div class='logo'>
                <span class='logo-icon'>🎭</span>
                ENTERTANIA
            </div>
            <div class='header-subtitle'>Your Gateway to Unforgettable Experiences</div>
        </div>
        
        <div class='email-body'>
            {content}
        </div>
        
        <div class='email-footer'>
            <div class='footer-border'></div>
            <p style='font-size: 14px; font-weight: 600; color: #FFFFFF; margin-bottom: 15px;'>
                Need Help? We're Here for You
            </p>
            <p>This is an automated message from Entertania.</p>
            <p>Please do not reply directly to this email.</p>
            
            <div class='social-links'>
                <a href='#' class='social-link'>📘</a>
                <a href='#' class='social-link'>📷</a>
                <a href='#' class='social-link'>🐦</a>
                <a href='#' class='social-link'>💼</a>
            </div>
            
            <p>
                <a href='{_baseUrl}'>Visit Our Website</a> | 
                <a href='{_baseUrl}/Contact'>Contact Support</a> | 
                <a href='{_baseUrl}/Events'>Browse Events</a>
            </p>
            
            <div class='copyright'>
                &copy; 2025 Entertania. All rights reserved.<br>
                Premium Event Booking Platform | Cairo, Egypt
            </div>
        </div>
    </div>
</body>
</html>";
        }

        #endregion

        #region User Registration & Authentication

        public async Task<bool> SendRegistrationConfirmationAsync(string toEmail, string userName)
        {
            var content = $@"
                <h2>Welcome to Entertainia! 🎉</h2>
                <p>Dear {userName},</p>
                <p>Thank you for registering with Entertainia! Your account has been created successfully.</p>
                <p>You can now:</p>
                <ul>
                    <li>Browse exciting events</li>
                    <li>Book tickets instantly</li>
                    <li>Manage your bookings</li>
                    <li>Receive exclusive offers</li>
                </ul>
                <a href='{_baseUrl}/Events' class='button'>Explore Events</a>
                <p>If you have any questions, feel free to contact our support team.</p>
                <p>Best regards,<br>The Entertainia Team</p>";

            return await SendEmailAsync(toEmail, "Welcome to Entertainia!", GetEmailTemplate("Welcome", content));
        }

        /// <summary>
        /// ✅ NEW: Send 6-digit email confirmation code
        /// </summary>
        public async Task<bool> SendEmailConfirmationCodeAsync(string toEmail, string userName, string code)
        {
            var content = $@"
        <h2>Confirm Your Email Address</h2>
        <p class='greeting'>Dear {userName},</p>
        <p>Welcome to Entertania! To complete your registration and activate your account, please verify your email address using the code below:</p>
        
        <div class='highlight'>
            <p style='margin: 0 0 10px; color: #3b4f61; font-size: 14px;'>Your Verification Code</p>
            <div class='highlight-code'>{code}</div>
        </div>

        <div class='alert alert-info'>
            <strong>⏰ Important:</strong>
            <ul style='margin: 10px 0;'>
                <li>This code will <strong>expire in 5 minutes</strong></li>
                <li>Enter the code on the confirmation page to activate your account</li>
                <li>Don't share this code with anyone</li>
            </ul>
        </div>

        <p>If you didn't create an account with Entertania, you can safely ignore this email.</p>

        <div class='alert alert-warning'>
            <strong>🔒 Security Notice:</strong>
            <p style='margin: 5px 0 0;'>Entertania will never ask you for this code via email, phone, or any other method. If someone asks for your verification code, do not share it.</p>
        </div>

        <p>Need help? Contact our support team anytime.</p>
        <p><strong>Welcome aboard! 🎉</strong></p>";

            return await SendEmailAsync(
                toEmail,
                $"Your Verification Code: {code}",
                GetEmailTemplate("Email Confirmation", content)
            );
        }

        /// <summary>
        /// ⚠️ DEPRECATED: Old URL-based confirmation (keep for backward compatibility)
        /// </summary>
        public async Task<bool> SendEmailConfirmationAsync(string toEmail, string userName, string confirmationLink)
        {
            var content = $@"
        <h2>Confirm Your Email Address</h2>
        <p>Dear {userName},</p>
        <p>Please confirm your email address by clicking the button below:</p>
        <a href='{confirmationLink}' class='btn'>Confirm Email</a>
        <p>If the button doesn't work, copy and paste this link into your browser:</p>
        <p style='word-break: break-all;'>{confirmationLink}</p>
        <p>This link will expire in 24 hours.</p>
        <p>If you didn't create an account, you can safely ignore this email.</p>";

            return await SendEmailAsync(toEmail, "Confirm Your Email", GetEmailTemplate("Email Confirmation", content));
        }

        public async Task<bool> SendPasswordResetAsync(string toEmail, string userName, string resetLink)
        {
            var content = $@"
                <h2>Reset Your Password</h2>
                <p>Dear {userName},</p>
                <p>We received a request to reset your password. Click the button below to create a new password:</p>
                <a href='{resetLink}' class='button'>Reset Password</a>
                <p>If the button doesn't work, copy and paste this link into your browser:</p>
                <p style='word-break: break-all;'>{resetLink}</p>
                <div class='alert alert-warning'>
                    <strong>⚠ Security Notice:</strong>
                    <p>This link will expire in 1 hour for security reasons.</p>
                    <p>If you didn't request this password reset, please ignore this email or contact support if you're concerned about your account security.</p>
                </div>";

            return await SendEmailAsync(toEmail, "Password Reset Request", GetEmailTemplate("Password Reset", content));
        }

        #endregion

        #region Two-Factor Authentication

        public async Task<bool> Send2FACodeAsync(string toEmail, string userName, string code)
        {
            var content = $@"
                <h2>Your 2FA Verification Code</h2>
                <p>Dear {userName},</p>
                <p>Your two-factor authentication code is:</p>
                <div class='highlight'>{code}</div>
                <p>This code will expire in 10 minutes.</p>
                <div class='alert alert-info'>
                    <strong>🔐 Security Tip:</strong> Never share this code with anyone. Entertainia will never ask for your 2FA code.
                </div>";

            return await SendEmailAsync(toEmail, $"Your 2FA Code: {code}", GetEmailTemplate("2FA Code", content));
        }

        public async Task<bool> Send2FAEnabledNotificationAsync(string toEmail, string userName)
        {
            var content = $@"
                <h2>Two-Factor Authentication Enabled</h2>
                <p>Dear {userName},</p>
                <div class='alert alert-success'>
                    <strong>✅ Success!</strong> Two-factor authentication has been enabled on your account.
                </div>
                <p>Your account is now more secure. You'll need to enter a verification code when logging in.</p>
                <p>If you didn't enable 2FA, please contact support immediately.</p>";

            return await SendEmailAsync(toEmail, "2FA Enabled", GetEmailTemplate("2FA Enabled", content));
        }

        public async Task<bool> Send2FADisabledNotificationAsync(string toEmail, string userName)
        {
            var content = $@"
                <h2>Two-Factor Authentication Disabled</h2>
                <p>Dear {userName},</p>
                <div class='alert alert-warning'>
                    <strong>⚠ Notice:</strong> Two-factor authentication has been disabled on your account.
                </div>
                <p>If you didn't disable 2FA, please contact support immediately and reset your password.</p>";

            return await SendEmailAsync(toEmail, "2FA Disabled", GetEmailTemplate("2FA Disabled", content));
        }

        #endregion

        #region Security Alerts

        public async Task<bool> SendSecurityAlertAsync(string toEmail, string userName, string alertMessage)
        {
            var content = $@"
                <h2>Security Alert</h2>
                <p>Dear {userName},</p>
                <div class='alert alert-danger'>
                    <strong>🔒 Security Alert:</strong> {alertMessage}
                </div>
                <p>If this was you, you can safely ignore this email.</p>
                <p>If you didn't perform this action, please secure your account immediately:</p>
                <ul>
                    <li>Change your password</li>
                    <li>Enable two-factor authentication</li>
                    <li>Contact our support team</li>
                </ul>
                <a href='{_baseUrl}/Account/ChangePassword' class='button'>Change Password</a>";

            return await SendEmailAsync(toEmail, "Security Alert - Entertainia", GetEmailTemplate("Security Alert", content));
        }

        #endregion

        #region Booking Management

        public async Task<bool> SendBookingConfirmationAsync(string toEmail, string userName, string eventName, string bookingDate, string ticketDetails)
        {
            var content = $@"
                <h2>Booking Confirmation 🎉</h2>
                <p>Dear {userName},</p>
                <div class='alert alert-success'>
                    <strong>✅ Booking Created!</strong> Your booking is pending payment confirmation.
                </div>
                <div class='details'>
                    <h3>Booking Details</h3>
                    <div class='details-row'>
                        <span><strong>Event:</strong></span>
                        <span>{eventName}</span>
                    </div>
                    <div class='details-row'>
                        <span><strong>Booking Date:</strong></span>
                        <span>{bookingDate}</span>
                    </div>
                    <div class='details-row'>
                        <span><strong>Tickets:</strong></span>
                        <span>{ticketDetails}</span>
                    </div>
                </div>
                <p>Please complete your payment to confirm your booking.</p>
                <a href='{_baseUrl}/Booking/MyBookings' class='button'>View My Bookings</a>";

            return await SendEmailAsync(toEmail, $"Booking Created - {eventName}", GetEmailTemplate("Booking Confirmation", content));
        }

        public async Task<bool> SendPaymentConfirmationAsync(string toEmail, string userName, string eventName, decimal amount, string currency)
        {
            var content = $@"
                <h2>Payment Successful! 🎊</h2>
                <p>Dear {userName},</p>
                <div class='alert alert-success'>
                    <strong>✅ Payment Confirmed!</strong> Your booking is now confirmed.
                </div>
                <div class='details'>
                    <h3>Payment Details</h3>
                    <div class='details-row'>
                        <span><strong>Event:</strong></span>
                        <span>{eventName}</span>
                    </div>
                    <div class='details-row'>
                        <span><strong>Amount Paid:</strong></span>
                        <span>{amount:N2} {currency}</span>
                    </div>
                    <div class='details-row'>
                        <span><strong>Status:</strong></span>
                        <span style='color: #28a745;'><strong>CONFIRMED</strong></span>
                    </div>
                </div>
                <p>Your tickets are ready! You can view and print them from your bookings page.</p>
                <a href='{_baseUrl}/Booking/MyBookings' class='button'>View My Tickets</a>
                <div class='alert alert-info'>
                    <strong>📱 Important:</strong> Please bring a copy of this confirmation (digital or printed) to the event.
                </div>";

            return await SendEmailAsync(toEmail, $"Payment Confirmed - {eventName}", GetEmailTemplate("Payment Success", content));
        }

        public async Task<bool> SendBookingCancellationAsync(string toEmail, string userName, string eventName, string refundDetails)
        {
            var content = $@"
                <h2>Booking Cancellation</h2>
                <p>Dear {userName},</p>
                <div class='alert alert-warning'>
                    <strong>⚠ Booking Cancelled</strong>
                </div>
                <p>Your booking for <strong>{eventName}</strong> has been cancelled.</p>
                <div class='details'>
                    <h3>Refund Information</h3>
                    <p>{refundDetails}</p>
                </div>
                <p>We're sorry to see you go! We hope to see you at another event soon.</p>
                <a href='{_baseUrl}/Events' class='button'>Browse Events</a>";

            return await SendEmailAsync(toEmail, $"Booking Cancelled - {eventName}", GetEmailTemplate("Booking Cancellation", content));
        }

        public async Task<bool> SendTicketsEmailAsync(string toEmail, string userName, string eventName, int ticketCount, byte[] pdfAttachment)
        {
            try
            {
                var subject = $"🎫 Your Tickets for {eventName}";

                var htmlMessage = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
                    .content {{ background: #f8f9fa; padding: 30px; border-radius: 0 0 10px 10px; }}
                    .ticket-box {{ background: white; padding: 20px; border-left: 4px solid #28a745; margin: 20px 0; border-radius: 5px; }}
                    .warning-box {{ background: #fff3cd; padding: 15px; border-left: 4px solid #ffc107; margin: 20px 0; border-radius: 5px; }}
                    .btn {{ display: inline-block; padding: 12px 30px; background: #667eea; color: white; text-decoration: none; border-radius: 5px; margin: 10px 0; }}
                    .footer {{ text-align: center; margin-top: 20px; color: #666; font-size: 12px; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>🎉 Your Tickets Are Ready!</h1>
                    </div>
                    <div class='content'>
                        <p>Hello <strong>{userName}</strong>,</p>
                        <p>Great news! Your payment has been confirmed and your tickets for <strong>{eventName}</strong> are now ready!</p>
                        
                        <div class='ticket-box'>
                            <h3>📋 Booking Summary</h3>
                            <p><strong>Number of Tickets:</strong> {ticketCount}</p>
                            <p><strong>Event:</strong> {eventName}</p>
                            <p>Your tickets are attached to this email as a PDF file with QR codes.</p>
                        </div>

                        <div class='warning-box'>
                            <p style='margin: 0;'><strong>⚠️ Important Instructions:</strong></p>
                            <ul style='margin: 10px 0;'>
                                <li>📱 <strong>Save this PDF</strong> or have it ready on your phone</li>
                                <li>🎫 <strong>Present the QR code</strong> at the venue entrance</li>
                                <li>🔢 Each ticket has a <strong>unique QR code</strong></li>
                                <li>♻️ Tickets can be <strong>scanned multiple times</strong></li>
                                <li>📧 You can also view your tickets in your bookings page</li>
                            </ul>
                        </div>

                        <p style='text-align: center;'>
                            <a href='https://asteroidal-heritably-jeannine.ngrok-free.dev/Booking/MyBookings' class='btn'>View My Bookings</a>
                        </p>

                        <p>If you have any questions, feel free to contact us.</p>
                        <p><strong>See you at the event! 🎊</strong></p>
                        
                        <div class='footer'>
                            <p>This is an automated email from Entertania. Please do not reply.</p>
                            <p>© 2025 Entertania. All rights reserved.</p>
                        </div>
                    </div>
                </div>
            </body>
            </html>";

                var msg = new SendGridMessage
                {
                    From = new EmailAddress(_fromEmail, _fromName),
                    Subject = subject,
                    HtmlContent = htmlMessage
                };

                msg.AddTo(toEmail);

                // ✅ Add PDF attachment
                var fileName = $"Tickets_{eventName.Replace(" ", "_")}_{DateTime.UtcNow:yyyyMMdd}.pdf";
                msg.AddAttachment(fileName, Convert.ToBase64String(pdfAttachment), "application/pdf");

                var response = await new SendGridClient(_sendGridApiKey).SendEmailAsync(msg);

                if (response.StatusCode == System.Net.HttpStatusCode.OK ||
                    response.StatusCode == System.Net.HttpStatusCode.Accepted)
                {
                    _logger.LogInformation($"✅ Tickets email with PDF sent successfully to {toEmail}");
                    return true;
                }

                _logger.LogError($"Failed to send tickets email: {response.StatusCode}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending tickets email to {toEmail}");
                return false;
            }
        }

        #endregion

        #region Refund Management

        public async Task<bool> SendRefundRequestedAsync(string toEmail, string userName, string eventName, decimal refundAmount, string currency)
        {
            var content = $@"
                <h2>Refund Request Received</h2>
                <p>Dear {userName},</p>
                <div class='alert alert-info'>
                    <strong>📝 Refund Request Submitted</strong>
                </div>
                <p>We have received your refund request for <strong>{eventName}</strong>.</p>
                <div class='details'>
                    <h3>Refund Details</h3>
                    <div class='details-row'>
                        <span><strong>Event:</strong></span>
                        <span>{eventName}</span>
                    </div>
                    <div class='details-row'>
                        <span><strong>Refund Amount:</strong></span>
                        <span>{refundAmount:N2} {currency} (80% of payment)</span>
                    </div>
                    <div class='details-row'>
                        <span><strong>Status:</strong></span>
                        <span style='color: #ffc107;'>Pending Admin Review</span>
                    </div>
                </div>
                <p>Your refund request will be reviewed by our admin team within 1-2 business days.</p>
                <p>You will receive an email notification once your request has been processed.</p>
                <div class='alert alert-info'>
                    <strong>ℹ Refund Policy:</strong>
                    <ul style='margin: 10px 0;'>
                        <li>Refunds are 80% of the total paid amount</li>
                        <li>Processing takes 3-5 business days after approval</li>
                        <li>Refunds are issued to the original payment method</li>
                    </ul>
                </div>";

            return await SendEmailAsync(toEmail, $"Refund Request - {eventName}", GetEmailTemplate("Refund Request", content));
        }

        public async Task<bool> SendRefundApprovedAsync(string toEmail, string userName, string eventName, decimal refundAmount, string currency)
        {
            var content = $@"
                <h2>Refund Approved ✅</h2>
                <p>Dear {userName},</p>
                <div class='alert alert-success'>
                    <strong>✅ Great News!</strong> Your refund request has been approved.
                </div>
                <div class='details'>
                    <h3>Refund Details</h3>
                    <div class='details-row'>
                        <span><strong>Event:</strong></span>
                        <span>{eventName}</span>
                    </div>
                    <div class='details-row'>
                        <span><strong>Refund Amount:</strong></span>
                        <span style='color: #28a745; font-size: 18px;'><strong>{refundAmount:N2} {currency}</strong></span>
                    </div>
                    <div class='details-row'>
                        <span><strong>Processing Time:</strong></span>
                        <span>3-5 business days</span>
                    </div>
                </div>
                <p>Your refund will be processed and credited to your original payment method within 3-5 business days.</p>
                <p>You will receive a notification once the refund has been completed.</p>
                <div class='alert alert-info'>
                    <strong>📌 Note:</strong> The exact time may vary depending on your bank or payment provider.
                </div>";

            return await SendEmailAsync(toEmail, $"Refund Approved - {eventName}", GetEmailTemplate("Refund Approved", content));
        }

        public async Task<bool> SendRefundRejectedAsync(string toEmail, string userName, string eventName, string reason)
        {
            var content = $@"
                <h2>Refund Request Update</h2>
                <p>Dear {userName},</p>
                <div class='alert alert-danger'>
                    <strong>❌ Refund Request Declined</strong>
                </div>
                <p>We regret to inform you that your refund request for <strong>{eventName}</strong> has been declined.</p>
                <div class='details'>
                    <h3>Reason</h3>
                    <p>{reason}</p>
                </div>
                <p>If you believe this decision was made in error or have questions, please contact our support team.</p>
                <a href='{_baseUrl}/Contact' class='button'>Contact Support</a>
                <p>Your original booking remains active and you can still attend the event.</p>";

            return await SendEmailAsync(toEmail, $"Refund Request Update - {eventName}", GetEmailTemplate("Refund Declined", content));
        }

        public async Task SendRefundCompletedAsync(string toEmail, string userName, string eventName, decimal refundAmount, string currency)
        {
            var subject = "Refund Completed - Entertania";
            var htmlContent = $@"
        <h2>Refund Completed</h2>
        <p>Dear {userName},</p>
        <p>Your refund for <strong>{eventName}</strong> has been completed successfully.</p>
        <p><strong>Refund Amount:</strong> {refundAmount:N2} {currency}</p>
        <p>The funds have been returned to your original payment method. Please allow 5-7 business days for the amount to reflect in your account.</p>
        <p>Thank you for using Entertania.</p>
        <p>Best regards,<br>The Entertania Team</p>
    ";

            await SendEmailAsync(toEmail, subject, htmlContent);
        }

        #endregion

        #region Event Management

        public async Task<bool> SendEventUpdateAsync(string toEmail, string userName, string eventName, string updateMessage)
        {
            var content = $@"
                <h2>Event Update</h2>
                <p>Dear {userName},</p>
                <p>There's been an update to an event you're attending:</p>
                <div class='alert alert-info'>
                    <h3>{eventName}</h3>
                    <p>{updateMessage}</p>
                </div>
                <a href='{_baseUrl}/Booking/MyBookings' class='button'>View My Bookings</a>";

            return await SendEmailAsync(toEmail, $"Event Update - {eventName}", GetEmailTemplate("Event Update", content));
        }

        public async Task<bool> SendEventApprovedAsync(string toEmail, string organizerName, string eventName)
        {
            var content = $@"
                <h2>Event Approved! 🎉</h2>
                <p>Dear {organizerName},</p>
                <div class='alert alert-success'>
                    <strong>✅ Congratulations!</strong> Your event has been approved.
                </div>
                <p>Your event <strong>{eventName}</strong> is now live and visible to customers.</p>
                <p>Customers can now browse and book tickets for your event.</p>
                <a href='{_baseUrl}/Events' class='button'>View Event</a>";

            return await SendEmailAsync(toEmail, $"Event Approved - {eventName}", GetEmailTemplate("Event Approved", content));
        }

        public async Task<bool> SendEventRejectedAsync(string toEmail, string organizerName, string eventName, string? reason = null)
        {
            var reasonText = !string.IsNullOrEmpty(reason) ? $"<p><strong>Reason:</strong> {reason}</p>" : "";

            var content = $@"
                <h2>Event Review Update</h2>
                <p>Dear {organizerName},</p>
                <div class='alert alert-danger'>
                    <strong>Event Not Approved</strong>
                </div>
                <p>Your event <strong>{eventName}</strong> was not approved.</p>
                {reasonText}
                <p>You can make the necessary changes and resubmit your event for review.</p>
                <a href='{_baseUrl}/Organizer/Dashboard' class='button'>View Dashboard</a>";

            return await SendEmailAsync(toEmail, $"Event Review - {eventName}", GetEmailTemplate("Event Review", content));
        }

        #endregion

        #region Venue Management

        public async Task<bool> SendVenueApprovedAsync(string toEmail, string organizerName, string venueName)
        {
            var content = $@"
                <h2>Venue Approved! ✅</h2>
                <p>Dear {organizerName},</p>
                <div class='alert alert-success'>
                    <strong>✅ Success!</strong> Your venue has been approved.
                </div>
                <p>Your venue <strong>{venueName}</strong> is now available for creating events.</p>
                <a href='{_baseUrl}/Organizer/Dashboard' class='button'>Create Event</a>";

            return await SendEmailAsync(toEmail, $"Venue Approved - {venueName}", GetEmailTemplate("Venue Approved", content));
        }

        public async Task<bool> SendVenueRejectedAsync(string toEmail, string organizerName, string venueName, string? reason = null)
        {
            var reasonText = !string.IsNullOrEmpty(reason) ? $"<p><strong>Reason:</strong> {reason}</p>" : "";

            var content = $@"
                <h2>Venue Review Update</h2>
                <p>Dear {organizerName},</p>
                <div class='alert alert-danger'>
                    <strong>Venue Not Approved</strong>
                </div>
                <p>Your venue <strong>{venueName}</strong> was not approved.</p>
                {reasonText}
                <p>Please review the requirements and resubmit your venue.</p>
                <a href='{_baseUrl}/Organizer/Dashboard' class='button'>View Dashboard</a>";

            return await SendEmailAsync(toEmail, $"Venue Review - {venueName}", GetEmailTemplate("Venue Review", content));
        }

        #endregion

        #region Organizer Approval

        public async Task<bool> SendOrganizerApprovalAsync(string toEmail, string userName, bool isApproved)
        {
            string content;
            string subject;

            if (isApproved)
            {
                content = $@"
                    <h2>Organizer Account Approved! 🎉</h2>
                    <p>Dear {userName},</p>
                    <div class='alert alert-success'>
                        <strong>✅ Congratulations!</strong> Your organizer account has been approved.
                    </div>
                    <p>You can now:</p>
                    <ul>
                        <li>Create and manage venues</li>
                        <li>Create and publish events</li>
                        <li>Track ticket sales and revenue</li>
                        <li>Manage bookings</li>
                    </ul>
                    <a href='{_baseUrl}/Organizer/Dashboard' class='button'>Go to Dashboard</a>";
                subject = "Organizer Account Approved";
            }
            else
            {
                content = $@"
                    <h2>Organizer Application Status</h2>
                    <p>Dear {userName},</p>
                    <div class='alert alert-danger'>
                        <strong>Application Not Approved</strong>
                    </div>
                    <p>Unfortunately, your organizer application was not approved at this time.</p>
                    <p>Please ensure all required documents are provided and meet our requirements.</p>
                    <p>You may reapply after addressing the issues.</p>
                    <a href='{_baseUrl}/Contact' class='button'>Contact Support</a>";
                subject = "Organizer Application Update";
            }

            return await SendEmailAsync(toEmail, subject, GetEmailTemplate(subject, content));
        }

        #endregion

        #region ACCOUNT STATUS EMAILS

        // ====================== ACCOUNT STATUS EMAILS ======================

        public async Task<bool> SendWelcomeEmailAsync(string email, string name)
        {
            var content = $@"
        <h2>Welcome to Entertainia! 🎉</h2>
        <p>Dear {name},</p>
        <p>Our Platform Team Has created Your Account successfully!</p>
        <p>We Wish You a Joyful Experience.</p>
        <p>You can now explore events and start booking.</p>
        <a href='{_baseUrl}/Events' class='button'>Browse Events</a>";

            return await SendEmailAsync(email, "Welcome to Entertainia!", GetEmailTemplate("Welcome", content));
        }

        public async Task<bool> SendAccountUpdatedEmailAsync(string email, string name)
        {
            var content = $@"
        <h2>Account Updated</h2>
        <p>Dear {name},</p>
        <p>Your account information has been updated successfully.</p>";

            return await SendEmailAsync(email, "Account Updated", GetEmailTemplate("Account Updated", content));
        }

        public async Task<bool> SendAccountDeletedEmailAsync(string email, string name)
        {
            var content = $@"
        <h2>Account Deleted</h2>
        <p>Dear {name},</p>
        <p>Your account has been deleted. We're sorry to see you leave.</p>";

            return await SendEmailAsync(email, "Account Deleted", GetEmailTemplate("Account Deleted", content));
        }

        /*public async Task<bool> SendAccountRestoredEmailAsync(string email, string name)
        {
            var content = $@"
         <h2>Account Restored</h2>
         <p>Dear {name},</p>
         <p>Your account has been successfully restored and is now active again.</p>
         <p>If this wasn't you, please reset your password immediately.</p>
         <p>Welcome back to <strong>Entertania</strong>!</p>";

            return await SendEmailAsync(email, "Account Restored", GetEmailTemplate("Account Restored", content));
        }*/

        #endregion

        #region PASSWORD STATUS EMAILS

        // ====================== PASSWORD STATUS EMAILS ======================

        public async Task<bool> SendPasswordChangedEmailAsync(string email, string name)
        {
            var content = $@"
        <h2>Password Changed</h2>
        <p>Dear {name},</p>
        <p>Your password has been updated successfully.</p>
        <div class='alert alert-info'>
            <strong>Note:</strong> If this wasn't you, please reset password immediately.
        </div>";

            return await SendEmailAsync(email, "Password Changed", GetEmailTemplate("Password Changed", content));
        }

        public async Task<bool> SendPasswordChangedByAdminEmailAsync(string email, string name)
        {
            var content = $@"
        <h2>Password Changed by Admin</h2>
        <p>Dear {name},</p>
        <p>Your password has been changed by an administrator.</p>
        <div class='alert alert-warning'>
            <strong>If this was not expected:</strong> Please change your password immediately and contact support.
        </div>";

            return await SendEmailAsync(email, "Password Changed by Admin", GetEmailTemplate("Password Changed By Admin", content));
        }

        #endregion

        #region ORGANIZER APPROVAL

        // ====================== ORGANIZER APPROVAL ======================

        public async Task<bool> SendOrganizerApprovedEmailAsync(string email, string name)
        {
            var content = $@"
        <h2>Organizer Approved! ✅</h2>
        <p>Dear {name},</p>
        <p>Your organizer account has been approved.</p>
        <p>You can now create venues and events.</p>
        <a href='{_baseUrl}/Organizer/Dashboard' class='button'>Go to Dashboard</a>";

            return await SendEmailAsync(email, "Organizer Approved", GetEmailTemplate("Organizer Approved", content));
        }

        public async Task<bool> SendOrganizerRejectedEmailAsync(string email, string name, string? reason)
        {
            var reasonText = !string.IsNullOrEmpty(reason) ? $"<p><strong>Reason:</strong> {reason}</p>" : "";

            var content = $@"
        <h2>Organizer Application Rejected ❌</h2>
        <p>Dear {name},</p>
        <p>Your organizer request was not approved.</p>
        {reasonText}
        <a href='{_baseUrl}/Contact' class='button'>Contact Support</a>";

            return await SendEmailAsync(email, "Organizer Application Rejected", GetEmailTemplate("Organizer Rejected", content));
        }

        #endregion

        #region ACCOUNT Deleted EMAILS
        public async Task SendAccountBannedEmailAsync(string toEmail, string userName, string? reason)
        {
            var subject = "Account Permanently Banned - Entertania";
            var htmlContent = $@"
        <h2>Account Banned</h2>
        <p>Dear {userName},</p>
        <p>Your account has been permanently banned by an administrator.</p>
        {(string.IsNullOrEmpty(reason) ? "" : $"<p><strong>Reason:</strong> {reason}</p>")}
        <p>This email address cannot be used to register again.</p>
        <p>If you believe this is a mistake, please contact support.</p>
        <hr/>
        <p>Entertania Team</p>
    ";

            await SendEmailAsync(toEmail, subject, htmlContent);
        }

        public async Task SendAccountDeletedByAdminEmailAsync(string toEmail, string userName, string? reason)
        {
            var subject = "Account Deleted - Entertania";
            var htmlContent = $@"
        <h2>Account Deleted</h2>
        <p>Dear {userName},</p>
        <p>Your account has been deleted by an administrator.</p>
        {(string.IsNullOrEmpty(reason) ? "" : $"<p><strong>Reason:</strong> {reason}</p>")}
        <p>Your account may be restored by contacting support.</p>
        <hr/>
        <p>Entertania Team</p>
    ";

            await SendEmailAsync(toEmail, subject, htmlContent);
        }

        public async Task SendAccountRestoredEmailAsync(string toEmail, string userName)
        {
            var subject = "Account Restored - Entertania";
            var htmlContent = $@"
        <h2>Account Restored</h2>
        <p>Dear {userName},</p>
        <p>Great news! Your account has been restored.</p>
        <p>You can now log in and access your account normally.</p>
        <p><a href='https://yourdomain.com/Account/Login'>Click here to login</a></p>
        <hr/>
        <p>Entertania Team</p>
    ";

            await SendEmailAsync(toEmail, subject, htmlContent);
        }
        #endregion

        #region ORGANIZER ACCOUNT DEACTIVATED
        public async Task<bool> SendOrganizerAccountDeactivatedEmailAsync(
     string toEmail,
     string userName,
     bool deleteVenues,
     int futureEventsCount)
        {
            string subject = "Organizer Account Deactivated";

            string venueMessage = deleteVenues
                ? "Your venues have been marked as deleted. They are no longer available for new events."
                : "Your venues remain active and can still be used by other organizers.";

            string eventMessage = futureEventsCount > 0
                ? $"You still have <strong>{futureEventsCount}</strong> future event(s) that will remain visible and bookable."
                : "You currently have no future events.";

            string content = $@"
        <h2>Organizer Account Deactivated</h2>
        <p>Dear {userName},</p>
        <div class='alert alert-warning'>
            <strong>⚠️ Your organizer account has been deactivated.</strong>
        </div>
        <p>{venueMessage}</p>
        <p>{eventMessage}</p>
        <p>If you wish to reactivate your account, please contact our support team.</p>
        <a href='{_baseUrl}/Contact' class='button'>Contact Support</a>";

            return await SendEmailAsync(toEmail, subject, GetEmailTemplate(subject, content));
        }

        #endregion

        #region EMAIL FAILURE
        public async Task SendEmailFailureAsync(string to, string subject, string htmlMessage)
        {
            try
            {
                // Your email sending logic goes here (SMTP / MailKit / SendGrid)

                _logger.LogInformation("Email sent to {Email}", to);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}", to);
                throw;
            }
        }
        #endregion

    }
}