using Core.Interfaces.RepositoriesInterface;
using Core.Models.EventModels;
using Infrastructure.Data;
using Infrastructure.Data.DbContexts;
using Microsoft.EntityFrameworkCore;
using System;

namespace Infrastructure.Repositories
{
    public class SportEventRepository : ISportEventRepo
    {
        private readonly EntertaniaDbcontext _context;

        public SportEventRepository(EntertaniaDbcontext context)
        {
            _context = context;
        }

        public void Add(SportEvent sportEvent)
        {
            _context.SportEvents.Add(sportEvent);
        }

        public void Update(SportEvent sportEvent)
        {
            var existingEntry = _context.ChangeTracker.Entries<SportEvent>()
        .FirstOrDefault(e => e.Entity.SportId == sportEvent.SportId);

            if (existingEntry != null)
            {
                existingEntry.CurrentValues.SetValues(sportEvent);
                existingEntry.State = EntityState.Modified;
            }
            else
            {
                _context.SportEvents.Update(sportEvent);
            }
        }

        public void Delete(SportEvent sportEvent)
        {
            _context.SportEvents.Remove(sportEvent);
        }

        public SportEvent? GetById(int id)
        {
            return _context.SportEvents
                .AsNoTracking()
                .Include(s => s.EventBase)
                .FirstOrDefault(s => s.SportId == id);
        }

        public List<SportEvent> GetAll()
        {
            return _context.SportEvents
                .AsNoTracking()
                .Include(s => s.EventBase)
                .ToList();
        }

        public void Save()
        {
            _context.SaveChanges();
        }
    }
}