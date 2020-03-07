using System;
using System.Linq;
using System.Web.Mvc;
using Cinema.Core.Abstractions;
using Cinema.Web.Infrastructure;
using Cinema.Web.Models;

namespace Cinema.Web.Controllers
{
    public class HomeController : Controller
    {
        private const int DaysAhead = 7;

        private readonly IScreeningRepository _screenings;
        private readonly IClock _clock;

        public HomeController(IScreeningRepository screenings, IClock clock)
        {
            _screenings = screenings;
            _clock = clock;
        }

        // GET /?date=2026-03-06
        public ActionResult Index(DateTime? date)
        {
            var today = VenueTime.ToLocal(_clock.UtcNow).Date;
            var selected = date.HasValue && date.Value.Date >= today && date.Value.Date < today.AddDays(DaysAhead)
                ? date.Value.Date
                : today;

            var fromUtc = VenueTime.ToUtc(selected);
            if (fromUtc < _clock.UtcNow)
            {
                fromUtc = _clock.UtcNow;
            }

            var toUtc = VenueTime.ToUtc(selected.AddDays(1));

            var model = new ListingsViewModel
            {
                Date = selected,
                Days = Enumerable.Range(0, DaysAhead).Select(i => today.AddDays(i)).ToList(),
                Movies = _screenings.ListBetween(fromUtc, toUtc)
                    .GroupBy(s => s.Movie)
                    .OrderBy(g => g.Key.Title)
                    .Select(g => new MovieListing { Movie = g.Key, Screenings = g.OrderBy(s => s.StartsAtUtc).ToList() })
                    .ToList()
            };

            ViewBag.Title = "What's on";
            return View(model);
        }
    }
}
