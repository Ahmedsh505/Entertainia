using Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces.RepositoriesInterface
{
    public interface IDiscountCodeRepo
    {
        void Add(DiscountCode discountCode);
        Task AddAsync(DiscountCode discountCode);

        void Update(DiscountCode discountCode);
        void Delete(DiscountCode discountCode);
        List<DiscountCode> GetAll();
        DiscountCode? GetById(int id);
        DiscountCode? GetByCode(string code);
        List<DiscountCode> GetByEvent(int eventId);
        DiscountCode? GetValidCodeForEvent(string code, int eventId);
        void IncrementUsageCount(int discountCodeId);
        void Save();
    }
}