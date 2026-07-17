using Core.Enums;
using Core.Interfaces.ServicesInterface;
using Core.Interfaces.UnitOfWorkInterface;
using Core.Models.AuthModels;
using Microsoft.Extensions.Logging;

namespace Service.Services
{
    /// <summary>
    /// Service for admin-specific business logic
    /// NO UI DEPENDENCIES - Works with domain models only!
    /// </summary>
    public class AdminService : IAdminService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;
        private readonly IFileStorageService _fileStorageService;
        private readonly ILogger<AdminService> _logger;
        private readonly IEgyptTimeService _timeService;

        public AdminService(
            IUnitOfWork unitOfWork,
            IEmailService emailService,
            IFileStorageService fileStorageService,
            ILogger<AdminService> logger,
            IEgyptTimeService timeService)
        {
            _unitOfWork = unitOfWork;
            _emailService = emailService;
            _fileStorageService = fileStorageService;
            _logger = logger;
            _timeService = new EgyptTimeService();
        }

        #region Dashboard

        public async Task<AdminDashboardData> GetDashboardDataAsync()
        {
            try
            {
                var data = new AdminDashboardData();

                // Get pending approvals
                data.PendingOrganizers = _unitOfWork.Users.GetPendingOrganizers();
                data.PendingVenues = _unitOfWork.Venues.GetPendingVenues();
                data.PendingEvents = _unitOfWork.Events.GetPendingEvents();
                data.PendingRefunds = _unitOfWork.Refunds.GetPendingRefunds();

                // Build statistics
                data.EventStats = await BuildEventStatisticsAsync();
                data.VenueOccupancy = BuildVenueOccupancyInfo();
                data.RevenueStats = BuildRevenueStatistics();
                data.PaymentStats = BuildPaymentStatistics();

                _logger.LogInformation("Dashboard data compiled successfully");
                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building dashboard data");
                throw;
            }
        }

        private async Task<EventStatisticsData> BuildEventStatisticsAsync()
        {
            var allEvents = _unitOfWork.Events.GetAll();
            var now = DateTime.UtcNow;

            return new EventStatisticsData
            {
                TotalEvents = allEvents.Count,
                UpcomingEvents = allEvents.Count(e => e.StartDate > now && e.ApprovalStatus == ApprovalStatus.Approved),
                CurrentEvents = allEvents.Count(e => e.StartDate <= now && e.EndDate >= now && e.ApprovalStatus == ApprovalStatus.Approved),
                EndedEvents = allEvents.Count(e => e.EndDate < now && e.ApprovalStatus == ApprovalStatus.Approved),
                PendingApproval = allEvents.Count(e => e.ApprovalStatus == ApprovalStatus.Pending),
                ApprovedEvents = allEvents.Count(e => e.ApprovalStatus == ApprovalStatus.Approved),
                RejectedEvents = allEvents.Count(e => e.ApprovalStatus == ApprovalStatus.Rejected),
                UpcomingEventsList = allEvents
                    .Where(e => e.StartDate > now && e.ApprovalStatus == ApprovalStatus.Approved)
                    .OrderBy(e => e.StartDate)
                    .Take(5)
                    .ToList(),
                CurrentEventsList = allEvents
                    .Where(e => e.StartDate <= now && e.EndDate >= now && e.ApprovalStatus == ApprovalStatus.Approved)
                    .ToList(),
                RecentlyEndedEvents = allEvents
                    .Where(e => e.EndDate < now && e.ApprovalStatus == ApprovalStatus.Approved)
                    .OrderByDescending(e => e.EndDate)
                    .Take(5)
                    .ToList()
            };
        }

        private List<VenueOccupancyData> BuildVenueOccupancyInfo()
        {
            var allVenues = _unitOfWork.Venues.GetAll();

            return allVenues.Select(v => new VenueOccupancyData
            {
                VenueId = v.VenueId,
                VenueName = v.Name ?? string.Empty,
                Location = v.Location ?? string.Empty,
                Capacity = v.Capacity,
                IsCurrentlyOccupied = v.IsCurrentlyOccupied(),
                CurrentEvent = v.GetCurrentEvent(),
                TotalEventsScheduled = v.Events.Count(e => e.ApprovalStatus == ApprovalStatus.Approved)
            }).ToList();
        }

