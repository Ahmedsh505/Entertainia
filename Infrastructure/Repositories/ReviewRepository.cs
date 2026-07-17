using Core.Interfaces.RepositoriesInterface;
using Core.Models;
using Infrastructure.Data.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class ReviewRepository : IReviewRepo
    {
        private readonly EntertaniaDbcontext _context;

        public ReviewRepository(EntertaniaDbcontext context)
        {
            _context = context;
        }

        public async Task<List<Review>> GetTopReviewsAsync(int count)
        {
            return await _context.Reviews
                .Where(r => !r.IsDeleted)
                .Include(r => r.User)
                .OrderByDescending(r => r.Rating)
                .ThenByDescending(r => r.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<Review?> GetByIdAsync(int id)
        {
            return await _context.Reviews
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.ReviewId == id && !r.IsDeleted);
        }

        public async Task AddAsync(Review review)
        {
            await _context.Reviews.AddAsync(review);
        }

        public async Task DeleteAsync(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review != null)
            {
                review.IsDeleted = true;
            }
        }

        public async Task<int> SaveAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}