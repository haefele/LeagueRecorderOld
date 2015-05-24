using System;

namespace LeagueRecorder.Server.Infrastructure.Extensions
{
    public static class DateTimeExtensions
    {
        public static string ToLeagueTime(this DateTime dateTime)
        {
            return dateTime.ToString("MMM d, yyyy hh:mm:ss tt");
        }
    }
}