using System;
using System.Linq;
using Cinema.Core.Abstractions;
using Cinema.Core.Domain;
using Cinema.Core.Tests.Fakes;
using Xunit;

namespace Cinema.Core.Tests
{
    public class SeatHoldServiceTests
    {
        private readonly TestCinema _cinema = new TestCinema();

        [Fact]
        public void Hold_reserves_seats_until_configured_expiry()
        {
            var hold = _cinema.Holds().Hold(_cinema.Screening.Id, "alice", _cinema.Seats("B2", "B3"));

            Assert.Equal(SeatHoldStatus.Active, hold.Status);
            Assert.Equal(TestCinema.Now.AddMinutes(8), hold.ExpiresUtc);
            Assert.Equal(_cinema.Seats("B2", "B3"), hold.SeatIds.OrderBy(i => i));
            Assert.Equal(1, _cinema.Store.SaveCount);
        }

        [Fact]
        public void Hold_bumps_screening_seat_version_for_optimistic_concurrency()
        {
            var before = _cinema.Screening.SeatVersion;

            _cinema.Holds().Hold(_cinema.Screening.Id, "alice", _cinema.Seats("C1"));

            Assert.Equal(before + 1, _cinema.Screening.SeatVersion);
        }

        [Fact]
        public void Seats_held_by_someone_else_cannot_be_held()
        {
            var service = _cinema.Holds();
            service.Hold(_cinema.Screening.Id, "alice", _cinema.Seats("B2", "B3"));

            var ex = Assert.Throws<SeatUnavailableException>(
                () => service.Hold(_cinema.Screening.Id, "bob", _cinema.Seats("B3", "B4")));

            Assert.Equal(new[] { _cinema.Seat("B3") }, ex.SeatIds);
        }

        [Fact]
        public void Expired_holds_free_their_seats_without_a_sweep()
        {
            var service = _cinema.Holds();
            service.Hold(_cinema.Screening.Id, "alice", _cinema.Seats("B2", "B3"));

            _cinema.Clock.Advance(TimeSpan.FromMinutes(8));

            var hold = service.Hold(_cinema.Screening.Id, "bob", _cinema.Seats("B2", "B3"));
            Assert.Equal("bob", hold.HolderId);
        }

        [Fact]
        public void New_hold_replaces_the_holders_previous_hold()
        {
            var service = _cinema.Holds();
            var first = service.Hold(_cinema.Screening.Id, "alice", _cinema.Seats("B2", "B3"));

            var second = service.Hold(_cinema.Screening.Id, "alice", _cinema.Seats("B3", "B4"));

            Assert.Equal(SeatHoldStatus.Released, first.Status);
            Assert.Equal(SeatHoldStatus.Active, second.Status);
            Assert.Single(_cinema.Store.ListActive(_cinema.Screening.Id));
        }

        [Fact]
        public void Sold_seats_cannot_be_held()
        {
            _cinema.Store.Add(new Booking
            {
                ScreeningId = _cinema.Screening.Id,
                Status = BookingStatus.Confirmed,
                Seats = { new BookedSeat { ScreeningId = _cinema.Screening.Id, SeatId = _cinema.Seat("D4") } }
            });

            Assert.Throws<SeatUnavailableException>(
                () => _cinema.Holds().Hold(_cinema.Screening.Id, "alice", _cinema.Seats("D4")));
        }

        [Fact]
        public void Seats_from_a_cancelled_booking_are_available_again()
        {
            _cinema.Store.Add(new Booking
            {
                ScreeningId = _cinema.Screening.Id,
                Status = BookingStatus.Cancelled,
                Seats = { new BookedSeat { ScreeningId = _cinema.Screening.Id, SeatId = _cinema.Seat("D4") } }
            });

            var hold = _cinema.Holds().Hold(_cinema.Screening.Id, "alice", _cinema.Seats("D4"));
            Assert.Equal(SeatHoldStatus.Active, hold.Status);
        }

        [Fact]
        public void Rejects_empty_oversized_and_foreign_selections()
        {
            var service = _cinema.Holds();
            _cinema.Options.MaxSeatsPerBooking = 2;

            Assert.Throws<InvalidSeatSelectionException>(() => service.Hold(_cinema.Screening.Id, "a", new int[0]));
            Assert.Throws<InvalidSeatSelectionException>(() => service.Hold(_cinema.Screening.Id, "a", _cinema.Seats("A2", "A3", "A4")));
            Assert.Throws<InvalidSeatSelectionException>(() => service.Hold(_cinema.Screening.Id, "a", new[] { 99999 }));
        }

        [Fact]
        public void Duplicate_seat_ids_are_collapsed()
        {
            var seat = _cinema.Seat("C2");
            var hold = _cinema.Holds().Hold(_cinema.Screening.Id, "alice", new[] { seat, seat });

            Assert.Single(hold.Items);
        }

        [Fact]
        public void Sales_close_when_the_screening_starts()
        {
            _cinema.Clock.UtcNow = _cinema.Screening.StartsAtUtc;

            Assert.Throws<InvalidSeatSelectionException>(
                () => _cinema.Holds().Hold(_cinema.Screening.Id, "alice", _cinema.Seats("C2")));
        }

        [Fact]
        public void Unknown_screening_is_reported()
        {
            Assert.Throws<NotFoundException>(() => _cinema.Holds().Hold(424242, "alice", _cinema.Seats("C2")));
        }

        [Fact]
        public void Release_frees_seats_only_for_the_owner()
        {
            var service = _cinema.Holds();
            var hold = service.Hold(_cinema.Screening.Id, "alice", _cinema.Seats("B2"));

            service.Release(hold.Id, "mallory");
            Assert.Equal(SeatHoldStatus.Active, hold.Status);

            service.Release(hold.Id, "alice");
            Assert.Equal(SeatHoldStatus.Released, hold.Status);
        }

        [Fact]
        public void Sweep_marks_only_timed_out_holds_as_expired()
        {
            var service = _cinema.Holds();
            var old = service.Hold(_cinema.Screening.Id, "alice", _cinema.Seats("B2"));
            _cinema.Clock.Advance(TimeSpan.FromMinutes(5));
            var fresh = service.Hold(_cinema.Screening.Id, "bob", _cinema.Seats("C2"));
            _cinema.Clock.Advance(TimeSpan.FromMinutes(4));

            var swept = service.SweepExpired();

            Assert.Equal(1, swept);
            Assert.Equal(SeatHoldStatus.Expired, old.Status);
            Assert.Equal(SeatHoldStatus.Active, fresh.Status);
        }

        [Fact]
        public void Concurrency_conflicts_from_the_store_surface_to_the_caller()
        {
            _cinema.Store.FailNextSaveWith = new ConcurrencyConflictException();

            Assert.Throws<ConcurrencyConflictException>(
                () => _cinema.Holds().Hold(_cinema.Screening.Id, "alice", _cinema.Seats("B2")));
        }
    }
}
