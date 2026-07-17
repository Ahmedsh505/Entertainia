using System;

namespace Core.Interfaces.ServicesInterface
{
    public interface IEgyptTimeService
    {
        /// <summary>
        /// Gets current Egypt time (replaces DateTime.UtcNow throughout the app)
        /// </summary>
        DateTime Now { get; }

        /// <summary>
        /// Gets current UTC time (if you still need it somewhere)
        /// </summary>
        DateTime UtcNow { get; }

        /// <summary>
        /// Converts UTC time to Egypt time
        /// </summary>
        DateTime ToEgyptTime(DateTime utcTime);

        /// <summary>
        /// Converts Egypt time to UTC for database storage
        /// </summary>
        DateTime ToUtc(DateTime egyptTime);

        /// <summary>
        /// Converts nullable UTC time to Egypt time
        /// </summary>
        DateTime? ToEgyptTime(DateTime? utcTime);

        /// <summary>
        /// Converts nullable Egypt time to UTC
        /// </summary>
        DateTime? ToUtc(DateTime? egyptTime);
    }
}