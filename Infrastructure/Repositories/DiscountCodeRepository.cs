using Core.Interfaces.RepositoriesInterface;
using Core.Models;
using Infrastructure.Data.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class DiscountCodeRepository : IDiscountCodeRepo
    {
        private readonly EntertaniaDbcontext _context;

        public DiscountCodeRepository(EntertaniaDbcontext context)
        {
            _context = context;
        }

        public void Add(DiscountCode discountCode)
        {
            _context.Set<DiscountCode>().Add(discountCode);
        }

        public async Task AddAsync(DiscountCode discountCode) // ✅ New
        {
            await _context.Set<DiscountCode>().AddAsync(discountCode);
        }

        public void Update(DiscountCode discountCode)
        {
            _context.Set<DiscountCode>().Update(discountCode);
        }

        public void Delete(DiscountCode discountCode)
        {
            _context.Set<DiscountCode>().Remove(discountCode);
        }

        public List<DiscountCode> GetAll()
        {
            return _context.Set<DiscountCode>()
                .Include(d => d.Event)
                .Include(d => d.CreatedBy)
                .ToList();
        }

        public DiscountCode? GetById(int id)
        {
            return _context.Set<DiscountCode>()
                .Include(d => d.Event)
                .Include(d => d.CreatedBy)
                .FirstOrDefault(d => d.DiscountCodeId == id);
        }

        public DiscountCode? GetByCode(string code)
        {
            return _context.Set<DiscountCode>()
                .Include(d => d.Event)
                .FirstOrDefault(d => d.Code.ToLower() == code.ToLower());
        }

        public List<DiscountCode> GetByEvent(int eventId)
        {
            return _context.Set<DiscountCode>()
                .Include(d => d.Event)
                .Where(d => d.EventId == eventId)
                .ToList();
        }

        public DiscountCode? GetValidCodeForEvent(string code, int eventId)
        {
            var now = DateTime.Now;
            return _context.Set<DiscountCode>()
                .Include(d => d.Event)
                .FirstOrDefault(d =>
                    d.Code.ToLower() == code.ToLower() &&
                    d.EventId == eventId &&
                    d.IsActive &&
                    (!d.ValidFrom.HasValue || d.ValidFrom.Value <= now) &&
                    (!d.ValidUntil.HasValue || d.ValidUntil.Value >= now) &&
                    (!d.MaxUsageCount.HasValue || d.CurrentUsageCount < d.MaxUsageCount.Value));
        }

        public void IncrementUsageCount(int discountCodeId)
        {
            var discount = _context.Set<DiscountCode>().Find(discountCodeId);
            if (discount != null)
            {
                discount.CurrentUsageCount++;
                _context.Set<DiscountCode>().Update(discount);
            }
        }

        public void Save()
        {
            _context.SaveChanges();
        }
    }
}