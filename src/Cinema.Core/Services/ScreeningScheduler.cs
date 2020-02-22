using System;
using System.Linq;
using Cinema.Core.Abstractions;
using Cinema.Core.Domain;

namespace Cinema.Core.Services
{
    public sealed class ScheduleRequest
    {
        public int MovieId { get; set; }

        public int HallId { get; set; }

        public DateTime StartsAtUtc { get; set; }

        public decimal BasePrice { get; set; }

        public ScreeningFormat Format { get; set; }
    }

    public interface IScreeningScheduler
    {
        Screening Schedule(ScheduleRequest request);
    }

    /// <summary>
    /// Admin-side scheduling. A hall is busy from a screening's start until the film ends plus
    /// the cleaning buffer; new screenings may not overlap that window.
    /// </summary>
    public class ScreeningScheduler : IScreeningScheduler
    {
        private static readonly TimeSpan LongestFilm = TimeSpan.FromHours(5);

        private readonly IMovieRepository _movies;
        private readonly IHallRepository _halls;
        private readonly IScreeningRepository _screenings;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClock _clock;
        private readonly CinemaOptions _options;

        public ScreeningScheduler(
            IMovieRepository movies,
            IHallRepository halls,
            IScreeningRepository screenings,
            IUnitOfWork unitOfWork,
            IClock clock,
            CinemaOptions options)
        {
            _movies = movies;
            _halls = halls;
            _screenings = screenings;
            _unitOfWork = unitOfWork;
            _clock = clock;
            _options = options;
        }

        public Screening Schedule(ScheduleRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            var movie = _movies.Find(request.MovieId);
            if (movie == null)
            {
                throw new NotFoundException("Movie", request.MovieId);
            }

            if (!movie.IsActive)
            {
                throw new SchedulingConflictException("'" + movie.Title + "' is archived and cannot be scheduled.");
            }

            var hall = _halls.Find(request.HallId);
            if (hall == null)
            {
                throw new NotFoundException("Hall", request.HallId);
            }

            if (request.BasePrice <= 0)
            {
                throw new SchedulingConflictException("Base price must be positive.");
            }

            if (request.StartsAtUtc <= _clock.UtcNow)
            {
                throw new SchedulingConflictException("Screenings must be scheduled in the future.");
            }

            var start = request.StartsAtUtc;
            var end = start + movie.Duration + _options.CleaningBuffer;

            // Anything starting up to LongestFilm before us could still be running.
            var clash = _screenings
                .ListForHall(hall.Id, start - LongestFilm - _options.CleaningBuffer, end)
                .FirstOrDefault(other =>
                {
                    var otherEnd = other.StartsAtUtc + other.Movie.Duration + _options.CleaningBuffer;
                    return other.StartsAtUtc < end && start < otherEnd;
                });

            if (clash != null)
            {
                throw new SchedulingConflictException(string.Format(
                    "{0} is busy with '{1}' starting {2:yyyy-MM-dd HH:mm} UTC (including {3:0} min cleaning).",
                    hall.Name, clash.Movie.Title, clash.StartsAtUtc, _options.CleaningBuffer.TotalMinutes));
            }

            var screening = new Screening
            {
                MovieId = movie.Id,
                Movie = movie,
                HallId = hall.Id,
                Hall = hall,
                StartsAtUtc = DateTime.SpecifyKind(start, DateTimeKind.Utc),
                BasePrice = request.BasePrice,
                Format = request.Format
            };

            _screenings.Add(screening);
            _unitOfWork.SaveChanges();
            return screening;
        }
    }
}
