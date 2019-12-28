using System.Linq;
using Cinema.Core.Abstractions;

namespace Cinema.Core.Services
{
    public interface ISeatMapService
    {
        SeatMap GetSeatMap(int screeningId, string holderId);
    }

    public class SeatMapService : ISeatMapService
    {
        private readonly IScreeningRepository _screenings;
        private readonly ISeatHoldRepository _holds;
        private readonly IBookingRepository _bookings;
        private readonly IPriceCalculator _pricing;
        private readonly IClock _clock;

        public SeatMapService(
            IScreeningRepository screenings,
            ISeatHoldRepository holds,
            IBookingRepository bookings,
            IPriceCalculator pricing,
            IClock clock)
        {
            _screenings = screenings;
            _holds = holds;
            _bookings = bookings;
            _pricing = pricing;
            _clock = clock;
        }

        public SeatMap GetSeatMap(int screeningId, string holderId)
        {
            var screening = _screenings.Find(screeningId);
            if (screening == null)
            {
                throw new NotFoundException("Screening", screeningId);
            }

            var now = _clock.UtcNow;
            var availability = SeatAvailability.Load(screeningId, _holds, _bookings, now);

            var map = new SeatMap
            {
                ScreeningId = screening.Id,
                MovieTitle = screening.Movie.Title,
                HallName = screening.Hall.Name,
                StartsAtUtc = screening.StartsAtUtc,
                SeatVersion = screening.SeatVersion
            };

            foreach (var row in screening.Hall.Rows())
            {
                var mapRow = new SeatMapRow { Row = row.Key };
                foreach (var seat in row)
                {
                    var state = availability.StateFor(seat.Id, holderId);
                    mapRow.Seats.Add(new SeatMapSeat
                    {
                        SeatId = seat.Id,
                        Label = seat.Label,
                        Number = seat.Number,
                        Type = seat.Type.ToString(),
                        State = state,
                        Price = _pricing.SeatPrice(screening, seat.Type)
                    });

                    if (state == SeatState.Available)
                    {
                        map.AvailableCount++;
                    }
                }

                map.Rows.Add(mapRow);
            }

            if (holderId != null)
            {
                var mine = _holds.ListActive(screeningId)
                    .FirstOrDefault(h => h.HolderId == holderId && h.IsLive(now));
                if (mine != null)
                {
                    map.YourHoldId = mine.Id;
                    map.YourHoldExpiresUtc = mine.ExpiresUtc;
                }
            }

            return map;
        }
    }
}
