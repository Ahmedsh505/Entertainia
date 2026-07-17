using Core.Interfaces.RepositoriesInterface;
using Core.Models.EventModels;
using Infrastructure.Data;
using Infrastructure.Data.DbContexts;
using Microsoft.EntityFrameworkCore;
using System;

namespace Infrastructure.Repositories
{
    public class ConcertRepository : IConcertRepo
    {
        private readonly EntertaniaDbcontext _context;

        public ConcertRepository(EntertaniaDbcontext context)
        {
            _context = context;
        }

        public void Add(Concert concert)
        {
            _context.Concerts.Add(concert);
        }

        public void Update(Concert concert)
        {
            var existingEntry = _context.ChangeTracker.Entries<Concert>()
                    .FirstOrDefault(e => e.Entity.ConcertId == concert.ConcertId);

            if (existingEntry != null)
            {
                existingEntry.CurrentValues.SetValues(concert);
                existingEntry.State = EntityState.Modified;
            }
            else
            {
                _context.Concerts.Update(concert);
            }
        }

        public void Delete(Concert concert)
        {
            _context.Concerts.Remove(concert);
        }

        public Concert? GetById(int id)
        {
            return _context.Concerts
                .AsNoTracking()
                .Include(c => c.EventBase)
                .FirstOrDefault(c => c.ConcertId == id);
        }

        public List<Concert> GetAll()
        {
            return _context.Concerts
                .AsNoTracking()
                .Include(c => c.EventBase)
                .ToList();
        }

        public void Save()
        {
            _context.SaveChanges();
        }
    }
}