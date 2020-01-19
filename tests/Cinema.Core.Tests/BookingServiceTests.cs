using System;
using System.Linq;
using Cinema.Core.Abstractions;
using Cinema.Core.Domain;
using Cinema.Core.Services;
using Cinema.Core.Tests.Fakes;
using Xunit;

namespace Cinema.Core.Tests
{
    public class BookingServiceTests
    {
        private readonly TestCinema _cinema = new TestCinema();

        private SeatHold HoldFor(string holder, params string[] seats)
        {
            return _cinema.Holds().Hold(_cinema.Screening.Id, holder, _cinema.Seats(seats));
        }

        [Fact]
        public void Quote_prices_each_seat_and_adds_service_fee()
        {
            var hold = HoldFor("alice", "C2", "C3", "D2");

            var quote = _cinema.Bookings().Quote(hold.Id, "alice");

            Assert.Equal(new[] { "C2", "C3", "D2" }, quote.Lines.Select(l => l.Seat.Label));
            Assert.Equal(new[] { 10.00m, 10.00m, 13.50m }, quote.Lines.Select(l => l.Price));
            Assert.Equal(2.25m, quote.ServiceFee);
            Assert.Equal(35.75m, quote.Total);
        }

        [Fact]
        public void Confirm_creates_booking_and_converts_hold()
        {
            var hold = HoldFor("alice", "C2", "C3");

            var booking = _cinema.Bookings().Confirm(hold.Id, "alice", "user-1", " alice@example.com ");

            Assert.Equal(BookingStatus.Confirmed, booking.Status);
            Assert.Equal(8, booking.Reference.Length);
            Assert.Equal("alice@example.com", booking.CustomerEmail);
            Assert.Equal(21.50m, booking.Total);
            Assert.Equal(SeatHoldStatus.Converted, hold.Status);
            Assert.Equal(new[] { _cinema.Seat("C2"), _cinema.Seat("C3") }, _cinema.Store.SoldSeatIds(_cinema.Screening.Id).OrderBy(i => i));
        }

        [Fact]
        public void Confirmed_seats_are_sold_on_the_seat_map()
        {
            var hold = HoldFor("alice", "C2", "C3");
            _cinema.Bookings().Confirm(hold.Id, "alice", "user-1", "alice@example.com");

            var map = _cinema.SeatMaps().GetSeatMap(_cinema.Screening.Id, "bob");

            Assert.All(map.Rows.Single(r => r.Row == "C").Seats.Where(s => s.Number == 2 || s.Number == 3),
                s => Assert.Equal(SeatState.Sold, s.State));
        }

        [Fact]
        public void Expired_hold_cannot_be_confirmed()
        {
            var hold = HoldFor("alice", "C2");
            _cinema.Clock.Advance(_cinema.Options.HoldDuration);

            Assert.Throws<HoldExpiredException>(
                () => _cinema.Bookings().Confirm(hold.Id, "alice", "user-1", "alice@example.com"));
        }

        [Fact]
        public void Another_holders_hold_cannot_be_confirmed()
        {
            var hold = HoldFor("alice", "C2");

            Assert.Throws<NotFoundException>(
                () => _cinema.Bookings().Confirm(hold.Id, "mallory", "user-2", "m@example.com"));
        }

        [Fact]
        public void Hold_cannot_be_confirmed_twice()
        {
            var hold = HoldFor("alice", "C2");
            var service = _cinema.Bookings();
            service.Confirm(hold.Id, "alice", "user-1", "alice@example.com");

            Assert.Throws<HoldExpiredException>(() => service.Confirm(hold.Id, "alice", "user-1", "alice@example.com"));
            Assert.Single(_cinema.Store.Bookings);
        }

        [Fact]
        public void Seat_sold_behind_the_holds_back_releases_hold_and_fails()
        {
            var hold = HoldFor("alice", "C2");
            _cinema.Store.Add(new Booking
            {
                ScreeningId = _cinema.Screening.Id,
                Status = BookingStatus.Confirmed,
                Seats = { new BookedSeat { ScreeningId = _cinema.Screening.Id, SeatId = _cinema.Seat("C2") } }
            });

            Assert.Throws<SeatUnavailableException>(
                () => _cinema.Bookings().Confirm(hold.Id, "alice", "user-1", "alice@example.com"));
            Assert.Equal(SeatHoldStatus.Released, hold.Status);
        }

        [Fact]
        public void Email_is_required()
        {
            var hold = HoldFor("alice", "C2");

            Assert.Throws<CinemaDomainException>(() => _cinema.Bookings().Confirm(hold.Id, "alice", "user-1", " "));
        }

        [Fact]
        public void Reference_collisions_are_retried()
        {
            _cinema.References = new SequenceReferenceGenerator("AAAAAAAA", "AAAAAAAA", "BBBBBBBB");
            var service = _cinema.Bookings();

            var first = service.Confirm(HoldFor("alice", "B2").Id, "alice", "u1", "a@example.com");
            var second = service.Confirm(HoldFor("bob", "C5").Id, "bob", "u2", "b@example.com");

            Assert.Equal("AAAAAAAA", first.Reference);
            Assert.Equal("BBBBBBBB", second.Reference);
        }

        [Fact]
        public void Cancel_before_cutoff_frees_the_seats()
        {
            var service = _cinema.Bookings();
            var booking = service.Confirm(HoldFor("alice", "C2").Id, "alice", "user-1", "a@example.com");

            service.Cancel(booking.Reference, "user-1");

            Assert.Equal(BookingStatus.Cancelled, booking.Status);
            Assert.Equal(TestCinema.Now, booking.CancelledUtc);
            Assert.Empty(_cinema.Store.SoldSeatIds(_cinema.Screening.Id));
        }

        [Fact]
        public void Cancel_after_cutoff_is_refused()
        {
            var service = _cinema.Bookings();
            var booking = service.Confirm(HoldFor("alice", "C2").Id, "alice", "user-1", "a@example.com");
            _cinema.Clock.UtcNow = _cinema.Screening.StartsAtUtc.AddMinutes(-59);

            Assert.Throws<BookingNotCancellableException>(() => service.Cancel(booking.Reference, "user-1"));
        }

        [Fact]
        public void Users_cannot_cancel_other_peoples_bookings()
        {
            var service = _cinema.Bookings();
            var booking = service.Confirm(HoldFor("alice", "C2").Id, "alice", "user-1", "a@example.com");

            Assert.Throws<NotFoundException>(() => service.Cancel(booking.Reference, "user-2"));
        }

        [Fact]
        public void Reference_generator_uses_unambiguous_alphabet()
        {
            var generator = new BookingReferenceGenerator();
            var refs = Enumerable.Range(0, 200).Select(_ => generator.Next()).ToList();

            Assert.All(refs, r => Assert.Equal(8, r.Length));
            Assert.All(refs, r => Assert.DoesNotContain(r, c => "01IO".IndexOf(c) >= 0));
            Assert.True(refs.Distinct().Count() > 190);
        }
    }
}
