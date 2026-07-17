using Core.Enums;
using Core.Models.PaymentModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces.RepositoriesInterface
{
    public interface IRefundRepo
    {
        void Add(Refund refund);
        void Update(Refund refund);
        void Delete(Refund refund);
        List<Refund> GetAll();
        Refund? GetById(int id);
        Refund? GetByBookingId(int bookingId);
        Refund? GetByPaymentId(int paymentId);
        List<Refund> GetByStatus(PaymentStatus status);
        List<Refund> GetPendingRefunds();
        void UpdateStatus(int refundId, PaymentStatus status);
        void Save();
    }
}
