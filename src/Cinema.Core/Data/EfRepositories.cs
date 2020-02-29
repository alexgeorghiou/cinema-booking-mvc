using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Cinema.Core.Abstractions;
using Cinema.Core.Domain;

namespace Cinema.Core.Data
{
    public class EfMovieRepository : IMovieRepository
    {
        private readonly CinemaDbContext _db;

        public EfMovieRepository(CinemaDbContext db)
        {
            _db = db;
        }

        public Movie Find(int id)
        {
            return _db.Movies.Find(id);
        }

        public IReadOnlyList<Movie> ListAll()
        {
            return _db.Movies.OrderBy(m => m.Title).ToList();
        }

        public void Add(Movie movie)
        {
            _db.Movies.Add(movie);
        }
    }

    public class EfHallRepository : IHallRepository
    {
        private readonly CinemaDbContext _db;

        public EfHallRepository(CinemaDbContext db)
        {
            _db = db;
        }

        public Hall Find(int id)
        {
            return _db.Halls.Include(h => h.Seats).SingleOrDefault(h => h.Id == id);
        }

        public IReadOnlyList<Hall> ListAll()
        {
            return _db.Halls.Include(h => h.Seats).OrderBy(h => h.Name).ToList();
        }

        public void Add(Hall hall)
        {
            _db.Halls.Add(hall);
        }
    }

    public class EfScreeningRepository : IScreeningRepository
    {
        private readonly CinemaDbContext _db;

        public EfScreeningRepository(CinemaDbContext db)
        {
            _db = db;
        }

        public Screening Find(int id)
        {
            return _db.Screenings
                .Include(s => s.Movie)
                .Include(s => s.Hall.Seats)
                .SingleOrDefault(s => s.Id == id);
        }

        public IReadOnlyList<Screening> ListBetween(DateTime fromUtc, DateTime toUtc)
        {
            return _db.Screenings
                .Include(s => s.Movie)
                .Include(s => s.Hall)
                .Where(s => s.StartsAtUtc >= fromUtc && s.StartsAtUtc < toUtc)
                .OrderBy(s => s.StartsAtUtc)
                .ToList();
        }

        public IReadOnlyList<Screening> ListForHall(int hallId, DateTime fromUtc, DateTime toUtc)
        {
            return _db.Screenings
                .Include(s => s.Movie)
                .Where(s => s.HallId == hallId && s.StartsAtUtc >= fromUtc && s.StartsAtUtc < toUtc)
                .ToList();
        }

        public void Add(Screening screening)
        {
            _db.Screenings.Add(screening);
        }
    }

    public class EfSeatHoldRepository : ISeatHoldRepository
    {
        private readonly CinemaDbContext _db;

        public EfSeatHoldRepository(CinemaDbContext db)
        {
            _db = db;
        }

        public SeatHold Find(Guid id)
        {
            return _db.SeatHolds.Include(h => h.Items).SingleOrDefault(h => h.Id == id);
        }

        public IReadOnlyList<SeatHold> ListActive(int screeningId)
        {
            return _db.SeatHolds
                .Include(h => h.Items)
                .Where(h => h.ScreeningId == screeningId && h.Status == SeatHoldStatus.Active)
                .ToList();
        }

        public IReadOnlyList<SeatHold> ListStale(DateTime nowUtc, int max)
        {
            return _db.SeatHolds
                .Where(h => h.Status == SeatHoldStatus.Active && h.ExpiresUtc <= nowUtc)
                .OrderBy(h => h.ExpiresUtc)
                .Take(max)
                .ToList();
        }

        public void Add(SeatHold hold)
        {
            _db.SeatHolds.Add(hold);
        }
    }

    public class EfBookingRepository : IBookingRepository
    {
        private readonly CinemaDbContext _db;

        public EfBookingRepository(CinemaDbContext db)
        {
            _db = db;
        }

        public Booking FindByReference(string reference)
        {
            return WithDetails().SingleOrDefault(b => b.Reference == reference);
        }

        public bool ReferenceExists(string reference)
        {
            return _db.Bookings.Any(b => b.Reference == reference);
        }

        public ISet<int> SoldSeatIds(int screeningId)
        {
            return new HashSet<int>(_db.BookedSeats
                .Where(s => s.ScreeningId == screeningId && s.IsActive)
                .Select(s => s.SeatId));
        }

        public IReadOnlyList<Booking> ListForUser(string userId)
        {
            return WithDetails()
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.CreatedUtc)
                .ToList();
        }

        public IReadOnlyList<Booking> ListForScreening(int screeningId)
        {
            return WithDetails()
                .Where(b => b.ScreeningId == screeningId)
                .OrderBy(b => b.CreatedUtc)
                .ToList();
        }

        public void Add(Booking booking)
        {
            _db.Bookings.Add(booking);
        }

        private IQueryable<Booking> WithDetails()
        {
            return _db.Bookings
                .Include(b => b.Seats.Select(s => s.Seat))
                .Include(b => b.Screening.Movie)
                .Include(b => b.Screening.Hall);
        }
    }
}
