using Core.Interfaces.ServicesInterface;
using System;

namespace Service.Services
{
    public class EgyptTimeService : IEgyptTimeService
    {
        private readonly TimeZoneInfo _egyptZone;

        public EgyptTimeService()
        {
            try
            {
                // Try Windows timezone ID first
                _egyptZone = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                try
                {
                    // Fallback to Linux/Mac timezone ID
                    _egyptZone = TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo");
                }
                catch (TimeZoneNotFoundException)
                {
                    // Final fallback: Create custom timezone for UTC+2 (Egypt Standard Time)
                    _egyptZone = TimeZoneInfo.CreateCustomTimeZone(
                        "Egypt Standard Time",
                        TimeSpan.FromHours(2),
                        "Egypt Standard Time",
                        "Egypt Standard Time");
                }
            }
        }

        /// <summary>
        /// Current Egypt local time - Use this instead of DateTime.UtcNow
        /// </summary>
        public DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _egyptZone);

        /// <summary>
        /// Current UTC time (if needed)
        /// </summary>
        public DateTime UtcNow => DateTime.UtcNow;

        /// <summary>
        /// Convert UTC to Egypt time
        /// </summary>
        public DateTime ToEgyptTime(DateTime utcTime)
        {
            // Ensure the DateTime is treated as UTC
            if (utcTime.Kind == DateTimeKind.Unspecified)
                utcTime = DateTime.SpecifyKind(utcTime, DateTimeKind.Utc);
            else if (utcTime.Kind == DateTimeKind.Local)
                utcTime = utcTime.ToUniversalTime();

            return TimeZoneInfo.ConvertTimeFromUtc(utcTime, _egyptZone);
        }

        /// <summary>
        /// Convert Egypt time to UTC for database storage
        /// </summary>
        public DateTime ToUtc(DateTime egyptTime)
        {
            // Treat unspecified DateTime as Egypt local time
            if (egyptTime.Kind == DateTimeKind.Unspecified)
                egyptTime = DateTime.SpecifyKind(egyptTime, DateTimeKind.Unspecified);
            else if (egyptTime.Kind == DateTimeKind.Utc)
                return egyptTime; // Already UTC

            return TimeZoneInfo.ConvertTimeToUtc(egyptTime, _egyptZone);
        }

        /// <summary>
        /// Convert nullable UTC to Egypt time
        /// </summary>
        public DateTime? ToEgyptTime(DateTime? utcTime)
        {
            return utcTime.HasValue ? ToEgyptTime(utcTime.Value) : null;
        }

        /// <summary>
        /// Convert nullable Egypt time to UTC
        /// </summary>
        public DateTime? ToUtc(DateTime? egyptTime)
        {
            return egyptTime.HasValue ? ToUtc(egyptTime.Value) : null;
        }
    }
}