        private RevenueStatisticsData BuildRevenueStatistics()
        {
            var allBookings = _unitOfWork.Bookings.GetAll()
                .Where(b => b.Status == "Confirmed")
                .ToList();

            var stats = new RevenueStatisticsData
            {
                TotalRevenue = allBookings.Sum(b => b.TotalPrice),
                TotalTaxCollected = allBookings.Sum(b => b.TaxAmount),
                TotalDiscountsGiven = allBookings.Sum(b => b.DiscountAmount),
                TotalRefunded = _unitOfWork.Refunds.GetAll()
                    .Where(r => r.Status == PaymentStatus.Completed)
                    .Sum(r => r.RefundAmount),
                RevenueByEvent = allBookings
                    .GroupBy(b => b.Event.Title)
                    .ToDictionary(g => g.Key ?? "Unknown", g => g.Sum(b => b.TotalWithTax)),
                RevenueByCurrency = allBookings
                    .GroupBy(b => b.Currency)
                    .ToDictionary(g => g.Key.ToString(), g => g.Sum(b => b.TotalWithTax)),
                BookingsByEvent = allBookings
                    .GroupBy(b => b.Event.Title)
                    .ToDictionary(g => g.Key ?? "Unknown", g => g.Count())
            };

            stats.NetRevenue = stats.TotalRevenue - stats.TotalRefunded;

            return stats;
        }

        private PaymentStatisticsData BuildPaymentStatistics()
        {
            var allPayments = _unitOfWork.Payments.GetAll();

            return new PaymentStatisticsData
            {
                TotalPayments = allPayments.Count,
                CompletedPayments = allPayments.Count(p => p.Status == PaymentStatus.Completed),
                PendingPayments = allPayments.Count(p => p.Status == PaymentStatus.Pending),
                FailedPayments = allPayments.Count(p => p.Status == PaymentStatus.Failed),
                RefundedPayments = allPayments.Count(p => p.Status == PaymentStatus.Refunded),
                TotalAmountProcessed = allPayments
                    .Where(p => p.Status == PaymentStatus.Completed)
                    .Sum(p => p.TotalAmountWithTax),
                TotalAmountRefunded = _unitOfWork.Refunds.GetAll()
                    .Where(r => r.Status == PaymentStatus.Completed)
                    .Sum(r => r.RefundAmount),
                SuccessRate = allPayments.Count > 0
                    ? (double)allPayments.Count(p => p.Status == PaymentStatus.Completed) / allPayments.Count * 100
                    : 0
            };
        }

        #endregion

        #region Organizer Management

