using Core.Interfaces.RepositoriesInterface;
using Core.Models.AuthModels;
using Infrastructure.Data.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class BannedEmailRepository : IBannedEmailRepo
    {
        private readonly EntertaniaDbcontext _context;

        public BannedEmailRepository(EntertaniaDbcontext context)
        {
            _context = context;
        }

        public void Add(BannedEmail bannedEmail)
        {
            _context.BannedEmails.Add(bannedEmail);
        }

        public bool IsEmailBanned(string email)
        {
            var normalizedEmail = email.ToLower().Trim();
            return _context.BannedEmails.Any(b => b.Email.ToLower() == normalizedEmail);
        }

        public BannedEmail? GetByEmail(string email)
        {
            var normalizedEmail = email.ToLower().Trim();
            return _context.BannedEmails
                .Include(b => b.BannedBy)
                .FirstOrDefault(b => b.Email.ToLower() == normalizedEmail);
        }

        public List<BannedEmail> GetAll()
        {
            return _context.BannedEmails
                .Include(b => b.BannedBy)
                .OrderByDescending(b => b.BannedAt)
                .ToList();
        }

        public void Remove(BannedEmail bannedEmail)
        {
            _context.BannedEmails.Remove(bannedEmail);
        }

        public void Save()
        {
            _context.SaveChanges();
        }
    }
}