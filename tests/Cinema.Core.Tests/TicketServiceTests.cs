using System;
using Cinema.Core.Domain;
using Cinema.Core.Services;
using Cinema.Core.Tests.Fakes;
using Xunit;

namespace Cinema.Core.Tests
{
    public class TicketServiceTests
    {
        private static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        private readonly TestCinema _cinema = new TestCinema();
        private readonly Booking _booking;

        public TicketServiceTests()
        {
            var hold = _cinema.Holds().Hold(_cinema.Screening.Id, "alice", _cinema.Seats("C2", "C3"));
            _booking = _cinema.Bookings().Confirm(hold.Id, "alice", "user-1", "alice@example.com");
        }

        private void DoorsOpen()
        {
            _cinema.Clock.UtcNow = _cinema.Screening.StartsAtUtc.AddMinutes(-30);
        }

        [Fact]
        public void Payload_contains_reference_screening_and_seat_count()
        {
            var payload = _cinema.Tickets().BuildPayload(_booking);

            Assert.StartsWith("CNM1." + _booking.Reference + "." + _cinema.Screening.Id + ".2.", payload);
        }

        [Fact]
        public void Qr_code_renders_as_png()
        {
            var png = _cinema.Tickets().RenderQrPng(_booking, 4);

            Assert.True(png.Length > 100);
            Assert.Equal(PngSignature, new ArraySegment<byte>(png, 0, 8));
        }

        [Fact]
        public void Genuine_ticket_is_valid_during_admission_window()
        {
            var tickets = _cinema.Tickets();
            DoorsOpen();

            var check = tickets.Verify(tickets.BuildPayload(_booking));

            Assert.Equal(TicketCheckOutcome.Valid, check.Outcome);
            Assert.Same(_booking, check.Booking);
        }

        [Fact]
        public void Ticket_is_not_admitted_on_the_wrong_day()
        {
            var tickets = _cinema.Tickets();

            Assert.Equal(TicketCheckOutcome.WrongDay, tickets.Verify(tickets.BuildPayload(_booking)).Outcome);
        }

        [Fact]
        public void Tampered_seat_count_breaks_the_signature()
        {
            var tickets = _cinema.Tickets();
            var tampered = tickets.BuildPayload(_booking).Replace("." + _cinema.Screening.Id + ".2.", "." + _cinema.Screening.Id + ".9.");

            Assert.Equal(TicketCheckOutcome.BadSignature, tickets.Verify(tampered).Outcome);
        }

        [Fact]
        public void Ticket_signed_with_another_key_is_rejected()
        {
            var payload = _cinema.Tickets().BuildPayload(_booking);
            _cinema.Options.TicketSigningKey = "rotated-key";

            Assert.Equal(TicketCheckOutcome.BadSignature, _cinema.Tickets().Verify(payload).Outcome);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("hello")]
        [InlineData("CNM1.ABC.x.2.sig")]
        [InlineData("XXX1.ABC.1.2.sig")]
        public void Garbage_is_malformed(string payload)
        {
            Assert.Equal(TicketCheckOutcome.Malformed, _cinema.Tickets().Verify(payload).Outcome);
        }

        [Fact]
        public void Check_in_admits_once()
        {
            var tickets = _cinema.Tickets();
            var payload = tickets.BuildPayload(_booking);
            DoorsOpen();

            Assert.True(tickets.CheckIn(payload).IsValid);
            Assert.Equal(BookingStatus.CheckedIn, _booking.Status);

            Assert.Equal(TicketCheckOutcome.AlreadyUsed, tickets.CheckIn(payload).Outcome);
        }

        [Fact]
        public void Cancelled_booking_is_refused_at_the_door()
        {
            var tickets = _cinema.Tickets();
            var payload = tickets.BuildPayload(_booking);
            _cinema.Bookings().Cancel(_booking.Reference, "user-1");
            DoorsOpen();

            Assert.Equal(TicketCheckOutcome.Cancelled, tickets.CheckIn(payload).Outcome);
        }
    }
}
