using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Globalization;
using Cinema.Core.Abstractions;

namespace Cinema.Web.Infrastructure
{
    /// <summary>Binds <see cref="CinemaOptions"/> from <c>Cinema:*</c> appSettings, falling back to defaults.</summary>
    public static class CinemaOptionsFactory
    {
        public static CinemaOptions FromAppSettings()
        {
            return From(ConfigurationManager.AppSettings);
        }

        public static CinemaOptions From(NameValueCollection settings)
        {
            var options = new CinemaOptions();

            var tz = settings["Cinema:TimeZoneId"];
            if (!string.IsNullOrWhiteSpace(tz))
            {
                options.TimeZone = TimeZoneInfo.FindSystemTimeZoneById(tz);
            }

            options.HoldDuration = Minutes(settings, "Cinema:HoldMinutes", options.HoldDuration);
            options.CleaningBuffer = Minutes(settings, "Cinema:CleaningBufferMinutes", options.CleaningBuffer);
            options.CancellationCutoff = Minutes(settings, "Cinema:CancellationCutoffMinutes", options.CancellationCutoff);

            int max;
            if (int.TryParse(settings["Cinema:MaxSeatsPerBooking"], out max) && max > 0)
            {
                options.MaxSeatsPerBooking = max;
            }

            bool gap;
            if (bool.TryParse(settings["Cinema:EnforceNoSingleSeatGap"], out gap))
            {
                options.EnforceNoSingleSeatGap = gap;
            }

            decimal fee;
            if (decimal.TryParse(settings["Cinema:ServiceFeePerSeat"], NumberStyles.Number, CultureInfo.InvariantCulture, out fee) && fee >= 0)
            {
                options.ServiceFeePerSeat = fee;
            }

            var key = settings["Cinema:TicketSigningKey"];
            if (!string.IsNullOrWhiteSpace(key))
            {
                options.TicketSigningKey = key;
            }

            return options;
        }

        private static TimeSpan Minutes(NameValueCollection settings, string key, TimeSpan fallback)
        {
            int minutes;
            return int.TryParse(settings[key], out minutes) && minutes >= 0 ? TimeSpan.FromMinutes(minutes) : fallback;
        }
    }
}
