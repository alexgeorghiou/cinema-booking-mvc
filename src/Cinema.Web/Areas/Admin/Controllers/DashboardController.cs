using System;
using System.Linq;
using System.Web.Mvc;
using Cinema.Core.Abstractions;
using Cinema.Core.Domain;
using Cinema.Web.Areas.Admin.Models;

namespace Cinema.Web.Areas.Admin.Controllers
{
    public class DashboardController : AdminControllerBase
    {
        private readonly IScreeningRepository _screenings;
        private readonly IHallRepository _halls;
        private readonly IBookingRepository _bookings;
        private readonly IClock _clock;

        public DashboardController(IScreeningRepository screenings, IHallRepository halls, IBookingRepository bookings, IClock clock)
        {
            _screenings = screenings;
            _halls = halls;
            _bookings = bookings;
            _clock = clock;
        }

        public ActionResult Index()
        {
            var now = _clock.UtcNow;
            var model = new DashboardViewModel();
            var capacity = _halls.ListAll().ToDictionary(h => h.Id, h => h.Capacity);

            foreach (var screening in _screenings.ListBetween(now.AddHours(-3), now.AddDays(2)))
            {
                var bookings = _bookings.ListForScreening(screening.Id).Where(b => b.Status != BookingStatus.Cancelled).ToList();
                model.Upcoming.Add(new ScreeningOccupancy
                {
                    Screening = screening,
                    Sold = bookings.Sum(b => b.Seats.Count),
                    Capacity = capacity[screening.HallId],
                    Revenue = bookings.Sum(b => b.Total)
                });
            }

            ViewBag.Title = "Dashboard";
            return View(model);
        }
    }
}
