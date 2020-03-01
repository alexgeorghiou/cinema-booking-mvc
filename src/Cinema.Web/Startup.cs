using System;
using System.Web.Mvc;
using Cinema.Core.Identity;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Owin;

[assembly: OwinStartup(typeof(Cinema.Web.Startup))]

namespace Cinema.Web
{
    /// <summary>OWIN pipeline: cookie authentication for ASP.NET Identity.</summary>
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // The user manager lives in the Autofac request scope so it shares the request's DbContext.
            app.CreatePerOwinContext(() => DependencyResolver.Current.GetService<ApplicationUserManager>());

            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
                LoginPath = new PathString("/account/login"),
                CookieName = "cinema.auth",
                CookieHttpOnly = true,
                SlidingExpiration = true,
                ExpireTimeSpan = TimeSpan.FromDays(7),
                Provider = new CookieAuthenticationProvider
                {
                    OnValidateIdentity = SecurityStampValidator.OnValidateIdentity<ApplicationUserManager, ApplicationUser>(
                        validateInterval: TimeSpan.FromMinutes(30),
                        regenerateIdentity: (manager, user) => user.GenerateUserIdentityAsync(manager, DefaultAuthenticationTypes.ApplicationCookie))
                }
            });
        }
    }
}
