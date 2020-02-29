using System.Data.Entity;
using System.Web.Mvc;
using System.Web.Routing;
using Cinema.Core.Data;
using Cinema.Core.Migrations;

namespace Cinema.Web
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            MvcHandler.DisableMvcResponseHeader = true;

            Database.SetInitializer(new MigrateDatabaseToLatestVersion<CinemaDbContext, Configuration>());

            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            var container = AutofacConfig.Configure();
            HoldSweeper.Start(container);
        }
    }
}
