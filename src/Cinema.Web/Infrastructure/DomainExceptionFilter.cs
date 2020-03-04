using System.Net;
using System.Web.Mvc;
using Cinema.Core.Abstractions;

namespace Cinema.Web.Infrastructure
{
    /// <summary>
    /// Translates <see cref="CinemaDomainException"/> into friendly responses: JSON with a status
    /// code for AJAX calls from the seat map, or a flash message and redirect for page posts.
    /// </summary>
    public class DomainExceptionFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            var ex = context.Exception as CinemaDomainException;
            if (ex == null || context.ExceptionHandled)
            {
                return;
            }

            var status = StatusFor(ex);
            var request = context.HttpContext.Request;

            if (request.IsAjaxRequest() || (request.AcceptTypes != null && System.Array.IndexOf(request.AcceptTypes, "application/json") >= 0))
            {
                var unavailable = ex as SeatUnavailableException;
                context.Result = new JsonResult
                {
                    Data = new { error = ex.Message, seatIds = unavailable != null ? unavailable.SeatIds : null },
                    JsonRequestBehavior = JsonRequestBehavior.AllowGet
                };
            }
            else if (status == HttpStatusCode.NotFound)
            {
                context.Result = new HttpNotFoundResult(ex.Message);
            }
            else
            {
                context.Controller.TempData[Flash.ErrorKey] = ex.Message;
                var back = request.UrlReferrer != null ? request.UrlReferrer.PathAndQuery : "/";
                context.Result = new RedirectResult(back);
                status = HttpStatusCode.Redirect;
            }

            context.ExceptionHandled = true;
            context.HttpContext.Response.Clear();
            context.HttpContext.Response.TrySkipIisCustomErrors = true;
            context.HttpContext.Response.StatusCode = (int)status;
        }

        private static HttpStatusCode StatusFor(CinemaDomainException ex)
        {
            if (ex is NotFoundException)
            {
                return HttpStatusCode.NotFound;
            }

            if (ex is SeatUnavailableException || ex is ConcurrencyConflictException || ex is HoldExpiredException)
            {
                return HttpStatusCode.Conflict;
            }

            return HttpStatusCode.BadRequest;
        }
    }

    public static class Flash
    {
        public const string ErrorKey = "flash:error";
        public const string SuccessKey = "flash:success";
    }
}
