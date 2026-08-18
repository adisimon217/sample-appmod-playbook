using System;

namespace MerchantHub.Common.Extensions
{
    public static class DateTimeExtensions
    {
        /// <summary>
        /// Get the start of the month for a given date.
        /// </summary>
        public static DateTime StartOfMonth(this DateTime date)
        {
            return new DateTime(date.Year, date.Month, 1);
        }

        /// <summary>
        /// Get the end of the month for a given date.
        /// </summary>
        public static DateTime EndOfMonth(this DateTime date)
        {
            return new DateTime(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month), 23, 59, 59);
        }

        /// <summary>
        /// Get the start of the week (Monday) for a given date.
        /// </summary>
        public static DateTime StartOfWeek(this DateTime date)
        {
            int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.AddDays(-diff).Date;
        }

        /// <summary>
        /// Get Eastern Time from UTC.
        /// NOTE: All MerchantHub reports use Eastern Time zone.
        /// </summary>
        public static DateTime ToEasternTime(this DateTime utcDate)
        {
            var etz = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(utcDate, etz);
        }

        /// <summary>
        /// Format date for display in the merchant portal.
        /// </summary>
        public static string ToMerchantHubDisplay(this DateTime date)
        {
            return date.ToString("MM/dd/yyyy hh:mm tt");
        }

        /// <summary>
        /// Format date for API responses (ISO 8601).
        /// </summary>
        public static string ToApiFormat(this DateTime date)
        {
            return date.ToString("yyyy-MM-ddTHH:mm:ssZ");
        }

        /// <summary>
        /// Check if a date is a business day (Mon-Fri, excluding major US holidays).
        /// Used for settlement date calculations.
        /// </summary>
        public static bool IsBusinessDay(this DateTime date)
        {
            if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                return false;

            // Check major US holidays (simplified)
            // New Year's Day
            if (date.Month == 1 && date.Day == 1) return false;
            // Memorial Day (last Monday in May)
            if (date.Month == 5 && date.DayOfWeek == DayOfWeek.Monday && date.Day > 24) return false;
            // Independence Day
            if (date.Month == 7 && date.Day == 4) return false;
            // Labor Day (first Monday in September)
            if (date.Month == 9 && date.DayOfWeek == DayOfWeek.Monday && date.Day <= 7) return false;
            // Thanksgiving (fourth Thursday in November)
            if (date.Month == 11 && date.DayOfWeek == DayOfWeek.Thursday && date.Day >= 22 && date.Day <= 28) return false;
            // Christmas
            if (date.Month == 12 && date.Day == 25) return false;

            return true;
        }

        /// <summary>
        /// Get the next business day.
        /// </summary>
        public static DateTime NextBusinessDay(this DateTime date)
        {
            var next = date.AddDays(1);
            while (!next.IsBusinessDay())
            {
                next = next.AddDays(1);
            }
            return next;
        }

        /// <summary>
        /// Get a friendly "time ago" string.
        /// </summary>
        public static string TimeAgo(this DateTime date)
        {
            var span = DateTime.Now - date;

            if (span.TotalMinutes < 1) return "just now";
            if (span.TotalMinutes < 60) return string.Format("{0} min ago", (int)span.TotalMinutes);
            if (span.TotalHours < 24) return string.Format("{0} hours ago", (int)span.TotalHours);
            if (span.TotalDays < 7) return string.Format("{0} days ago", (int)span.TotalDays);
            if (span.TotalDays < 30) return string.Format("{0} weeks ago", (int)(span.TotalDays / 7));
            if (span.TotalDays < 365) return string.Format("{0} months ago", (int)(span.TotalDays / 30));
            return string.Format("{0} years ago", (int)(span.TotalDays / 365));
        }
    }
}
