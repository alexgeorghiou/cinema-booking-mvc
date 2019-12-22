using System;
using System.Collections.Generic;
using Cinema.Core.Domain;

namespace Cinema.Core.Abstractions
{
    public interface IMovieRepository
    {
        Movie Find(int id);

        IReadOnlyList<Movie> ListAll();

        void Add(Movie movie);
    }

    public interface IHallRepository
    {
        /// <summary>Loads the hall together with its seats.</summary>
        Hall Find(int id);

        IReadOnlyList<Hall> ListAll();

        void Add(Hall hall);
    }

    public interface IScreeningRepository
    {
        /// <summary>Loads the screening with movie, hall and seats.</summary>
        Screening Find(int id);

        IReadOnlyList<Screening> ListBetween(DateTime fromUtc, DateTime toUtc);

        IReadOnlyList<Screening> ListForHall(int hallId, DateTime fromUtc, DateTime toUtc);

        void Add(Screening screening);
    }

    public interface ISeatHoldRepository
    {
        SeatHold Find(Guid id);

        /// <summary>Holds with status Active for the screening, regardless of expiry time.</summary>
        IReadOnlyList<SeatHold> ListActive(int screeningId);

        /// <summary>Holds still flagged Active whose expiry time has passed.</summary>
        IReadOnlyList<SeatHold> ListStale(DateTime nowUtc, int max);

        void Add(SeatHold hold);
    }

    public interface IBookingRepository
    {
        Booking FindByReference(string reference);

        bool ReferenceExists(string reference);

        /// <summary>Seat ids sold for a screening (excluding cancelled bookings).</summary>
        ISet<int> SoldSeatIds(int screeningId);

        IReadOnlyList<Booking> ListForUser(string userId);

        IReadOnlyList<Booking> ListForScreening(int screeningId);

        void Add(Booking booking);
    }

    /// <summary>Commits pending changes. Implementations translate store conflicts into <see cref="ConcurrencyConflictException"/>.</summary>
    public interface IUnitOfWork
    {
        void SaveChanges();
    }
}
