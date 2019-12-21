using System;
using Cinema.Core.Abstractions;
using Cinema.Core.Domain;

namespace Cinema.Core.Services
{
    public interface IPriceCalculator
    {
        decimal SeatPrice(Screening screening, SeatType seatType);

        decimal ServiceFee(int seatCount);

        bool IsMatinee(Screening screening);
    }

    /// <summary>
    /// Ticket price = (base price x seat multiplier + format surcharge) x (1 - matinee discount),
    /// rounded to pennies. Accessible spaces are always charged at the standard rate.
    /// </summary>
    public class PriceCalculator : IPriceCalculator
    {
        private readonly CinemaOptions _options;

        public PriceCalculator(CinemaOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            _options = options;
        }

        public decimal SeatPrice(Screening screening, SeatType seatType)
        {
            if (screening == null)
            {
                throw new ArgumentNullException("screening");
            }

            var price = screening.BasePrice * Multiplier(seatType) + Surcharge(screening.Format);

            if (IsMatinee(screening))
            {
                price *= 1 - _options.MatineeDiscount;
            }

            return Round(price);
        }

        public decimal ServiceFee(int seatCount)
        {
            return seatCount <= 0 ? 0m : Round(_options.ServiceFeePerSeat * seatCount);
        }

        public bool IsMatinee(Screening screening)
        {
            var local = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.SpecifyKind(screening.StartsAtUtc, DateTimeKind.Utc), _options.TimeZone);

            var weekday = local.DayOfWeek != DayOfWeek.Saturday && local.DayOfWeek != DayOfWeek.Sunday;
            return weekday && local.Hour < _options.MatineeEndsAtLocalHour;
        }

        private decimal Multiplier(SeatType type)
        {
            return type == SeatType.Premium ? _options.PremiumMultiplier : 1m;
        }

        private decimal Surcharge(ScreeningFormat format)
        {
            switch (format)
            {
                case ScreeningFormat.ThreeD:
                    return _options.ThreeDSurcharge;
                case ScreeningFormat.Imax:
                    return _options.ImaxSurcharge;
                default:
                    return 0m;
            }
        }

        private static decimal Round(decimal value)
        {
            return Math.Round(value, 2, MidpointRounding.AwayFromZero);
        }
    }
}
