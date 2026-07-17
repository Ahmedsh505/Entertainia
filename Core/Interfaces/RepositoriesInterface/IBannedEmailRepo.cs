using Core.Models.AuthModels;

namespace Core.Interfaces.RepositoriesInterface
{
    public interface IBannedEmailRepo
    {
        /// <summary>
        /// Add a new banned email
        /// </summary>
        void Add(BannedEmail bannedEmail);

        /// <summary>
        /// Check if an email is banned
        /// </summary>
        bool IsEmailBanned(string email);

        /// <summary>
        /// Get banned email record by email
        /// </summary>
        BannedEmail? GetByEmail(string email);

        /// <summary>
        /// Get all banned emails
        /// </summary>
        List<BannedEmail> GetAll();

        /// <summary>
        /// Remove a banned email (unban)
        /// </summary>
        void Remove(BannedEmail bannedEmail);

        /// <summary>
        /// Save changes to database
        /// </summary>
        void Save();
    }
}