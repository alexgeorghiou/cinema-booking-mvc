using System;
using System.Collections.Generic;
using System.Linq;
using Cinema.Core.Abstractions;
using Cinema.Core.Domain;

namespace Cinema.Core.Services
{
    /// <summary>Snapshot of who occupies which seat in a screening at a point in time.</summary>
    public sealed class SeatAvailability
    {
        private readonly ISet<int> _sold;
        private readonly IDictionary<int, SeatHold> _heldBy;

        private SeatAvailability(ISet<int> sold, IDictionary<int, SeatHold> heldBy)
        {
            _sold = sold;
            _heldBy = heldBy;
        }

        public static SeatAvailability Load(
            int screeningId, ISeatHoldRepository holds, IBookingRepository bookings, DateTime nowUtc)
        {
            var sold = bookings.SoldSeatIds(screeningId);
            var heldBy = new Dictionary<int, SeatHold>();
            foreach (var hold in holds.ListActive(screeningId).Where(h => h.IsLive(nowUtc)))
            {
                foreach (var seatId in hold.SeatIds)
                {
                    heldBy[seatId] = hold;
                }
            }

            return new SeatAvailability(sold, heldBy);
        }

        public bool IsSold(int seatId)
        {
            return _sold.Contains(seatId);
        }

        public SeatHold HoldFor(int seatId)
        {
            SeatHold hold;
            return _heldBy.TryGetValue(seatId, out hold) ? hold : null;
        }

        public SeatState StateFor(int seatId, string holderId)
        {
            if (IsSold(seatId))
            {
                return SeatState.Sold;
            }

            var hold = HoldFor(seatId);
            if (hold == null)
            {
                return SeatState.Available;
            }

            return holderId != null && hold.HolderId == holderId ? SeatState.HeldByYou : SeatState.Held;
        }

        /// <summary>True when the seat is free for <paramref name="holderId"/> (unoccupied, or held by them).</summary>
        public bool IsFreeFor(int seatId, string holderId)
        {
            var state = StateFor(seatId, holderId);
            return state == SeatState.Available || state == SeatState.HeldByYou;
        }
    }
}
