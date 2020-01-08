using System;
using System.Linq;
using Cinema.Core.Abstractions;
using Cinema.Core.Services;
using Cinema.Core.Tests.Fakes;
using Xunit;

namespace Cinema.Core.Tests
{
    public class SeatMapServiceTests
    {
        private readonly TestCinema _cinema = new TestCinema();

        private SeatMapSeat SeatIn(SeatMap map, string label)
        {
            return map.Rows.SelectMany(r => r.Seats).Single(s => s.Label == label);
        }

        [Fact]
        public void Map_lists_rows_front_to_back_with_prices()
        {
            var map = _cinema.SeatMaps().GetSeatMap(_cinema.Screening.Id, null);

            Assert.Equal(new[] { "A", "B", "C", "D" }, map.Rows.Select(r => r.Row));
            Assert.Equal(24, map.AvailableCount);
            Assert.Equal(10.00m, SeatIn(map, "B1").Price);
            Assert.Equal(13.50m, SeatIn(map, "D1").Price);
            Assert.Equal("Accessible", SeatIn(map, "A1").Type);
        }

        [Fact]
        public void Map_distinguishes_own_hold_from_other_holds()
        {
            var holds = _cinema.Holds();
            var mine = holds.Hold(_cinema.Screening.Id, "alice", _cinema.Seats("B1", "B2"));
            holds.Hold(_cinema.Screening.Id, "bob", _cinema.Seats("C1"));

            var map = _cinema.SeatMaps().GetSeatMap(_cinema.Screening.Id, "alice");

            Assert.Equal(SeatState.HeldByYou, SeatIn(map, "B1").State);
            Assert.Equal(SeatState.Held, SeatIn(map, "C1").State);
            Assert.Equal(mine.Id, map.YourHoldId);
            Assert.Equal(mine.ExpiresUtc, map.YourHoldExpiresUtc);
            Assert.Equal(21, map.AvailableCount);
        }

        [Fact]
        public void Expired_holds_show_as_available()
        {
            _cinema.Holds().Hold(_cinema.Screening.Id, "bob", _cinema.Seats("C1"));
            _cinema.Clock.Advance(TimeSpan.FromMinutes(10));

            var map = _cinema.SeatMaps().GetSeatMap(_cinema.Screening.Id, "alice");

            Assert.Equal(SeatState.Available, SeatIn(map, "C1").State);
            Assert.Null(map.YourHoldId);
        }

        [Fact]
        public void Unknown_screening_throws()
        {
            Assert.Throws<NotFoundException>(() => _cinema.SeatMaps().GetSeatMap(-1, null));
        }
    }
}