        public async Task<(bool Success, string Message)> ApproveOrganizerAsync(int organizerId)
        {
            try
            {
                var organizer = _unitOfWork.Users.GetById(organizerId);
                if (organizer == null)
                    return (false, "Organizer not found");

                if (organizer.Role != "Organizer")
                    return (false, "User is not an organizer");

                _unitOfWork.Users.SetAccountStatus(organizerId, AccountStatus.Active);

                try
                {
                    await _emailService.SendOrganizerApprovalAsync(
                        organizer.Email,
                        $"{organizer.FirstName} {organizer.LastName}",
                        true
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Failed to send organizer approval email to {organizer.Email}");
                }

                _logger.LogInformation($"Organizer {organizerId} approved successfully");
                return (true, "Organizer approved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error approving organizer {organizerId}");
                return (false, $"Error approving organizer: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> RejectOrganizerAsync(int organizerId)
        {
            try
            {
                var organizer = _unitOfWork.Users.GetById(organizerId);
                if (organizer == null)
                    return (false, "Organizer not found");

                if (organizer.Role != "Organizer")
                    return (false, "User is not an organizer");

                _unitOfWork.Users.SetAccountStatus(organizerId, AccountStatus.Rejected);

                try
                {
                    await _emailService.SendOrganizerApprovalAsync(
                        organizer.Email,
                        $"{organizer.FirstName} {organizer.LastName}",
                        false
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Failed to send organizer rejection email to {organizer.Email}");
                }

                _logger.LogInformation($"Organizer {organizerId} rejected");
                return (true, "Organizer rejected successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error rejecting organizer {organizerId}");
                return (false, $"Error rejecting organizer: {ex.Message}");
            }
        }

        #endregion

        #region Venue Management

        public async Task<(bool Success, string Message)> ApproveVenueAsync(int venueId, int adminId)
        {
            try
            {
                var venue = _unitOfWork.Venues.GetById(venueId);
                if (venue == null)
                    return (false, "Venue not found");

                _unitOfWork.Venues.SetApprovalStatus(venueId, ApprovalStatus.Approved, adminId);

                if (venue.CreatedByUser != null)
                {
                    try
                    {
                        await _emailService.SendVenueApprovedAsync(
                            venue.CreatedByUser.Email,
                            $"{venue.CreatedByUser.FirstName} {venue.CreatedByUser.LastName}",
                            venue.Name ?? "Venue"
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Failed to send venue approval email for venue {venueId}");
                    }
                }

                _logger.LogInformation($"Venue {venueId} approved by admin {adminId}");
                return (true, "Venue approved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error approving venue {venueId}");
                return (false, $"Error approving venue: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> RejectVenueAsync(int venueId, int adminId)
        {
            try
            {
                var venue = _unitOfWork.Venues.GetById(venueId);
                if (venue == null)
                    return (false, "Venue not found");

                _unitOfWork.Venues.SetApprovalStatus(venueId, ApprovalStatus.Rejected, adminId);

                if (venue.CreatedByUser != null)
                {
                    try
                    {
                        await _emailService.SendVenueRejectedAsync(
                            venue.CreatedByUser.Email,
                            $"{venue.CreatedByUser.FirstName} {venue.CreatedByUser.LastName}",
                            venue.Name ?? "Venue",
                            "Venue does not meet our quality standards or requirements."
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Failed to send venue rejection email for venue {venueId}");
                    }
                }

                _logger.LogInformation($"Venue {venueId} rejected by admin {adminId}");
                return (true, "Venue rejected successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error rejecting venue {venueId}");
                return (false, $"Error rejecting venue: {ex.Message}");
            }
        }

        #endregion

        #region Event Management

        public async Task<(bool Success, string Message)> ApproveEventAsync(int eventId, int adminId)
        {
            try
            {
                var eventItem = _unitOfWork.Events.GetById(eventId);
                if (eventItem == null)
                    return (false, "Event not found");

                _unitOfWork.Events.SetApprovalStatus(eventId, ApprovalStatus.Approved, adminId);

                if (eventItem.Organizer != null)
                {
                    try
                    {
                        await _emailService.SendEventApprovedAsync(
                            eventItem.Organizer.Email,
                            $"{eventItem.Organizer.FirstName} {eventItem.Organizer.LastName}",
                            eventItem.Title ?? "Event"
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Failed to send event approval email for event {eventId}");
                    }
                }

                _logger.LogInformation($"Event {eventId} approved by admin {adminId}");
                return (true, "Event approved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error approving event {eventId}");
                return (false, $"Error approving event: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> RejectEventAsync(int eventId, int adminId)
        {
            try
            {
                var eventItem = _unitOfWork.Events.GetById(eventId);
                if (eventItem == null)
                    return (false, "Event not found");

                _unitOfWork.Events.SetApprovalStatus(eventId, ApprovalStatus.Rejected, adminId);

                if (eventItem.Organizer != null)
                {
                    try
                    {
                        await _emailService.SendEventRejectedAsync(
                            eventItem.Organizer.Email,
                            $"{eventItem.Organizer.FirstName} {eventItem.Organizer.LastName}",
                            eventItem.Title ?? "Event",
                            "Event does not meet our content guidelines or requirements."
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Failed to send event rejection email for event {eventId}");
                    }
                }

                _logger.LogInformation($"Event {eventId} rejected by admin {adminId}");
                return (true, "Event rejected successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error rejecting event {eventId}");
                return (false, $"Error rejecting event: {ex.Message}");
            }
        }

        #endregion

        #region Refund Management

        public async Task<(bool Success, string Message)> ApproveRefundAsync(int refundId, int adminId)
        {
            try
            {
                var refund = _unitOfWork.Refunds.GetById(refundId);
                if (refund == null)
                    return (false, "Refund not found");

                refund.Status = PaymentStatus.Completed;
                refund.ProcessedByUserId = adminId;
                refund.ProcessedAt = DateTime.UtcNow;
                refund.CompletedAt = DateTime.UtcNow;
                _unitOfWork.Refunds.Update(refund);

                var booking = _unitOfWork.Bookings.GetById(refund.BookingId);
                if (booking != null)
                {
                    booking.Status = "Refunded";
                    _unitOfWork.Bookings.Update(booking);

                    try
                    {
                        var user = _unitOfWork.Users.GetById(booking.UserId);
                        if (user != null)
                        {
                            await _emailService.SendRefundApprovedAsync(
                                user.Email,
                                $"{user.FirstName} {user.LastName}",
                                booking.Event?.Title ?? "Event",
                                refund.RefundAmount,
                                refund.Currency.ToString()
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Failed to send refund approval email for refund {refundId}");
                    }
                }

                var payment = _unitOfWork.Payments.GetById(refund.PaymentId);
                if (payment != null)
                {
                    payment.Status = PaymentStatus.Refunded;
                    _unitOfWork.Payments.Update(payment);
                }

                _unitOfWork.Complete();

                _logger.LogInformation($"Refund {refundId} approved by admin {adminId}");
                return (true, "Refund approved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error approving refund {refundId}");
                return (false, $"Error approving refund: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> RejectRefundAsync(int refundId, int adminId, string reason)
        {
            try
            {
                var refund = _unitOfWork.Refunds.GetById(refundId);
                if (refund == null)
                    return (false, "Refund not found");

                refund.Status = PaymentStatus.Failed;
                refund.ProcessedByUserId = adminId;
                refund.ProcessedAt = DateTime.UtcNow;
                refund.FailedAt = DateTime.UtcNow;
                refund.FailureReason = reason;
                refund.AdminNotes = reason;
                _unitOfWork.Refunds.Update(refund);

                try
                {
                    var booking = _unitOfWork.Bookings.GetById(refund.BookingId);
                    if (booking != null)
                    {
                        var user = _unitOfWork.Users.GetById(booking.UserId);
                        if (user != null)
                        {
                            await _emailService.SendRefundRejectedAsync(
                                user.Email,
                                $"{user.FirstName} {user.LastName}",
                                booking.Event?.Title ?? "Event",
                                reason ?? "Refund request does not meet our refund policy criteria."
                            );
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Failed to send refund rejection email for refund {refundId}");
                }

                _unitOfWork.Complete();

                _logger.LogInformation($"Refund {refundId} rejected by admin {adminId}");
                return (true, "Refund rejected successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error rejecting refund {refundId}");
                return (false, $"Error rejecting refund: {ex.Message}");
            }
        }

        #endregion

        #region Document Management

        public async Task<List<VerificationDoc>> GetOrganizerDocumentsAsync(int organizerId)
        {
            try
            {
                var user = _unitOfWork.Users.GetById(organizerId);
                if (user == null)
                {
                    _logger.LogWarning($"Organizer {organizerId} not found");
                    return new List<VerificationDoc>();
                }

                return user.VerificationDocs?.ToList() ?? new List<VerificationDoc>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching organizer documents for organizer {organizerId}");
                return new List<VerificationDoc>();
            }
        }

        public async Task<(bool Success, VerificationDoc? Document, string ErrorMessage)> GetVerificationDocumentAsync(int docId)
        {
            try
            {
                var doc = _unitOfWork.Users.GetAll()
                    .SelectMany(u => u.VerificationDocs)
                    .FirstOrDefault(d => d.VerificationDocId == docId);

                if (doc == null)
                    return (false, null, "Document not found");

                return (true, doc, string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching verification document {docId}");
                return (false, null, $"Error retrieving document: {ex.Message}");
            }
        }

        #endregion
    }
}