using System;
using System.Collections.Generic;
using System.Linq;
using Cinema.Core.Abstractions;
using Cinema.Core.Domain;

namespace Cinema.Core.Services
{
    public interface IBookingService
    {
        /// <summary>Quote for a live hold: per-seat prices, fee and total.</summary>
        BookingQuote Quote(Guid holdId, string holderId);

        /// <summary>Turns a live hold into a confirmed booking.</summary>
        Booking Confirm(Guid holdId, string holderId, string userId, string customerEmail);

        Booking Cancel(string reference, string userId);

        Booking Find(string reference);

        IReadOnlyList<Booking> ForUser(string userId);
    }

    public sealed class BookingQuote
    {
        public BookingQuote()
        {
            Lines = new List<BookingQuoteLine>();
        }

        public Guid HoldId { get; set; }

        public Screening Screening { get; set; }

        public DateTime ExpiresUtc { get; set; }

        public List<BookingQuoteLine> Lines { get; set; }

        public decimal ServiceFee { get; set; }

        public decimal Total { get; set; }
    }

    public sealed class BookingQuoteLine
    {
        public Seat Seat { get; set; }

        public decimal Price { get; set; }
    }

    public class BookingService : IBookingService
    {
        private const int MaxReferenceAttempts = 5;

        private readonly IScreeningRepository _screenings;
        private readonly ISeatHoldRepository _holds;
        private readonly IBookingRepository _bookings;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPriceCalculator _pricing;
        private readonly IBookingReferenceGenerator _references;
        private readonly IClock _clock;
        private readonly CinemaOptions _options;

        public BookingService(
            IScreeningRepository screenings,
            ISeatHoldRepository holds,
            IBookingRepository bookings,
            IUnitOfWork unitOfWork,
            IPriceCalculator pricing,
            IBookingReferenceGenerator references,
            IClock clock,
            CinemaOptions options)
        {
            _screenings = screenings;
            _holds = holds;
            _bookings = bookings;
            _unitOfWork = unitOfWork;
            _pricing = pricing;
            _references = references;
            _clock = clock;
            _options = options;
        }

        public BookingQuote Quote(Guid holdId, string holderId)
        {
            var hold = LiveHold(holdId, holderId);
            var screening = LoadScreening(hold.ScreeningId);
            var seats = screening.Hall.Seats.ToDictionary(s => s.Id);

            var quote = new BookingQuote { HoldId = hold.Id, Screening = screening, ExpiresUtc = hold.ExpiresUtc };
            foreach (var seatId in hold.SeatIds.OrderBy(id => id))
            {
                var seat = seats[seatId];
                quote.Lines.Add(new BookingQuoteLine { Seat = seat, Price = _pricing.SeatPrice(screening, seat.Type) });
            }

            quote.Lines = quote.Lines.OrderBy(l => l.Seat.RowIndex).ThenBy(l => l.Seat.Number).ToList();
            quote.ServiceFee = _pricing.ServiceFee(quote.Lines.Count);
            quote.Total = quote.Lines.Sum(l => l.Price) + quote.ServiceFee;
            return quote;
        }

        public Booking Confirm(Guid holdId, string holderId, string userId, string customerEmail)
        {
            if (string.IsNullOrWhiteSpace(customerEmail))
            {
                throw new CinemaDomainException("An email address is required for the e-ticket.");
            }

            var quote = Quote(holdId, holderId);
            var hold = _holds.Find(holdId);

            // A hold guarantees seats against other holds, but re-check sales defensively:
            // the unique (ScreeningId, SeatId) index in the database is the final arbiter.
            var sold = _bookings.SoldSeatIds(hold.ScreeningId);
            var clash = hold.SeatIds.Where(sold.Contains).ToList();
            if (clash.Count > 0)
            {
                hold.Status = SeatHoldStatus.Released;
                _unitOfWork.SaveChanges();
                throw new SeatUnavailableException(clash);
            }

            var booking = new Booking
            {
                Reference = NewReference(),
                ScreeningId = quote.Screening.Id,
                Screening = quote.Screening,
                UserId = userId,
                CustomerEmail = customerEmail.Trim(),
                CreatedUtc = _clock.UtcNow,
                Status = BookingStatus.Confirmed,
                ServiceFee = quote.ServiceFee,
                Total = quote.Total
            };

            foreach (var line in quote.Lines)
            {
                booking.Seats.Add(new BookedSeat
                {
                    ScreeningId = quote.Screening.Id,
                    SeatId = line.Seat.Id,
                    Seat = line.Seat,
                    Price = line.Price
                });
            }

            _bookings.Add(booking);
            hold.Status = SeatHoldStatus.Converted;
            quote.Screening.TouchSeats();
            _unitOfWork.SaveChanges();

            return booking;
        }

        public Booking Cancel(string reference, string userId)
        {
            var booking = _bookings.FindByReference(reference);
            if (booking == null || booking.UserId != userId)
            {
                throw new NotFoundException("Booking", reference);
            }

            if (booking.Status != BookingStatus.Confirmed)
            {
                throw new BookingNotCancellableException("Only confirmed, unused bookings can be cancelled.");
            }

            var screening = LoadScreening(booking.ScreeningId);
            var deadline = screening.StartsAtUtc - _options.CancellationCutoff;
            if (_clock.UtcNow > deadline)
            {
                throw new BookingNotCancellableException(string.Format(
                    "Bookings can be cancelled up to {0:0} minutes before the screening.",
                    _options.CancellationCutoff.TotalMinutes));
            }

            booking.Status = BookingStatus.Cancelled;
            booking.CancelledUtc = _clock.UtcNow;
            screening.TouchSeats();
            _unitOfWork.SaveChanges();
            return booking;
        }

        public Booking Find(string reference)
        {
            return string.IsNullOrWhiteSpace(reference) ? null : _bookings.FindByReference(reference.Trim());
        }

        public IReadOnlyList<Booking> ForUser(string userId)
        {
            return _bookings.ListForUser(userId);
        }

        private SeatHold LiveHold(Guid holdId, string holderId)
        {
            var hold = _holds.Find(holdId);
            if (hold == null || hold.HolderId != holderId)
            {
                throw new NotFoundException("Seat hold", holdId);
            }

            if (!hold.IsLive(_clock.UtcNow))
            {
                throw new HoldExpiredException();
            }

            return hold;
        }

        private Screening LoadScreening(int id)
        {
            var screening = _screenings.Find(id);
            if (screening == null)
            {
                throw new NotFoundException("Screening", id);
            }

            return screening;
        }

        private string NewReference()
        {
            for (var attempt = 0; attempt < MaxReferenceAttempts; attempt++)
            {
                var candidate = _references.Next();
                if (!_bookings.ReferenceExists(candidate))
                {
                    return candidate;
                }
            }

            throw new InvalidOperationException("Could not allocate a unique booking reference.");
        }
    }
}
