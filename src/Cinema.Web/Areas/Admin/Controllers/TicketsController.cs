using System.Web.Mvc;
using Cinema.Core.Services;

namespace Cinema.Web.Areas.Admin.Controllers
{
    /// <summary>
    /// Door check-in. Hand-held scanners act as keyboards, so the page is just a focused text box
    /// that submits the decoded QR payload.
    /// </summary>
    public class TicketsController : AdminControllerBase
    {
        private readonly ITicketService _tickets;

        public TicketsController(ITicketService tickets)
        {
            _tickets = tickets;
        }

        public ActionResult Scan()
        {
            ViewBag.Title = "Door check-in";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Scan(string payload)
        {
            ViewBag.Title = "Door check-in";
            return View(_tickets.CheckIn(payload));
        }
    }
}
