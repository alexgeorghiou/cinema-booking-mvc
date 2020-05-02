using System;
using System.Linq;
using System.Web.Mvc;
using Cinema.Core.Abstractions;
using Cinema.Core.Services;
using Cinema.Web.Areas.Admin.Models;
using Cinema.Web.Infrastructure;

namespace Cinema.Web.Areas.Admin.Controllers
{
    public class ScreeningsController : AdminControllerBase
    {
        private readonly IScreeningRepository _screenings;
        private readonly IMovieRepository _movies;
        private readonly IHallRepository _halls;
        private readonly IBookingRepository _bookings;
        private readonly IScreeningScheduler _scheduler;
        private readonly IClock _clock;

        public ScreeningsController(
            IScreeningRepository screenings,
            IMovieRepository movies,
            IHallRepository halls,
            IBookingRepository bookings,
            IScreeningScheduler scheduler,
            IClock clock)
        {
            _screenings = screenings;
            _movies = movies;
            _halls = halls;
            _bookings = bookings;
            _scheduler = scheduler;
            _clock = clock;
        }

        public ActionResult Index()
        {
            var now = _clock.UtcNow;
            ViewBag.Title = "Screenings";
            return View(_screenings.ListBetween(now.Date, now.Date.AddDays(14)));
        }

        public ActionResult Create()
        {
            ViewBag.Title = "Schedule screening";
            var tomorrowEvening = VenueTime.ToLocal(_clock.UtcNow).Date.AddDays(1).AddHours(19);
            return View(WithLists(new ScreeningForm { StartsAtLocal = tomorrowEvening, BasePrice = 10m }));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ScreeningForm form)
        {
            ViewBag.Title = "Schedule screening";
            if (!ModelState.IsValid)
            {
                return View(WithLists(form));
            }

            try
            {
                var screening = _scheduler.Schedule(new ScheduleRequest
                {
                    MovieId = form.MovieId,
                    HallId = form.HallId,
                    StartsAtUtc = VenueTime.ToUtc(form.StartsAtLocal.Value),
                    BasePrice = form.BasePrice,
                    Format = form.Format
                });

                Success(string.Format("Scheduled {0} in {1}.", screening.Movie.Title, screening.Hall.Name));
                return RedirectToAction("Index");
            }
            catch (SchedulingConflictException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(WithLists(form));
            }
        }

        public ActionResult Bookings(int id)
        {
            var screening = _screenings.Find(id);
            if (screening == null)
            {
                return HttpNotFound();
            }

            ViewBag.Title = "Bookings";
            return View(new ScreeningBookingsViewModel { Screening = screening, Bookings = _bookings.ListForScreening(id) });
        }

        private ScreeningForm WithLists(ScreeningForm form)
        {
            form.Movies = _movies.ListAll()
                .Where(m => m.IsActive)
                .Select(m => new SelectListItem { Value = m.Id.ToString(), Text = m.Title + " (" + m.DurationMinutes + " min)" });
            form.Halls = _halls.ListAll()
                .Select(h => new SelectListItem { Value = h.Id.ToString(), Text = h.Name + " - " + h.Capacity + " seats" });
            return form;
        }
    }
}
