using Core.Interfaces.RepositoriesInterface;
using Core.Models.EventModels;
using Infrastructure.Data;
using Infrastructure.Data.DbContexts;
using Microsoft.EntityFrameworkCore;
using System;

namespace Infrastructure.Repositories
{
    public class TourismActivityRepository : ITourismActivityRepo
    {
        private readonly EntertaniaDbcontext _context;

        public TourismActivityRepository(EntertaniaDbcontext context)
        {
            _context = context;
        }

        // إضافة TourismActivity جديد
        public void Add(TourismActivity tourismActivity)
        {
            _context.TourismActivities.Add(tourismActivity);
        }

        // تحديث TourismActivity موجود
        public void Update(TourismActivity tourismActivity)
        {
            var existingEntry = _context.ChangeTracker.Entries<TourismActivity>()
            .FirstOrDefault(e => e.Entity.TourismId == tourismActivity.TourismId);

            if (existingEntry != null)
            {
                existingEntry.CurrentValues.SetValues(tourismActivity);
                existingEntry.State = EntityState.Modified;
            }
            else
            {
                _context.TourismActivities.Update(tourismActivity);
            }
        }

        // حذف TourismActivity
        public void Delete(TourismActivity tourismActivity)
        {
            _context.TourismActivities.Remove(tourismActivity);
        }

        // جلب TourismActivity بالـ ID
        public TourismActivity? GetById(int id)
        {
            // الحل: استخدم AsNoTracking علشان متتتبعش الـ entity
            return _context.TourismActivities
                .AsNoTracking()
                .Include(t => t.EventBase)
                .FirstOrDefault(t => t.TourismId == id);
        }

        // جلب كل الـ TourismActivities
        public List<TourismActivity> GetAll()
        {
            return _context.TourismActivities
                .AsNoTracking()
                .Include(t => t.EventBase)
                .ToList();
        }

        // حفظ التغييرات
        public void Save()
        {
            _context.SaveChanges();
        }
    }
}