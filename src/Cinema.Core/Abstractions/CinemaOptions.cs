using System;

namespace Cinema.Core.Abstractions
{
    /// <summary>Business settings for the venue. Bound from Web.config appSettings in the web app.</summary>
    public class CinemaOptions
    {
        public CinemaOptions()
        {
            HoldDuration = TimeSpan.FromMinutes(8);
            MaxSeatsPerBooking = 10;
            EnforceNoSingleSeatGap = true;
            CleaningBuffer = TimeSpan.FromMinutes(20);
            CancellationCutoff = TimeSpan.FromHours(1);
            ServiceFeePerSeat = 0.75m;
            PremiumMultiplier = 1.35m;
            ThreeDSurcharge = 2.00m;
            ImaxSurcharge = 4.50m;
            MatineeDiscount = 0.20m;
            MatineeEndsAtLocalHour = 17;
            TimeZone = TimeZoneInfo.Utc;
            TicketSigningKey = "change-me-in-web-config";
        }

        public TimeSpan HoldDuration { get; set; }

        public int MaxSeatsPerBooking { get; set; }

        /// <summary>Reject selections that strand a single empty seat between occupied seats.</summary>
        public bool EnforceNoSingleSeatGap { get; set; }

        /// <summary>Turnaround between screenings in the same hall.</summary>
        public TimeSpan CleaningBuffer { get; set; }

        /// <summary>Bookings can be cancelled until this long before the screening starts.</summary>
        public TimeSpan CancellationCutoff { get; set; }

        public decimal ServiceFeePerSeat { get; set; }

        public decimal PremiumMultiplier { get; set; }

        public decimal ThreeDSurcharge { get; set; }

        public decimal ImaxSurcharge { get; set; }

        /// <summary>Fractional discount for weekday screenings starting before <see cref="MatineeEndsAtLocalHour"/>.</summary>
        public decimal MatineeDiscount { get; set; }

        public int MatineeEndsAtLocalHour { get; set; }

        /// <summary>Venue time zone, used for matinee rules and display.</summary>
        public TimeZoneInfo TimeZone { get; set; }

        public string TicketSigningKey { get; set; }
    }
}
