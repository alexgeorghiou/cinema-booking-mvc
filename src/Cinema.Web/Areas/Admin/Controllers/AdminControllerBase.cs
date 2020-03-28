using System.Web.Mvc;
using Cinema.Core.Identity;

namespace Cinema.Web.Areas.Admin.Controllers
{
    [Authorize(Roles = Roles.Admin)]
    public abstract class AdminControllerBase : Controller
    {
        protected void Success(string message)
        {
            TempData[Infrastructure.Flash.SuccessKey] = message;
        }
    }
}
