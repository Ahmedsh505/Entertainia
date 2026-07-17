using Core.Interfaces.RepositoriesInterface;
using Core.Models.EventModels;
using Infrastructure.Data;
using Infrastructure.Data.DbContexts;
using Microsoft.EntityFrameworkCore;
using System;

namespace Infrastructure.Repositories
{
    public class CulturalEventRepository : ICulturalEventRepo
    {
        private readonly EntertaniaDbcontext _context;

        public CulturalEventRepository(EntertaniaDbcontext context)
        {
            _context = context;
        }

        // إضافة CulturalEvent جديد
        public void Add(CulturalEvent culturalEvent)
        {
            _context.CulturalEvents.Add(culturalEvent);
        }

        // تحديث CulturalEvent موجود
        public void Update(CulturalEvent culturalEvent)
        {

            var existingEntry = _context.ChangeTracker.Entries<CulturalEvent>()
            .FirstOrDefault(e => e.Entity.CulturalId == culturalEvent.CulturalId);

            if (existingEntry != null)
            {
                existingEntry.CurrentValues.SetValues(culturalEvent);
                existingEntry.State = EntityState.Modified;
            }
            else
            {
                _context.CulturalEvents.Update(culturalEvent);
            }

        }

        // حذف CulturalEvent
        public void Delete(CulturalEvent culturalEvent)
        {
            _context.CulturalEvents.Remove(culturalEvent);
        }

        // جلب CulturalEvent بالـ ID
        public CulturalEvent? GetById(int id)
        {
            // الحل: استخدم AsNoTracking علشان متتتبعش الـ entity
            return _context.CulturalEvents
                .AsNoTracking()
                .Include(c => c.EventBase)
                .FirstOrDefault(c => c.CulturalId == id);
        }

        // جلب كل الـ CulturalEvents
        public List<CulturalEvent> GetAll()
        {
            return _context.CulturalEvents
                .AsNoTracking()
                .Include(c => c.EventBase)
                .ToList();
        }

        // حفظ التغييرات
        public void Save()
        {
            _context.SaveChanges();
        }
    }
}