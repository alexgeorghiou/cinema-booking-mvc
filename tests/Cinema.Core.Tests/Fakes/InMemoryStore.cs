using System;
using System.Collections.Generic;
using System.Linq;
using Cinema.Core.Abstractions;
using Cinema.Core.Domain;

namespace Cinema.Core.Tests.Fakes
{
    /// <summary>
    /// A single in-memory store implementing every repository port, assigning identities
    /// the way the database would. Good enough to exercise service rules without EF.
    /// </summary>
    public sealed class InMemoryStore :
        IMovieRepository, IHallRepository, IScreeningRepository, ISeatHoldRepository, IBookingRepository, IUnitOfWork
    {
        private int _nextId = 1;

        public List<Movie> Movies { get; } = new List<Movie>();
        public List<Hall> Halls { get; } = new List<Hall>();
        public List<Screening> Screenings { get; } = new List<Screening>();
        public List<SeatHold> Holds { get; } = new List<SeatHold>();
        public List<Booking> Bookings { get; } = new List<Booking>();

        public int SaveCount { get; private set; }

        /// <summary>When set, the next SaveChanges throws this instead of committing.</summary>
        public Exception FailNextSaveWith { get; set; }

        Movie IMovieRepository.Find(int id) => Movies.SingleOrDefault(m => m.Id == id);
        IReadOnlyList<Movie> IMovieRepository.ListAll() => Movies.ToList();
        public void Add(Movie movie) { movie.Id = _nextId++; Movies.Add(movie); }

        Hall IHallRepository.Find(int id) => Halls.SingleOrDefault(h => h.Id == id);
        IReadOnlyList<Hall> IHallRepository.ListAll() => Halls.ToList();

        public void Add(Hall hall)
        {
            hall.Id = _nextId++;
            foreach (var seat in hall.Seats)
            {
                seat.Id = _nextId++;
                seat.HallId = hall.Id;
                seat.Hall = hall;
            }

            Halls.Add(hall);
        }

        Screening IScreeningRepository.Find(int id) => Screenings.SingleOrDefault(s => s.Id == id);

        public IReadOnlyList<Screening> ListBetween(DateTime fromUtc, DateTime toUtc) =>
            Screenings.Where(s => s.StartsAtUtc >= fromUtc && s.StartsAtUtc < toUtc).OrderBy(s => s.StartsAtUtc).ToList();

        public IReadOnlyList<Screening> ListForHall(int hallId, DateTime fromUtc, DateTime toUtc) =>
            Screenings.Where(s => s.HallId == hallId && s.StartsAtUtc >= fromUtc && s.StartsAtUtc < toUtc).ToList();

        public void Add(Screening screening)
        {
            screening.Id = _nextId++;
            screening.Movie = screening.Movie ?? Movies.Single(m => m.Id == screening.MovieId);
            screening.Hall = screening.Hall ?? Halls.Single(h => h.Id == screening.HallId);
            Screenings.Add(screening);
        }

        SeatHold ISeatHoldRepository.Find(Guid id) => Holds.SingleOrDefault(h => h.Id == id);

        public IReadOnlyList<SeatHold> ListActive(int screeningId) =>
            Holds.Where(h => h.ScreeningId == screeningId && h.Status == SeatHoldStatus.Active).ToList();

        public IReadOnlyList<SeatHold> ListStale(DateTime nowUtc, int max) =>
            Holds.Where(h => h.Status == SeatHoldStatus.Active && h.ExpiresUtc <= nowUtc).Take(max).ToList();

        public void Add(SeatHold hold) => Holds.Add(hold);

        public Booking FindByReference(string reference) =>
            Bookings.SingleOrDefault(b => string.Equals(b.Reference, reference, StringComparison.OrdinalIgnoreCase));

        public bool ReferenceExists(string reference) => FindByReference(reference) != null;

        public ISet<int> SoldSeatIds(int screeningId) =>
            new HashSet<int>(Bookings
                .Where(b => b.ScreeningId == screeningId && b.Status != BookingStatus.Cancelled)
                .SelectMany(b => b.Seats)
                .Where(s => s.IsActive)
                .Select(s => s.SeatId));

        public IReadOnlyList<Booking> ListForUser(string userId) => Bookings.Where(b => b.UserId == userId).ToList();

        public IReadOnlyList<Booking> ListForScreening(int screeningId) => Bookings.Where(b => b.ScreeningId == screeningId).ToList();

        public void Add(Booking booking)
        {
            booking.Id = _nextId++;
            Bookings.Add(booking);
        }

        public void SaveChanges()
        {
            if (FailNextSaveWith != null)
            {
                var ex = FailNextSaveWith;
                FailNextSaveWith = null;
                throw ex;
            }

            SaveCount++;
        }
    }
}
