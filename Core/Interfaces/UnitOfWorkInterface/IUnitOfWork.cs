using Core.Interfaces.RepositoriesInterface;

namespace Core.Interfaces.UnitOfWorkInterface
{
    public interface IUnitOfWork : IDisposable
    {
        // Base repositories
        IEventRepo Events { get; }
        IUserRepo Users { get; }
        IVenueRepo Venues { get; }
        IBookingRepo Bookings { get; }
        ITicketRepo Tickets { get; }

        // Sub-event repositories
        IConcertRepo Concerts { get; }
        ISportEventRepo SportEvents { get; }
        ITheaterPlayRepo TheaterPlays { get; }
        ICulturalEventRepo CulturalEvents { get; }
        ITourismActivityRepo TourismActivities { get; }

        // Enhancement repositories
        IDiscountCodeRepo DiscountCodes { get; }
        IEventTicketTierRepo TicketTiers { get; }
        IPaymentRepo Payments { get; }
        IRefundRepo Refunds { get; }
        ITransactionRepo Transactions { get; }
        IReviewRepo Reviews { get; }

        // Photo repositories
        IEventPhotoRepo EventPhotos { get; }
        IVenuePhotoRepo VenuePhotos { get; }

        // validation repositories
        IEventVideoRepo EventVideos { get; }
        IVenueVideoRepo VenueVideos { get; }

        // Authentication repositories (if needed)
        IAuthRepository Auth { get; }

        // Commit changes
        int Complete();
        Task<int> SaveAsync();

    }
}