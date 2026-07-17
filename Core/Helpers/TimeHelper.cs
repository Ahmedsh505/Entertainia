using System;

namespace Core.Helpers
{
    public static class TimeHelper
    {
        private static readonly TimeZoneInfo EgyptZone =
            TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");

        public static DateTime NowEgypt =>
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, EgyptZone);

        public static DateTime ToEgyptTime(DateTime utcTime) =>
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcTime, DateTimeKind.Utc), EgyptZone);

        public static DateTime ToUtcFromEgypt(DateTime egyptTime) =>
            TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(egyptTime, DateTimeKind.Unspecified), EgyptZone);

        public static DateTime? ToUtcFromEgypt(DateTime? egyptTime) =>
            egyptTime.HasValue ? ToUtcFromEgypt(egyptTime.Value) : null;

        public static DateTime? ToEgyptTime(DateTime? utcTime) =>
            utcTime.HasValue ? ToEgyptTime(utcTime.Value) : null;
    }
}
