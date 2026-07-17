namespace Core.Enums
{
    /// <summary>
    /// Specifies how a user account was deleted
    /// </summary>
    public enum DeletionType
    {
        /// <summary>
        /// Not deleted (active user)
        /// </summary>
        None = 0,

        /// <summary>
        /// User deleted their own account (can re-register)
        /// </summary>
        SelfDeleted = 1,

        /// <summary>
        /// Admin permanently banned the user (email blacklisted)
        /// </summary>
        AdminBanned = 2,

        /// <summary>
        /// Admin deleted normally (can be restored by admin)
        /// </summary>
        AdminDeleted = 3
    }
}