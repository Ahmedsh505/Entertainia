using Core.Interfaces.ServicesInterface;
using Core.Interfaces.UnitOfWorkInterface;
using Core.Models.TicketModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QRCoder;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Geom;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Service.Services
{
    public class TicketService : ITicketService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;
        private readonly ILogger<TicketService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IEgyptTimeService _timeService;

        private string SecretKey => _configuration["Jwt:SecretKey"] ?? "default-secret-key";
        private string BaseUrl => _configuration["AppSettings:BaseUrl"] ?? "https://asteroidal-heritably-jeannine.ngrok-free.dev";

        public TicketService(
            IUnitOfWork unitOfWork,
            IEmailService emailService,
            ILogger<TicketService> logger,
            IConfiguration configuration,
            IEgyptTimeService timeService)
        {
            _unitOfWork = unitOfWork;
            _emailService = emailService;
            _logger = logger;
            _configuration = configuration;
            _timeService = timeService;
        }

        #region Generate Tickets

        public async Task<(bool Success, string Message, List<Ticket>? Tickets)> GenerateTicketsForBookingAsync(int bookingId)
        {
            try
            {
                _logger.LogInformation($"Generating tickets for booking {bookingId}");

                var booking = _unitOfWork.Bookings.GetById(bookingId);
                if (booking == null)
                {
                    return (false, "Booking not found", null);
                }

                if (booking.Status != "Confirmed")
                {
                    return (false, "Only confirmed bookings can have tickets generated", null);
                }

                var existingTickets = _unitOfWork.Tickets.GetByBookingId(bookingId);
                if (existingTickets.Any())
                {
                    _logger.LogWarning($"Tickets already exist for booking {bookingId}");

                    bool needsUpdate = false;
                    foreach (var ticket in existingTickets)
                    {
                        if (string.IsNullOrEmpty(ticket.QRCodeData) || string.IsNullOrEmpty(ticket.VerificationHash))
                        {
                            ticket.GenerateQRCodeData(SecretKey, BaseUrl);
                            _unitOfWork.Tickets.Update(ticket);
                            needsUpdate = true;
                            _logger.LogInformation($"✅ Regenerated QR for existing ticket {ticket.TicketId}");
                        }
                    }

                    if (needsUpdate)
                    {
                        _unitOfWork.Complete();
                    }

                    return (true, "Tickets already generated", existingTickets);
                }

                var tickets = new List<Ticket>();

                for (int i = 1; i <= booking.NumberOfTickets; i++)
                {
                    var ticket = new Ticket
                    {
                        BookingId = bookingId,
                        EventId = booking.EventId,
                        UserId = booking.UserId,
                        TicketNumber = i,
                        Tier = booking.TicketTier,
                        Status = "Active",
                        CreatedAt = DateTime.UtcNow,
                        ExpiresAt = booking.Event?.EndDate ?? booking.Event?.StartDate.AddHours(24)
                    };

                    _unitOfWork.Tickets.Add(ticket);
                    tickets.Add(ticket);
                }

                _unitOfWork.Complete();
                _logger.LogInformation($"✅ Created {tickets.Count} ticket records with IDs");

                foreach (var ticket in tickets)
                {
                    try
                    {
                        ticket.GenerateQRCodeData(SecretKey, BaseUrl);
                        _unitOfWork.Tickets.Update(ticket);
                        _logger.LogInformation($"✅ Ticket {ticket.TicketId}: QR = {ticket.QRCodeData}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error generating QR for ticket {ticket.TicketId}");
                    }
                }

                _unitOfWork.Complete();
                _logger.LogInformation($"✅ Successfully generated {tickets.Count} tickets with QR codes for booking {bookingId}");

                return (true, $"Successfully generated {tickets.Count} ticket(s)", tickets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating tickets for booking {bookingId}");
                return (false, $"Failed to generate tickets: {ex.Message}", null);
            }
        }

        #endregion

        #region Retrieve Tickets

        public async Task<Ticket?> GetTicketByIdAsync(int ticketId)
        {
            return await Task.Run(() => _unitOfWork.Tickets.GetById(ticketId));
        }

        public async Task<Ticket?> GetTicketByHashAsync(string verificationHash)
        {
            return await Task.Run(() => _unitOfWork.Tickets.GetByVerificationHash(verificationHash));
        }

        public async Task<List<Ticket>> GetTicketsByBookingIdAsync(int bookingId)
        {
            return await Task.Run(() => _unitOfWork.Tickets.GetByBookingId(bookingId));
        }

        public async Task<List<Ticket>> GetTicketsByEventIdAsync(int eventId)
        {
            return await Task.Run(() => _unitOfWork.Tickets.GetByEventId(eventId));
        }

        public async Task<List<Ticket>> GetTicketsByUserIdAsync(int userId)
        {
            return await Task.Run(() => _unitOfWork.Tickets.GetByUserId(userId));
        }

        #endregion

        #region Verify & Scan

        public async Task<(bool IsValid, string Message, Ticket? Ticket)> VerifyTicketAsync(string qrCodeData)
        {
            try
            {
                var parts = qrCodeData.Split('|');
                if (parts.Length != 4)
                {
                    return (false, "Invalid QR code format", null);
                }

                if (!int.TryParse(parts[0], out int ticketId) ||
                    !int.TryParse(parts[1], out int bookingId) ||
                    !int.TryParse(parts[2], out int eventId))
                {
                    return (false, "Invalid ticket data", null);
                }

                var hash = parts[3];
                var ticket = await GetTicketByIdAsync(ticketId);

                if (ticket == null)
                {
                    return (false, "Ticket not found", null);
                }

                if (ticket.VerificationHash != hash)
                {
                    _logger.LogWarning($"Hash mismatch for ticket {ticketId}");
                    return (false, "Invalid ticket - verification failed", null);
                }

                if (ticket.BookingId != bookingId || ticket.EventId != eventId)
                {
                    return (false, "Ticket data mismatch", null);
                }

                if (ticket.ExpiresAt.HasValue && ticket.ExpiresAt.Value < DateTime.UtcNow)
                {
                    return (false, "Ticket has expired", ticket);
                }

                if (ticket.Status != "Active")
                {
                    return (false, $"Ticket is {ticket.Status.ToLower()}", ticket);
                }

                return (true, "Ticket is valid", ticket);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying ticket");
                return (false, "Verification error", null);
            }
        }

        public async Task<(bool Success, string Message)> ScanTicketAsync(int ticketId, int scannedByUserId)
        {
            try
            {
                var ticket = await GetTicketByIdAsync(ticketId);
                if (ticket == null)
                {
                    return (false, "Ticket not found");
                }

                if (!ticket.IsValid())
                {
                    return (false, $"Ticket is not valid - Status: {ticket.Status}");
                }

                ticket.MarkAsScanned();
                _unitOfWork.Tickets.Update(ticket);
                _unitOfWork.Complete();

                _logger.LogInformation($"Ticket {ticketId} scanned by user {scannedByUserId}. Scan count: {ticket.ScanCount}");

                return (true, $"Ticket scanned successfully (Scan #{ticket.ScanCount})");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error scanning ticket {ticketId}");
                return (false, "Failed to scan ticket");
            }
        }

        #endregion

        #region Cancel Tickets

        public async Task<(bool Success, string Message)> CancelTicketAsync(int ticketId)
        {
            try
            {
                var ticket = await GetTicketByIdAsync(ticketId);
                if (ticket == null)
                {
                    return (false, "Ticket not found");
                }

                ticket.Status = "Cancelled";
                _unitOfWork.Tickets.Update(ticket);
                _unitOfWork.Complete();

                return (true, "Ticket cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error cancelling ticket {ticketId}");
                return (false, "Failed to cancel ticket");
            }
        }

        public async Task<(bool Success, string Message)> CancelTicketsForBookingAsync(int bookingId)
        {
            try
            {
                var tickets = await GetTicketsByBookingIdAsync(bookingId);
                foreach (var ticket in tickets)
                {
                    ticket.Status = "Cancelled";
                    _unitOfWork.Tickets.Update(ticket);
                }

                _unitOfWork.Complete();
                return (true, $"{tickets.Count} ticket(s) cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error cancelling tickets for booking {bookingId}");
                return (false, "Failed to cancel tickets");
            }
        }

        #endregion

        #region QR Code Generation

        public string GenerateQRCodeBase64(string qrCodeData)
        {
            try
            {
                using var qrGenerator = new QRCodeGenerator();
                using var qrCodeData_obj = qrGenerator.CreateQrCode(qrCodeData, QRCodeGenerator.ECCLevel.Q);
                using var qrCode = new PngByteQRCode(qrCodeData_obj);

                var qrCodeBytes = qrCode.GetGraphic(20);
                return Convert.ToBase64String(qrCodeBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating QR code");
                return string.Empty;
            }
        }

        public byte[] GenerateQRCodeBytes(string qrCodeData)
        {
            try
            {
                using var qrGenerator = new QRCodeGenerator();
                using var qrCodeData_obj = qrGenerator.CreateQrCode(qrCodeData, QRCodeGenerator.ECCLevel.Q);
                using var qrCode = new PngByteQRCode(qrCodeData_obj);

                return qrCode.GetGraphic(20);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating QR code bytes");
                return Array.Empty<byte>();
            }
        }

        #endregion

        #region PDF Generation - Enhanced with Beautiful Layout

        public async Task<byte[]> GenerateTicketsPDFAsync(int bookingId)
        {
            MemoryStream memoryStream = null;
            PdfWriter writer = null;
            PdfDocument pdf = null;
            Document document = null;

            try
            {
                _logger.LogInformation($"📄 Generating enhanced PDF for booking {bookingId}");

                var booking = _unitOfWork.Bookings.GetById(bookingId);
                if (booking == null)
                {
                    throw new Exception("Booking not found");
                }

                var tickets = await GetTicketsByBookingIdAsync(bookingId);
                if (!tickets.Any())
                {
                    throw new Exception("No tickets found for this booking");
                }

                memoryStream = new MemoryStream();
                writer = new PdfWriter(memoryStream);
                writer.SetCloseStream(false);

                pdf = new PdfDocument(writer);
                document = new Document(pdf, PageSize.A4);
                document.SetMargins(40, 40, 40, 40);

                // Generate each ticket on a separate page
                for (int i = 0; i < tickets.Count; i++)
                {
                    var ticket = tickets[i];

                    if (i > 0)
                    {
                        document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
                    }

                    AddTicketPage(document, ticket, booking, tickets.Count);
                }

                document.Close();
                pdf.Close();
                writer.Close();

                memoryStream.Position = 0;
                var pdfBytes = memoryStream.ToArray();

                _logger.LogInformation($"✅ PDF generated successfully: {pdfBytes.Length} bytes for {tickets.Count} tickets");

                return pdfBytes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error generating PDF for booking {bookingId}");
                throw new Exception($"Failed to generate PDF: {ex.Message}", ex);
            }
            finally
            {
                document?.Close();
                pdf?.Close();
                writer?.Close();
                memoryStream?.Dispose();
            }
        }

        private void AddTicketPage(Document document, Ticket ticket, Core.Models.BookingModels.Booking booking, int totalTickets)
        {
            // ===== HEADER - Verification Status =====
            var statusColor = ticket.Status == "Active" ? ColorConstants.GREEN : ColorConstants.RED;
            var statusIcon = ticket.Status == "Active" ? "✓" : "✗";

            var headerTable = new Table(1).UseAllAvailableWidth();
            headerTable.SetBackgroundColor(statusColor);

            var headerCell = new Cell()
                .Add(new Paragraph($"{statusIcon} Ticket Verification")
                    .SetFontSize(20)
                    .SetBold()
                    .SetFontColor(ColorConstants.WHITE)
                    .SetTextAlignment(TextAlignment.CENTER))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetPadding(15);

            headerTable.AddCell(headerCell);
            document.Add(headerTable);

            // Status Message
            var statusMsg = ticket.Status == "Active" ? "Ticket is valid ✓" : $"Ticket is {ticket.Status}";
            var statusPara = new Paragraph(statusMsg)
                .SetFontSize(16)
                .SetBold()
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFontColor(statusColor)
                .SetMarginTop(10)
                .SetMarginBottom(20);
            document.Add(statusPara);

            // ===== QR CODE =====
            try
            {
                if (string.IsNullOrEmpty(ticket.QRCodeData))
                {
                    ticket.GenerateQRCodeData(SecretKey, BaseUrl);
                    _unitOfWork.Tickets.Update(ticket);
                    _unitOfWork.Complete();
                }

                var qrCodeBytes = GenerateQRCodeBytes(ticket.QRCodeData);
                if (qrCodeBytes.Length > 0)
                {
                    var imageData = ImageDataFactory.Create(qrCodeBytes);
                    var qrImage = new iText.Layout.Element.Image(imageData)
                        .SetWidth(250)
                        .SetHeight(250)
                        .SetHorizontalAlignment(HorizontalAlignment.CENTER)
                        .SetMarginBottom(20);
                    document.Add(qrImage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding QR code for ticket {ticket.TicketId}");
            }

            // ===== TICKET DETAILS CARD =====
            AddSectionCard(document, "🎫 Ticket Details", ColorConstants.BLUE, () =>
            {
                var detailsTable = new Table(2).UseAllAvailableWidth();

                AddDetailRow(detailsTable, "Ticket Number", $"Ticket #{ticket.TicketNumber} of {totalTickets}");
                AddDetailRow(detailsTable, "Ticket ID", ticket.TicketId.ToString());
                AddDetailRow(detailsTable, "Tier", ticket.Tier.ToString());
                AddDetailRow(detailsTable, "Status", ticket.Status);

                document.Add(detailsTable);
            });

            // ===== EVENT INFORMATION CARD =====
            if (booking?.Event != null)
            {
                AddSectionCard(document, "📅 Event Information", ColorConstants.CYAN, () =>
                {
                    document.Add(new Paragraph(booking.Event.Title)
                        .SetFontSize(16)
                        .SetBold()
                        .SetMarginBottom(5));

                    if (!string.IsNullOrEmpty(booking.Event.Description))
                    {
                        document.Add(new Paragraph(booking.Event.Description)
                            .SetFontSize(10)
                            .SetFontColor(ColorConstants.DARK_GRAY)
                            .SetMarginBottom(10));
                    }

                    var eventTable = new Table(2).UseAllAvailableWidth();
                    AddDetailRow(eventTable, "Event Date", booking.Event.StartDate.ToString("dd MMM yyyy HH:mm"));
                    AddDetailRow(eventTable, "Category", booking.Event.Category ?? "N/A");

                    // ✅ ENHANCED VENUE DISPLAY
                    if (booking.Event.Venue != null)
                    {
                        var venue = booking.Event.Venue;
                        var governorateName = GetGovernorateDisplayName(venue.Governorate);

                        AddDetailRow(eventTable, "Venue", venue.Name ?? "Unknown Venue");

                        // Format location properly
                        var locationParts = new List<string>();
                        if (!string.IsNullOrWhiteSpace(venue.Location))
                        {
                            locationParts.Add(venue.Location);
                        }
                        if (!string.IsNullOrWhiteSpace(governorateName))
                        {
                            locationParts.Add(governorateName);
                        }
                        locationParts.Add("Egypt");

                        var fullLocation = locationParts.Count > 0
                            ? string.Join(", ", locationParts)
                            : "Location not specified";

                        AddDetailRow(eventTable, "Location", fullLocation);

                        // Add Google Maps URL if available
                        if (!string.IsNullOrWhiteSpace(venue.GoogleMapsUrl))
                        {
                            document.Add(eventTable);
                            document.Add(new Paragraph($"📍 Maps: {venue.GoogleMapsUrl}")
                                .SetFontSize(9)
                                .SetFontColor(ColorConstants.BLUE)
                                .SetMarginTop(5));
                            return;
                        }
                    }

                    document.Add(eventTable);
                });
            }

            // ===== CUSTOMER INFORMATION CARD =====
            if (booking?.User != null)
            {
                AddSectionCard(document, "👤 Customer Information", ColorConstants.GRAY, () =>
                {
                    var userTable = new Table(2).UseAllAvailableWidth();
                    AddDetailRow(userTable, "Name", $"{booking.User.FirstName} {booking.User.LastName}");
                    AddDetailRow(userTable, "Email", booking.User.Email ?? "N/A");
                    document.Add(userTable);
                });
            }

            // ===== SCAN HISTORY (if scanned) =====
            if (ticket.IsScanned)
            {
                AddSectionCard(document, "📱 Scan History", ColorConstants.ORANGE, () =>
                {
                    var scanTable = new Table(3).UseAllAvailableWidth();
                    AddDetailRow(scanTable, "Scan Count", $"{ticket.ScanCount} times", true);
                    AddDetailRow(scanTable, "First Scan", ticket.FirstScannedAt?.ToString("dd MMM yyyy HH:mm") ?? "N/A", true);
                    AddDetailRow(scanTable, "Last Scan", ticket.LastScannedAt?.ToString("dd MMM yyyy HH:mm") ?? "N/A", true);
                    document.Add(scanTable);
                });
            }

            // ===== VALIDITY INFORMATION =====
            AddSectionCard(document, "⏰ Validity Information", ColorConstants.LIGHT_GRAY, () =>
            {
                var validityTable = new Table(2).UseAllAvailableWidth();
                AddDetailRow(validityTable, "Created At", ticket.CreatedAt.ToString("dd MMM yyyy HH:mm"));
                if (ticket.ExpiresAt.HasValue)
                {
                    AddDetailRow(validityTable, "Expires At", ticket.ExpiresAt.Value.ToString("dd MMM yyyy HH:mm"));
                }
                document.Add(validityTable);
            });

            // ===== VERIFICATION URL =====
            var verifyUrl = $"{BaseUrl}/Ticket/Verify/{ticket.TicketId}";
            document.Add(new Paragraph($"🔗 Verify online: {verifyUrl}")
                .SetFontSize(9)
                .SetFontColor(ColorConstants.BLUE)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginTop(20));

            // ===== FOOTER =====
            document.Add(new Paragraph("Please present this QR code at the venue entrance.")
                .SetFontSize(10)
                .SetItalic()
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFontColor(ColorConstants.DARK_GRAY)
                .SetMarginTop(20));

            document.Add(new Paragraph("Powered by Entertania")
                .SetFontSize(8)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFontColor(ColorConstants.GRAY)
                .SetMarginTop(5));
        }

        private void AddSectionCard(Document document, string title, Color headerColor, Action addContent)
        {
            // Card container
            var cardTable = new Table(1).UseAllAvailableWidth().SetMarginTop(15).SetMarginBottom(10);

            // Header
            var headerCell = new Cell()
                .Add(new Paragraph(title)
                    .SetFontSize(12)
                    .SetBold()
                    .SetFontColor(ColorConstants.WHITE))
                .SetBackgroundColor(headerColor)
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetPadding(10);
            cardTable.AddCell(headerCell);

            document.Add(cardTable);

            // Content
            addContent();
        }

        private void AddDetailRow(Table table, string label, string value, bool threeColumns = false)
        {
            var labelCell = new Cell()
                .Add(new Paragraph(label)
                    .SetFontSize(9)
                    .SetFontColor(ColorConstants.DARK_GRAY))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetPaddingTop(5)
                .SetPaddingBottom(5);

            var valueCell = new Cell()
                .Add(new Paragraph(value)
                    .SetFontSize(10)
                    .SetBold())
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetPaddingTop(5)
                .SetPaddingBottom(5);

            table.AddCell(labelCell);
            table.AddCell(valueCell);
        }

        // ✅ Helper method to get Governorate display name
        private string GetGovernorateDisplayName(Core.Enums.Governorate governorate)
        {
            var memberInfo = governorate.GetType().GetMember(governorate.ToString()).FirstOrDefault();
            var displayAttribute = memberInfo?.GetCustomAttribute<DisplayAttribute>();
            return displayAttribute?.Name ?? governorate.ToString();
        }

        #endregion

        #region Email

        public async Task<bool> SendTicketsViaEmailAsync(int bookingId)
        {
            try
            {
                _logger.LogInformation($"Sending tickets via email for booking {bookingId}");

                var booking = _unitOfWork.Bookings.GetById(bookingId);
                if (booking == null || booking.User == null || booking.Event == null)
                {
                    _logger.LogError($"Booking, user, or event not found for booking {bookingId}");
                    return false;
                }

                var tickets = await GetTicketsByBookingIdAsync(bookingId);
                if (!tickets.Any())
                {
                    _logger.LogWarning($"No tickets found for booking {bookingId}");
                    return false;
                }

                var pdfBytes = await GenerateTicketsPDFAsync(bookingId);

                var emailSent = await _emailService.SendTicketsEmailAsync(
                    booking.User.Email,
                    $"{booking.User.FirstName} {booking.User.LastName}",
                    booking.Event.Title,
                    tickets.Count,
                    pdfBytes
                );

                if (emailSent)
                {
                    _logger.LogInformation($"✅ Tickets sent via email to {booking.User.Email}");
                }
                else
                {
                    _logger.LogError($"❌ Failed to send tickets email to {booking.User.Email}");
                }

                return emailSent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending tickets via email for booking {bookingId}");
                return false;
            }
        }

        #endregion
    }
}