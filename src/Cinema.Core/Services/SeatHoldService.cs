using System;
using System.Collections.Generic;
using System.Linq;
using Cinema.Core.Abstractions;
using Cinema.Core.Domain;

namespace Cinema.Core.Services
{
    public interface ISeatHoldService
    {
        /// <summary>
        /// Holds the given seats for <paramref name="holderId"/>, replacing any previous hold
        /// the same holder had on this screening.
        /// </summary>
        SeatHold Hold(int screeningId, string holderId, IReadOnlyCollection<int> seatIds);

        void Release(Guid holdId, string holderId);

        /// <summary>Marks timed-out holds as expired. Returns how many were swept.</summary>
        int SweepExpired(int max = 200);
    }

    public class SeatHoldService : ISeatHoldService
    {
        private readonly IScreeningRepository _screenings;
        private readonly ISeatHoldRepository _holds;
        private readonly IBookingRepository _bookings;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClock _clock;
        private readonly CinemaOptions _options;

        public SeatHoldService(
            IScreeningRepository screenings,
            ISeatHoldRepository holds,
            IBookingRepository bookings,
            IUnitOfWork unitOfWork,
            IClock clock,
            CinemaOptions options)
        {
            _screenings = screenings;
            _holds = holds;
            _bookings = bookings;
            _unitOfWork = unitOfWork;
            _clock = clock;
            _options = options;
        }

        public SeatHold Hold(int screeningId, string holderId, IReadOnlyCollection<int> seatIds)
        {
            if (string.IsNullOrWhiteSpace(holderId))
            {
                throw new ArgumentException("A holder id is required.", "holderId");
            }

            var requested = (seatIds ?? new int[0]).Distinct().ToList();
            if (requested.Count == 0)
            {
                throw new InvalidSeatSelectionException("Select at least one seat.");
            }

            if (requested.Count > _options.MaxSeatsPerBooking)
            {
                throw new InvalidSeatSelectionException(
                    string.Format("You can book at most {0} seats at a time.", _options.MaxSeatsPerBooking));
            }

            var screening = _screenings.Find(screeningId);
            if (screening == null)
            {
                throw new NotFoundException("Screening", screeningId);
            }

            var now = _clock.UtcNow;
            if (screening.StartsAtUtc <= now)
            {
                throw new InvalidSeatSelectionException("Sales for this screening have closed.");
            }

            var hallSeats = screening.Hall.Seats.ToDictionary(s => s.Id);
            var foreign = requested.Where(id => !hallSeats.ContainsKey(id)).ToList();
            if (foreign.Count > 0)
            {
                throw new InvalidSeatSelectionException("Selected seats do not belong to this hall.");
            }

            var availability = SeatAvailability.Load(screeningId, _holds, _bookings, now);
            var taken = requested.Where(id => !availability.IsFreeFor(id, holderId)).ToList();
            if (taken.Count > 0)
            {
                throw new SeatUnavailableException(taken);
            }

            if (_options.EnforceNoSingleSeatGap)
            {
                var stranded = SingleSeatGapRule.FindStrandedSeats(
                    screening.Hall.Seats, requested, id => !availability.IsFreeFor(id, holderId));
                if (stranded.Count > 0)
                {
                    var labels = stranded.Select(id => hallSeats[id].Label);
                    throw new InvalidSeatSelectionException(
                        "Please don't leave a single empty seat (" + string.Join(", ", labels) + ") between taken seats.");
                }
            }

            foreach (var previous in _holds.ListActive(screeningId).Where(h => h.HolderId == holderId))
            {
                previous.Status = SeatHoldStatus.Released;
            }

            var hold = new SeatHold
            {
                Id = Guid.NewGuid(),
                ScreeningId = screeningId,
                Screening = screening,
                HolderId = holderId,
                CreatedUtc = now,
                ExpiresUtc = now.Add(_options.HoldDuration),
                Status = SeatHoldStatus.Active
            };

            foreach (var seatId in requested)
            {
                hold.Items.Add(new SeatHoldItem { SeatHoldId = hold.Id, SeatId = seatId, Seat = hallSeats[seatId] });
            }

            _holds.Add(hold);
            screening.TouchSeats();
            _unitOfWork.SaveChanges();

            return hold;
        }

        public void Release(Guid holdId, string holderId)
        {
            var hold = _holds.Find(holdId);
            if (hold == null || hold.HolderId != holderId || hold.Status != SeatHoldStatus.Active)
            {
                return;
            }

            hold.Status = SeatHoldStatus.Released;
            var screening = _screenings.Find(hold.ScreeningId);
            if (screening != null)
            {
                screening.TouchSeats();
            }

            _unitOfWork.SaveChanges();
        }

        public int SweepExpired(int max = 200)
        {
            var stale = _holds.ListStale(_clock.UtcNow, max);
            foreach (var hold in stale)
            {
                hold.Status = SeatHoldStatus.Expired;
            }

            if (stale.Count > 0)
            {
                _unitOfWork.SaveChanges();
            }

            return stale.Count;
        }
    }
}
