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
        private readonly IBookingRepository _bookings;
        private readonly IClock _clock;

        public DashboardController(IScreeningRepository screenings, IBookingRepository bookings, IClock clock)
        {
            _screenings = screenings;
            _bookings = bookings;
            _clock = clock;
        }

        public ActionResult Index()
        {
            var now = _clock.UtcNow;
            var model = new DashboardViewModel();

            foreach (var screening in _screenings.ListBetween(now.AddHours(-3), now.AddDays(2)))
            {
                var bookings = _bookings.ListForScreening(screening.Id).Where(b => b.Status != BookingStatus.Cancelled).ToList();
                var full = _screenings.Find(screening.Id);
                model.Upcoming.Add(new ScreeningOccupancy
                {
                    Screening = screening,
                    Sold = bookings.Sum(b => b.Seats.Count),
                    Capacity = full.Hall.Capacity,
                    Revenue = bookings.Sum(b => b.Total)
                });
            }

            ViewBag.Title = "Dashboard";
            return View(model);
        }
    }
}
