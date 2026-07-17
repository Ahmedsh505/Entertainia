using Core.Interfaces.RepositoriesInterface;
using Core.Interfaces.UnitOfWorkInterface;
using Infrastructure.Data.DbContexts;
using Infrastructure.Repositories;

namespace Infrastructure.Data.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly EntertaniaDbcontext _context;
        private readonly IReviewRepo _reviewRepo;


        // Base repositories
        public IEventRepo Events { get; private set; }
        public IUserRepo Users { get; private set; }
        public IVenueRepo Venues { get; private set; }
        public IBookingRepo Bookings { get; private set; }
        public ITicketRepo Tickets { get; private set; }

        // Sub-event repositories
        public IConcertRepo Concerts { get; private set; }
        public ISportEventRepo SportEvents { get; private set; }
        public ITheaterPlayRepo TheaterPlays { get; private set; }
        public ICulturalEventRepo CulturalEvents { get; private set; }
        public ITourismActivityRepo TourismActivities { get; private set; }

        // Enhancement repositories
        public IDiscountCodeRepo DiscountCodes { get; private set; }
        public IEventTicketTierRepo TicketTiers { get; private set; }
        public IPaymentRepo Payments { get; private set; }
        public IRefundRepo Refunds { get; private set; }
        public ITransactionRepo Transactions { get; private set; }
        public IReviewRepo Reviews => _reviewRepo;



        // Photo repositories  ✅ FIXED
        public IEventPhotoRepo EventPhotos { get; private set; }
        public IVenuePhotoRepo VenuePhotos { get; private set; }

        // Video repositories
        public IEventVideoRepo EventVideos { get; private set; }
        public IVenueVideoRepo VenueVideos { get; private set; }

        // Authentication
        public IAuthRepository Auth { get; private set; }

        public UnitOfWork(EntertaniaDbcontext context)
        {
            _context = context;

            // Base repos
            Events = new EventRepository(_context);
            Users = new UserRepository(_context);
            Venues = new VenueRepository(_context);
            Bookings = new BookingRepository(_context);
            Tickets = new TicketRepository(_context);


            // Sub-events
            Concerts = new ConcertRepository(_context);
            SportEvents = new SportEventRepository(_context);
            TheaterPlays = new TheaterPlayRepository(_context);
            CulturalEvents = new CulturalEventRepository(_context);
            TourismActivities = new TourismActivityRepository(_context);

            // Enhancement
            DiscountCodes = new DiscountCodeRepository(_context);
            TicketTiers = new EventTicketTierRepository(_context);
            Payments = new PaymentRepository(_context);
            Refunds = new RefundRepository(_context);
            Transactions = new TransactionRepository(_context);
            _reviewRepo = new ReviewRepository(_context);

            // Photos  ✅ MUST MATCH INTERFACE
            EventPhotos = new EventPhotoRepository(_context);
            VenuePhotos = new VenuePhotoRepository(_context);

            // Videos
            EventVideos = new EventVideoRepository(_context);
            VenueVideos = new VenueVideoRepository(_context);

            // Auth
            Auth = new AuthRepository(_context);
        }

        public int Complete()
        {
            return _context.SaveChanges();
        }

        public Task<int> SaveAsync() => _context.SaveChangesAsync();

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
