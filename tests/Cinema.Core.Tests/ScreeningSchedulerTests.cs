using System;
using Cinema.Core.Abstractions;
using Cinema.Core.Domain;
using Cinema.Core.Services;
using Cinema.Core.Tests.Fakes;
using Xunit;

namespace Cinema.Core.Tests
{
    public class ScreeningSchedulerTests
    {
        private readonly TestCinema _cinema = new TestCinema();

        private ScreeningScheduler Scheduler()
        {
            var s = _cinema.Store;
            return new ScreeningScheduler(s, s, s, s, _cinema.Clock, _cinema.Options);
        }

        private ScheduleRequest At(DateTime start, int? movieId = null)
        {
            return new ScheduleRequest
            {
                MovieId = movieId ?? _cinema.Movie.Id,
                HallId = _cinema.Hall.Id,
                StartsAtUtc = start,
                BasePrice = 9.5m,
                Format = ScreeningFormat.ThreeD
            };
        }

        // Existing screening: 19:00 - 21:00 + 20 min cleaning => hall busy until 21:20.
        private DateTime Existing => _cinema.Screening.StartsAtUtc;

        [Fact]
        public void Schedules_after_film_and_cleaning_buffer()
        {
            var screening = Scheduler().Schedule(At(Existing.AddMinutes(140)));

            Assert.Equal(Existing.AddMinutes(140), screening.StartsAtUtc);
            Assert.Equal(ScreeningFormat.ThreeD, screening.Format);
            Assert.Equal(2, _cinema.Store.Screenings.Count);
        }

        [Theory]
        [InlineData(139)]   // during cleaning of the existing screening
        [InlineData(60)]    // during the film
        [InlineData(0)]     // same start
        [InlineData(-139)]  // new film + cleaning would run into the existing start
        public void Rejects_overlaps(int offsetMinutes)
        {
            var ex = Assert.Throws<SchedulingConflictException>(() => Scheduler().Schedule(At(Existing.AddMinutes(offsetMinutes))));
            Assert.Contains("Screen 1", ex.Message);
        }

        [Fact]
        public void Allows_a_screening_that_finishes_cleaning_before_the_next_starts()
        {
            Scheduler().Schedule(At(Existing.AddMinutes(-140)));
        }

        [Fact]
        public void Other_halls_do_not_conflict()
        {
            var hall2 = new HallLayoutBuilder().Build("Screen 2", 2, 2, 0, null);
            _cinema.Store.Add(hall2);
            var request = At(Existing);
            request.HallId = hall2.Id;

            Assert.Equal(hall2.Id, Scheduler().Schedule(request).HallId);
        }

        [Fact]
        public void Archived_movies_cannot_be_scheduled()
        {
            _cinema.Movie.IsActive = false;

            Assert.Throws<SchedulingConflictException>(() => Scheduler().Schedule(At(Existing.AddDays(2))));
        }

        [Fact]
        public void Past_start_and_non_positive_price_are_rejected()
        {
            Assert.Throws<SchedulingConflictException>(() => Scheduler().Schedule(At(TestCinema.Now.AddMinutes(-1))));

            var free = At(Existing.AddDays(3));
            free.BasePrice = 0;
            Assert.Throws<SchedulingConflictException>(() => Scheduler().Schedule(free));
        }

        [Fact]
        public void Unknown_movie_or_hall_is_reported()
        {
            Assert.Throws<NotFoundException>(() => Scheduler().Schedule(At(Existing.AddDays(2), movieId: 777)));

            var request = At(Existing.AddDays(2));
            request.HallId = 777;
            Assert.Throws<NotFoundException>(() => Scheduler().Schedule(request));
        }
    }
}
