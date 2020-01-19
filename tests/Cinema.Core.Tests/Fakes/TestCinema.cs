using System;
using System.Linq;
using Cinema.Core.Abstractions;
using Cinema.Core.Domain;
using Cinema.Core.Services;

namespace Cinema.Core.Tests.Fakes
{
    /// <summary>Wires real services to the in-memory store with a controllable clock.</summary>
    public sealed class TestCinema
    {
        public static readonly DateTime Now = new DateTime(2026, 3, 2, 10, 0, 0, DateTimeKind.Utc);

        public TestCinema()
        {
            Store = new InMemoryStore();
            Clock = new FakeClock(Now);
            Options = new CinemaOptions();

            Movie = new Movie { Title = "The Long Projection", DurationMinutes = 120, Rating = AgeRating.Twelve };
            Store.Add(Movie);

            Hall = new HallLayoutBuilder().Build("Screen 1", 4, 6, 1, new[] { "A1" });
            Store.Add(Hall);

            Screening = new Screening
            {
                MovieId = Movie.Id,
                HallId = Hall.Id,
                StartsAtUtc = Now.AddDays(1).Date.AddHours(19),
                BasePrice = 10m
            };
            Store.Add(Screening);
        }

        public InMemoryStore Store { get; }
        public FakeClock Clock { get; }
        public CinemaOptions Options { get; }
        public Movie Movie { get; }
        public Hall Hall { get; }
        public Screening Screening { get; }

        public PriceCalculator Pricing => new PriceCalculator(Options);

        public SeatHoldService Holds() =>
            new SeatHoldService(Store, Store, Store, Store, Clock, Options);

        public SeatMapService SeatMaps() =>
            new SeatMapService(Store, Store, Store, Pricing, Clock);

        public IBookingReferenceGenerator References { get; set; } = new BookingReferenceGenerator();

        public BookingService Bookings() =>
            new BookingService(Store, Store, Store, Store, Pricing, References, Clock, Options);

        public int Seat(string label) => Hall.Seats.Single(s => s.Label == label).Id;

        public int[] Seats(params string[] labels) => labels.Select(Seat).ToArray();
    }
}
