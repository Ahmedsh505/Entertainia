using Core.Models.TicketModels;

namespace Core.Interfaces.ServicesInterface
{
    public interface ITicketService
    {
        // ===== TICKET GENERATION =====
        /// <summary>
        /// Generate tickets for a booking after successful payment
        /// </summary>
        Task<(bool Success, string Message, List<Ticket>? Tickets)> GenerateTicketsForBookingAsync(int bookingId);

        // ===== TICKET RETRIEVAL =====
        Task<Ticket?> GetTicketByIdAsync(int ticketId);
        Task<Ticket?> GetTicketByHashAsync(string verificationHash);
        Task<List<Ticket>> GetTicketsByBookingIdAsync(int bookingId);
        Task<List<Ticket>> GetTicketsByEventIdAsync(int eventId);
        Task<List<Ticket>> GetTicketsByUserIdAsync(int userId);

        // ===== TICKET VERIFICATION =====
        /// <summary>
        /// Verify ticket using QR code data
        /// Format: TicketId|BookingId|EventId|Hash
        /// </summary>
        Task<(bool IsValid, string Message, Ticket? Ticket)> VerifyTicketAsync(string qrCodeData);

        // ===== TICKET SCANNING =====
        Task<(bool Success, string Message)> ScanTicketAsync(int ticketId, int scannedByUserId);

        // ===== QR CODE GENERATION =====
        /// <summary>
        /// Generate QR code image as base64 string
        /// </summary>
        string GenerateQRCodeBase64(string qrCodeData);

        /// <summary>
        /// Generate QR code image as byte array
        /// </summary>
        byte[] GenerateQRCodeBytes(string qrCodeData);

        // ===== TICKET STATUS =====
        Task<(bool Success, string Message)> CancelTicketAsync(int ticketId);
        Task<(bool Success, string Message)> CancelTicketsForBookingAsync(int bookingId);

        // ===== PDF GENERATION =====
        /// <summary>
        /// Generate PDF with all tickets for a booking
        /// </summary>
        Task<byte[]> GenerateTicketsPDFAsync(int bookingId);

        // ===== EMAIL =====
        /// <summary>
        /// Send tickets via email with PDF attachment
        /// </summary>
        Task<bool> SendTicketsViaEmailAsync(int bookingId);
    }
}