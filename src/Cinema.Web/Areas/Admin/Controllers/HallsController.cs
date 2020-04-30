using System.Web.Mvc;
using Cinema.Core.Abstractions;
using Cinema.Core.Services;
using Cinema.Web.Areas.Admin.Models;

namespace Cinema.Web.Areas.Admin.Controllers
{
    public class HallsController : AdminControllerBase
    {
        private readonly IHallRepository _halls;
        private readonly HallLayoutBuilder _layout;
        private readonly IUnitOfWork _unitOfWork;

        public HallsController(IHallRepository halls, HallLayoutBuilder layout, IUnitOfWork unitOfWork)
        {
            _halls = halls;
            _layout = layout;
            _unitOfWork = unitOfWork;
        }

        public ActionResult Index()
        {
            ViewBag.Title = "Halls";
            return View(_halls.ListAll());
        }

        public ActionResult Details(int id)
        {
            var hall = _halls.Find(id);
            if (hall == null)
            {
                return HttpNotFound();
            }

            ViewBag.Title = hall.Name;
            return View(hall);
        }

        public ActionResult Create()
        {
            ViewBag.Title = "Add hall";
            return View(new HallForm { Rows = 10, SeatsPerRow = 14, PremiumRows = 2 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(HallForm form)
        {
            ViewBag.Title = "Add hall";
            if (!ModelState.IsValid)
            {
                return View(form);
            }

            try
            {
                var hall = _layout.Build(form.Name, form.Rows, form.SeatsPerRow, form.PremiumRows,
                    HallLayoutBuilder.ParseSeatList(form.AccessibleSeats));
                _halls.Add(hall);
                _unitOfWork.SaveChanges();
                Success(string.Format("Created {0} with {1} seats.", hall.Name, hall.Capacity));
                return RedirectToAction("Details", new { id = hall.Id });
            }
            catch (InvalidSeatSelectionException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(form);
            }
        }
    }
}
