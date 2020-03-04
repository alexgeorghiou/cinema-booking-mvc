using System;
using System.Web;

namespace Cinema.Web.Infrastructure
{
    /// <summary>
    /// Seat holds belong to a browser, not to an account, so guests can pick seats and the
    /// hold survives signing in halfway through checkout. The id is an opaque random token.
    /// </summary>
    public static class HolderCookie
    {
        public const string Name = "cinema.holder";

        public static string GetOrCreate(HttpContextBase http)
        {
            var existing = http.Request.Cookies[Name];
            Guid parsed;
            if (existing != null && Guid.TryParseExact(existing.Value, "N", out parsed))
            {
                return existing.Value;
            }

            var value = Guid.NewGuid().ToString("N");
            http.Response.Cookies.Add(new HttpCookie(Name, value)
            {
                HttpOnly = true,
                Secure = http.Request.IsSecureConnection,
                Expires = DateTime.UtcNow.AddDays(30)
            });

            // Make it visible to the rest of this request too.
            http.Request.Cookies.Set(new HttpCookie(Name, value));
            return value;
        }
    }
}
