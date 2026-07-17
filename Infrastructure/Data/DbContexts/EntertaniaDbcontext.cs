using Core.Models;
using Core.Models.AuthModels;
using Core.Models.BookingModels;
using Core.Models.EventModels;
using Core.Models.PaymentModels;
using Core.Models.PhotoModels;
using Core.Models.VideoModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Core.Models.TicketModels;

namespace Infrastructure.Data.DbContexts
{
    public class EntertaniaDbcontext : IdentityDbContext<User, IdentityRole<int>, int>
    {
        public EntertaniaDbcontext(DbContextOptions<EntertaniaDbcontext> options)
            : base(options)
        {
        }

        //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        //{
        //    if (!optionsBuilder.IsConfigured)
        //    {
        //        optionsBuilder.UseSqlServer(
        //            "Data Source=.;Initial Catalog=EntertaniaDb;Integrated Security=True;Encrypt=False;TrustServerCertificate=True");
        //    }
        //}

        // --- Application entities ---
        public DbSet<User> Users { get; set; }
        public DbSet<Venue> Venues { get; set; }
        public DbSet<EventBase> Events { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Ticket> Tickets { get; set; }

        // Derived event types (TPT)
        public DbSet<CulturalEvent> CulturalEvents { get; set; }
        public DbSet<TheaterPlay> TheaterPlays { get; set; }
        public DbSet<SportEvent> SportEvents { get; set; }
        public DbSet<Concert> Concerts { get; set; }
        public DbSet<TourismActivity> TourismActivities { get; set; }

        // Photo gallery entities
        public DbSet<EventPhoto> EventPhotos { get; set; }
        public DbSet<VenuePhoto> VenuePhotos { get; set; }

        // Video gallery entities
        public DbSet<EventVideo> EventVideos { get; set; }
        public DbSet<VenueVideo> VenueVideos { get; set; }

        // Organizer verification documents
        public DbSet<VerificationDoc> VerificationDocs { get; set; }

        // Enhancement entities
        public DbSet<DiscountCode> DiscountCodes { get; set; }
        public DbSet<EventTicketTier> EventTicketTiers { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Refund> Refunds { get; set; }

        // ✅ KEEP PaymentHistory for backward compatibility
        public DbSet<PaymentHistory> PaymentHistories { get; set; }

        // ✅ NEW: Transaction table for complete audit trail
        public DbSet<Transaction> Transactions { get; set; }

        // Authentication tokens
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<EmailConfirmationToken> EmailConfirmationTokens { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
        public DbSet<TwoFactorCode> TwoFactorCodes { get; set; }

        // Banned emails
        public DbSet<BannedEmail> BannedEmails { get; set; }
        // Reviews
        public DbSet<Review> Reviews { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply all entity configurations in the assembly
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(EntertaniaDbcontext).Assembly);

            // Global Query Filters
            modelBuilder.Entity<User>().HasQueryFilter(u => !u.IsDeleted || u.OrganizedEvents.Any());
            modelBuilder.Entity<Venue>().HasQueryFilter(v => !v.IsDeleted);

            // Table-Per-Type mapping for event hierarchy
            modelBuilder.Entity<CulturalEvent>().ToTable("CulturalEvents");
            modelBuilder.Entity<TheaterPlay>().ToTable("TheaterPlays");
            modelBuilder.Entity<SportEvent>().ToTable("SportEvents");
            modelBuilder.Entity<Concert>().ToTable("Concerts");
            modelBuilder.Entity<TourismActivity>().ToTable("TourismActivities");

            // Custom Identity table names
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<IdentityRole<int>>().ToTable("Roles");
            modelBuilder.Entity<IdentityUserRole<int>>().ToTable("UserRoles");
            modelBuilder.Entity<IdentityUserClaim<int>>().ToTable("UserClaims");
            modelBuilder.Entity<IdentityRoleClaim<int>>().ToTable("RoleClaims");
            modelBuilder.Entity<IdentityUserLogin<int>>().ToTable("UserLogins");
            modelBuilder.Entity<IdentityUserToken<int>>().ToTable("UserTokens");

            // Auth token table names
            modelBuilder.Entity<RefreshToken>().ToTable("RefreshTokens");
            modelBuilder.Entity<EmailConfirmationToken>().ToTable("EmailConfirmationTokens");
            modelBuilder.Entity<PasswordResetToken>().ToTable("PasswordResetTokens");
            modelBuilder.Entity<TwoFactorCode>().ToTable("TwoFactorCodes");

            // Banned emails table
            modelBuilder.Entity<BannedEmail>().ToTable("BannedEmails");

            // ✅ NEW: Transaction table
            modelBuilder.Entity<Transaction>().ToTable("Transactions");

            // Ticket table
            modelBuilder.Entity<Ticket>().ToTable("Tickets");

            // Reviews
            modelBuilder.Entity<Review>().ToTable("Reviews");
            modelBuilder.Entity<Review>().HasQueryFilter(r => !r.IsDeleted);
        }
    }
}