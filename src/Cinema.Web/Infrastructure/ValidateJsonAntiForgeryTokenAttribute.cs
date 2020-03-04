using System;
using System.Web.Helpers;
using System.Web.Mvc;

namespace Cinema.Web.Infrastructure
{
    /// <summary>
    /// Anti-forgery validation for AJAX posts: the form token travels in the
    /// <c>RequestVerificationToken</c> header instead of the form body.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class ValidateJsonAntiForgeryTokenAttribute : FilterAttribute, IAuthorizationFilter
    {
        public const string HeaderName = "RequestVerificationToken";

        public void OnAuthorization(AuthorizationContext filterContext)
        {
            var request = filterContext.HttpContext.Request;
            var cookie = request.Cookies[AntiForgeryConfig.CookieName];
            var header = request.Headers[HeaderName];

            AntiForgery.Validate(cookie != null ? cookie.Value : null, header);
        }
    }
}
