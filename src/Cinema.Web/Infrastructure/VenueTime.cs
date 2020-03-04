using System;
using System.Web.Mvc;
using Cinema.Core.Abstractions;

namespace Cinema.Web.Infrastructure
{
    /// <summary>Everything is stored in UTC; views render in the venue's time zone.</summary>
    public static class VenueTime
    {
        public static CinemaOptions Options
        {
            get { return DependencyResolver.Current.GetService<CinemaOptions>(); }
        }

        public static DateTime ToLocal(DateTime utc)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Options.TimeZone);
        }

        public static DateTime ToUtc(DateTime local)
        {
            return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), Options.TimeZone);
        }

        public static DateTime Today
        {
            get { return ToLocal(DateTime.UtcNow).Date; }
        }

        public static MvcHtmlString LocalTime(this HtmlHelper html, DateTime utc, string format = "ddd d MMM, HH:mm")
        {
            var local = ToLocal(utc);
            var iso = DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToString("o");
            return MvcHtmlString.Create(string.Format(
                "<time datetime=\"{0}\">{1}</time>", iso, HttpUtilityEncode(local.ToString(format))));
        }

        public static string Money(this HtmlHelper html, decimal amount)
        {
            return amount.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("en-GB"));
        }

        private static string HttpUtilityEncode(string value)
        {
            return System.Web.HttpUtility.HtmlEncode(value);
        }
    }
}
