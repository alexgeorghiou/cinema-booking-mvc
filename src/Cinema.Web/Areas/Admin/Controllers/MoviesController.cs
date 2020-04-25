using System.Web.Mvc;
using Cinema.Core.Abstractions;
using Cinema.Core.Domain;
using Cinema.Web.Areas.Admin.Models;

namespace Cinema.Web.Areas.Admin.Controllers
{
    public class MoviesController : AdminControllerBase
    {
        private readonly IMovieRepository _movies;
        private readonly IUnitOfWork _unitOfWork;

        public MoviesController(IMovieRepository movies, IUnitOfWork unitOfWork)
        {
            _movies = movies;
            _unitOfWork = unitOfWork;
        }

        public ActionResult Index()
        {
            ViewBag.Title = "Movies";
            return View(_movies.ListAll());
        }

        public ActionResult Create()
        {
            ViewBag.Title = "Add movie";
            return View("Edit", new MovieForm { DurationMinutes = 120 });
        }

        public ActionResult Edit(int id)
        {
            var movie = _movies.Find(id);
            if (movie == null)
            {
                return HttpNotFound();
            }

            ViewBag.Title = "Edit " + movie.Title;
            return View(MovieForm.From(movie));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Save(MovieForm form)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Title = form.Id.HasValue ? "Edit movie" : "Add movie";
                return View("Edit", form);
            }

            Movie movie;
            if (form.Id.HasValue)
            {
                movie = _movies.Find(form.Id.Value);
                if (movie == null)
                {
                    return HttpNotFound();
                }
            }
            else
            {
                movie = new Movie();
                _movies.Add(movie);
            }

            form.ApplyTo(movie);
            _unitOfWork.SaveChanges();
            Success("Saved '" + movie.Title + "'.");
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleActive(int id)
        {
            var movie = _movies.Find(id);
            if (movie == null)
            {
                return HttpNotFound();
            }

            movie.IsActive = !movie.IsActive;
            _unitOfWork.SaveChanges();
            Success(movie.IsActive ? "'" + movie.Title + "' is back on sale." : "'" + movie.Title + "' archived.");
            return RedirectToAction("Index");
        }
    }
}
