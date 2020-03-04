using System.Web.Mvc;
using Cinema.Web.Infrastructure;

namespace Cinema.Web
{
    public static class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            // Exception filters run highest Order first, so domain errors are translated before HandleError sees them.
            filters.Add(new DomainExceptionFilter(), 1);
            filters.Add(new HandleErrorAttribute());
        }
    }
}
