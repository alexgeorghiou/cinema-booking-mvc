using System;
using Cinema.Core.Abstractions;
using Cinema.Core.Domain;
using Cinema.Core.Services;
using Xunit;

namespace Cinema.Core.Tests
{
    public class PriceCalculatorTests
    {
        // 2026-03-06 is a Friday, 2026-03-07 a Saturday.
        private static readonly DateTime FridayEvening = new DateTime(2026, 3, 6, 19, 30, 0, DateTimeKind.Utc);
        private static readonly DateTime FridayAfternoon = new DateTime(2026, 3, 6, 14, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime SaturdayAfternoon = new DateTime(2026, 3, 7, 14, 0, 0, DateTimeKind.Utc);

        private readonly CinemaOptions _options = new CinemaOptions();

        private PriceCalculator Calculator()
        {
            return new PriceCalculator(_options);
        }

        private static Screening Screening(DateTime start, ScreeningFormat format = ScreeningFormat.TwoD, decimal basePrice = 10m)
        {
            return new Screening { StartsAtUtc = start, Format = format, BasePrice = basePrice };
        }

        [Fact]
        public void Standard_seat_in_evening_2d_costs_base_price()
        {
            Assert.Equal(10.00m, Calculator().SeatPrice(Screening(FridayEvening), SeatType.Standard));
        }

        [Fact]
        public void Premium_seat_applies_multiplier()
        {
            Assert.Equal(13.50m, Calculator().SeatPrice(Screening(FridayEvening), SeatType.Premium));
        }

        [Fact]
        public void Accessible_space_is_charged_at_standard_rate()
        {
            Assert.Equal(10.00m, Calculator().SeatPrice(Screening(FridayEvening), SeatType.Accessible));
        }

        [Theory]
        [InlineData(ScreeningFormat.ThreeD, 12.00)]
        [InlineData(ScreeningFormat.Imax, 14.50)]
        public void Format_surcharge_is_added(ScreeningFormat format, double expected)
        {
            Assert.Equal((decimal)expected, Calculator().SeatPrice(Screening(FridayEvening, format), SeatType.Standard));
        }

        [Fact]
        public void Weekday_matinee_is_discounted_after_surcharges()
        {
            // (10 * 1.35 + 2.00) * 0.8 = 12.40
            Assert.Equal(12.40m, Calculator().SeatPrice(Screening(FridayAfternoon, ScreeningFormat.ThreeD), SeatType.Premium));
        }

        [Fact]
        public void Weekend_afternoon_is_not_a_matinee()
        {
            Assert.False(Calculator().IsMatinee(Screening(SaturdayAfternoon)));
            Assert.Equal(10.00m, Calculator().SeatPrice(Screening(SaturdayAfternoon), SeatType.Standard));
        }

        [Fact]
        public void Matinee_rule_uses_venue_local_time()
        {
            // 15:30 UTC is 18:30 in a UTC+3 venue -> evening, no discount.
            _options.TimeZone = TimeZoneInfo.CreateCustomTimeZone("UTC+3", TimeSpan.FromHours(3), "UTC+3", "UTC+3");
            var screening = Screening(new DateTime(2026, 3, 6, 15, 30, 0, DateTimeKind.Utc));

            Assert.False(Calculator().IsMatinee(screening));
        }

        [Fact]
        public void Prices_round_half_away_from_zero()
        {
            _options.PremiumMultiplier = 1.333m;
            // 7.50 * 1.333 = 9.9975 -> 10.00
            Assert.Equal(10.00m, Calculator().SeatPrice(Screening(FridayEvening, basePrice: 7.50m), SeatType.Premium));
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 0.75)]
        [InlineData(4, 3.00)]
        public void Service_fee_is_per_seat(int seats, double expected)
        {
            Assert.Equal((decimal)expected, Calculator().ServiceFee(seats));
        }
    }
}
