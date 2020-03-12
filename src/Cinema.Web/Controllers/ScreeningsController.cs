using System.Linq;
using System.Net;
using System.Web.Mvc;
using System.Web.UI;
using Cinema.Core.Abstractions;
using Cinema.Core.Services;
using Cinema.Web.Infrastructure;
using Cinema.Web.Models;

namespace Cinema.Web.Controllers
{
    [RoutePrefix("screenings")]
    public class ScreeningsController : Controller
    {
        private readonly IScreeningRepository _screenings;
        private readonly ISeatMapService _seatMaps;
        private readonly ISeatHoldService _holds;
        private readonly CinemaOptions _options;

        public ScreeningsController(
            IScreeningRepository screenings,
            ISeatMapService seatMaps,
            ISeatHoldService holds,
            CinemaOptions options)
        {
            _screenings = screenings;
            _seatMaps = seatMaps;
            _holds = holds;
            _options = options;
        }

        // GET /screenings/42
        [Route("{id:int}")]
        public ActionResult Details(int id)
        {
            var screening = _screenings.Find(id);
            if (screening == null)
            {
                return HttpNotFound();
            }

            HolderCookie.GetOrCreate(HttpContext);
            ViewBag.Title = screening.Movie.Title;
            ViewBag.MaxSeats = _options.MaxSeatsPerBooking;
            return View(screening);
        }

        // GET /screenings/42/seats  – polled by the seat map
        [Route("{id:int}/seats")]
        [OutputCache(Location = OutputCacheLocation.None, NoStore = true)]
        public ActionResult Seats(int id)
        {
            var holder = HolderCookie.GetOrCreate(HttpContext);
            var map = _seatMaps.GetSeatMap(id, holder);
            return Json(new
            {
                map.ScreeningId,
                map.SeatVersion,
                map.AvailableCount,
                holdId = map.YourHoldId,
                holdExpiresUtc = map.YourHoldExpiresUtc.HasValue ? map.YourHoldExpiresUtc.Value.ToString("o") : null,
                rows = map.Rows.Select(r => new
                {
                    row = r.Row,
                    seats = r.Seats.Select(s => new
                    {
                        id = s.SeatId,
                        label = s.Label,
                        number = s.Number,
                        type = s.Type,
                        state = s.State.ToString(),
                        price = s.Price
                    })
                })
            }, JsonRequestBehavior.AllowGet);
        }

        // POST /screenings/42/hold  { seatIds: [..] }
        [HttpPost]
        [Route("{id:int}/hold")]
        [ValidateJsonAntiForgeryToken]
        public ActionResult Hold(int id, HoldRequest request)
        {
            var holder = HolderCookie.GetOrCreate(HttpContext);
            var hold = _holds.Hold(id, holder, request == null ? null : request.SeatIds);

            Response.StatusCode = (int)HttpStatusCode.Created;
            return Json(new
            {
                holdId = hold.Id,
                expiresUtc = hold.ExpiresUtc.ToString("o"),
                checkoutUrl = Url.Action("Checkout", "Bookings", new { holdId = hold.Id })
            });
        }

        // POST /screenings/hold/release
        [HttpPost]
        [Route("hold/release")]
        [ValidateJsonAntiForgeryToken]
        public ActionResult Release(ReleaseRequest request)
        {
            if (request != null)
            {
                _holds.Release(request.HoldId, HolderCookie.GetOrCreate(HttpContext));
            }

            return new HttpStatusCodeResult(HttpStatusCode.NoContent);
        }
    }
}
