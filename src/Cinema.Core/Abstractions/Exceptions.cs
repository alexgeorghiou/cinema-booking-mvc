using System;
using System.Collections.Generic;
using System.Linq;

namespace Cinema.Core.Abstractions
{
    /// <summary>Base type for rule violations that should be shown to the customer.</summary>
    [Serializable]
    public class CinemaDomainException : Exception
    {
        public CinemaDomainException(string message) : base(message)
        {
        }
    }

    [Serializable]
    public class NotFoundException : CinemaDomainException
    {
        public NotFoundException(string what, object key)
            : base(string.Format("{0} '{1}' was not found.", what, key))
        {
        }
    }

    [Serializable]
    public class SeatUnavailableException : CinemaDomainException
    {
        public SeatUnavailableException(IEnumerable<int> seatIds)
            : this(seatIds, "One or more selected seats are no longer available.")
        {
        }

        public SeatUnavailableException(IEnumerable<int> seatIds, string message)
            : base(message)
        {
            SeatIds = seatIds.ToList();
        }

        public IReadOnlyList<int> SeatIds { get; private set; }
    }

    [Serializable]
    public class InvalidSeatSelectionException : CinemaDomainException
    {
        public InvalidSeatSelectionException(string message) : base(message)
        {
        }
    }

    [Serializable]
    public class HoldExpiredException : CinemaDomainException
    {
        public HoldExpiredException()
            : base("Your seat hold has expired. Please pick your seats again.")
        {
        }
    }

    [Serializable]
    public class SchedulingConflictException : CinemaDomainException
    {
        public SchedulingConflictException(string message) : base(message)
        {
        }
    }

    [Serializable]
    public class BookingNotCancellableException : CinemaDomainException
    {
        public BookingNotCancellableException(string message) : base(message)
        {
        }
    }

    /// <summary>Raised when another request changed seat availability first; the caller should refresh and retry.</summary>
    [Serializable]
    public class ConcurrencyConflictException : CinemaDomainException
    {
        public ConcurrencyConflictException()
            : base("Seat availability changed while you were choosing. Please try again.")
        {
        }
    }
}
