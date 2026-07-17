using Core.Models;

namespace Core.Interfaces.RepositoriesInterface
{
    public interface IReviewRepo
    {
        Task<List<Review>> GetTopReviewsAsync(int count);
        Task<Review?> GetByIdAsync(int id);
        Task AddAsync(Review review);
        Task DeleteAsync(int id);
        Task<int> SaveAsync();
    }
}