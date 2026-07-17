using Core.Interfaces.RepositoriesInterface;
using Core.Models.EventModels;
using Infrastructure.Data;
using Infrastructure.Data.DbContexts;
using Microsoft.EntityFrameworkCore;
using System;

namespace Infrastructure.Repositories
{
    public class TheaterPlayRepository : ITheaterPlayRepo
    {
        private readonly EntertaniaDbcontext _context;

        public TheaterPlayRepository(EntertaniaDbcontext context)
        {
            _context = context;
        }

        // إضافة TheaterPlay جديد
        public void Add(TheaterPlay theaterPlay)
        {
            _context.TheaterPlays.Add(theaterPlay);
        }

        // تحديث TheaterPlay موجود
        public void Update(TheaterPlay theaterPlay)
        {
            var existingEntry = _context.ChangeTracker.Entries<TheaterPlay>()
            .FirstOrDefault(e => e.Entity.TheaterId == theaterPlay.TheaterId);

            if (existingEntry != null)
            {
                existingEntry.CurrentValues.SetValues(theaterPlay);
                existingEntry.State = EntityState.Modified;
            }
            else
            {
                _context.TheaterPlays.Update(theaterPlay);
            }
        }

        // حذف TheaterPlay
        public void Delete(TheaterPlay theaterPlay)
        {
            _context.TheaterPlays.Remove(theaterPlay);
        }

        // جلب TheaterPlay بالـ ID
        public TheaterPlay? GetById(int id)
        {
            // الحل: استخدم AsNoTracking علشان متتتبعش الـ entity
            return _context.TheaterPlays
                .AsNoTracking()
                .Include(t => t.EventBase)
                .FirstOrDefault(t => t.TheaterId == id);
        }

        // جلب كل الـ TheaterPlays
        public List<TheaterPlay> GetAll()
        {
            return _context.TheaterPlays
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