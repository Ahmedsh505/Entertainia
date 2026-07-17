using Core.Enums;
using Core.Models.PaymentModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces.RepositoriesInterface
{
    public interface IPaymentRepo
    {
        void Add(Payment payment);
        void Update(Payment payment);
        void Delete(Payment payment);
        List<Payment> GetAll();
        Payment? GetById(int id);
        Payment? GetByBookingId(int bookingId);
        Payment? GetByTransactionId(string transactionId);
        List<Payment> GetByStatus(PaymentStatus status);
        List<Payment> GetByUser(int userId);
        void UpdateStatus(int paymentId, PaymentStatus status);

        // ✅ Dashboard Helper Methods
        decimal GetTotalRevenueForOrganizer(int organizerId);
        decimal GetMonthlyRevenueForOrganizer(int organizerId, int year, int month);

        void Save();
    }
}
