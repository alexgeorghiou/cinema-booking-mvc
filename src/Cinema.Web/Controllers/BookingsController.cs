using System;
using System.Web.Mvc;
using System.Web.UI;
using Cinema.Core.Abstractions;
using Cinema.Core.Domain;
using Cinema.Core.Services;
using Cinema.Web.Infrastructure;
using Cinema.Web.Models;
using Microsoft.AspNet.Identity;

namespace Cinema.Web.Controllers
{
    [RoutePrefix("bookings")]
    public class BookingsController : Controller
    {
        private readonly IBookingService _bookings;
        private readonly ITicketService _tickets;
        private readonly CinemaOptions _options;

        public BookingsController(IBookingService bookings, ITicketService tickets, CinemaOptions options)
        {
            _bookings = bookings;
            _tickets = tickets;
            _options = options;
        }

        // GET /bookings/checkout?holdId=...
        [Route("checkout")]
        public ActionResult Checkout(Guid holdId)
        {
            var quote = _bookings.Quote(holdId, HolderCookie.GetOrCreate(HttpContext));
            var form = new CheckoutForm { HoldId = holdId };
            if (Request.IsAuthenticated)
            {
                form.Email = User.Identity.GetUserName();
            }

            ViewBag.Title = "Checkout";
            return View(new CheckoutViewModel { Quote = quote, Form = form });
        }

        [HttpPost]
        [Route("checkout")]
        [ValidateAntiForgeryToken]
        public ActionResult Checkout([Bind(Prefix = "Form")] CheckoutForm form)
        {
            var holder = HolderCookie.GetOrCreate(HttpContext);
            if (!ModelState.IsValid)
            {
                ViewBag.Title = "Checkout";
                return View(new CheckoutViewModel { Quote = _bookings.Quote(form.HoldId, holder), Form = form });
            }

            try
            {
                var userId = Request.IsAuthenticated ? User.Identity.GetUserId() : null;
                var booking = _bookings.Confirm(form.HoldId, holder, userId, form.Email);
                return RedirectToAction("Confirmation", new { reference = booking.Reference });
            }
            catch (HoldExpiredException ex)
            {
                TempData[Flash.ErrorKey] = ex.Message;
                return RedirectToAction("Index", "Home");
            }
        }

        // GET /bookings/K7M2QX9D
        [Route("{reference:length(8)}")]
        public ActionResult Confirmation(string reference)
        {
            var booking = _bookings.Find(reference);
            if (booking == null)
            {
                return HttpNotFound();
            }

            ViewBag.Title = "Booking " + booking.Reference;
            ViewBag.CanCancel = CanCancel(booking);
            return View(booking);
        }

        // GET /bookings/K7M2QX9D/ticket  – printable e-ticket
        [Route("{reference:length(8)}/ticket")]
        public ActionResult Ticket(string reference)
        {
            var booking = _bookings.Find(reference);
            if (booking == null || booking.Status == BookingStatus.Cancelled)
            {
                return HttpNotFound();
            }

            ViewBag.Title = "E-ticket " + booking.Reference;
            return View(booking);
        }

        // GET /bookings/K7M2QX9D/qr.png
        [Route("{reference:length(8)}/qr.png")]
        [OutputCache(Duration = 3600, Location = OutputCacheLocation.Client, VaryByParam = "reference")]
        public ActionResult Qr(string reference)
        {
            var booking = _bookings.Find(reference);
            if (booking == null || booking.Status == BookingStatus.Cancelled)
            {
                return HttpNotFound();
            }

            return File(_tickets.RenderQrPng(booking), "image/png");
        }

        [Authorize]
        [Route("mine")]
        public ActionResult Mine()
        {
            ViewBag.Title = "My bookings";
            return View(_bookings.ForUser(User.Identity.GetUserId()));
        }

        [Authorize]
        [HttpPost]
        [Route("{reference:length(8)}/cancel")]
        [ValidateAntiForgeryToken]
        public ActionResult Cancel(string reference)
        {
            _bookings.Cancel(reference, User.Identity.GetUserId());
            TempData[Flash.SuccessKey] = "Booking " + reference + " was cancelled and your seats released.";
            return RedirectToAction("Mine");
        }

        private bool CanCancel(Booking booking)
        {
            return Request.IsAuthenticated
                && booking.UserId == User.Identity.GetUserId()
                && booking.Status == BookingStatus.Confirmed
                && DateTime.UtcNow < booking.Screening.StartsAtUtc - _options.CancellationCutoff;
        }
    }
}
