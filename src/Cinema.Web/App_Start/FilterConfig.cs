using System.Web.Mvc;
using Cinema.Web.Infrastructure;

namespace Cinema.Web
{
    public static class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new DomainExceptionFilter());
            filters.Add(new HandleErrorAttribute());
        }
    }
}
