using Core.Enums;
using Core.Models.PaymentModels;


namespace Core.Interfaces.RepositoriesInterface
{
    public interface ITransactionRepo
    {
        void Add(Transaction transaction);
        void Update(Transaction transaction);
        void Delete(Transaction transaction);

        Transaction? GetById(int id);
        Transaction? GetByReference(string transactionReference);
        Transaction? GetByGatewayTransactionId(string gatewayTransactionId);

        List<Transaction> GetAll();
        List<Transaction> GetByUser(int userId);
        List<Transaction> GetByBooking(int bookingId);
        List<Transaction> GetByPayment(int paymentId);
        List<Transaction> GetByRefund(int refundId);
        List<Transaction> GetByStatus(PaymentStatus status);
        List<Transaction> GetByType(string transactionType);
        List<Transaction> GetByDateRange(DateTime startDate, DateTime endDate);

        void Save();
    }
}
