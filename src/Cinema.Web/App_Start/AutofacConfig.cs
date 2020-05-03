using System.Web;
using System.Web.Mvc;
using Autofac;
using Autofac.Integration.Mvc;
using Cinema.Core.Abstractions;
using Cinema.Core.Data;
using Cinema.Core.Identity;
using Cinema.Core.Services;
using Cinema.Web.Infrastructure;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Owin.Security;

namespace Cinema.Web
{
    /// <summary>Composition root. One DbContext (and unit of work) per HTTP request.</summary>
    public static class AutofacConfig
    {
        public static IContainer Configure()
        {
            var builder = new ContainerBuilder();

            builder.RegisterControllers(typeof(MvcApplication).Assembly);
            builder.RegisterFilterProvider();
            builder.RegisterModelBinderProvider();
            builder.RegisterModule<AutofacWebTypesModule>();

            // Singletons
            builder.RegisterInstance(CinemaOptionsFactory.FromAppSettings()).AsSelf().SingleInstance();
            builder.RegisterType<SystemClock>().As<IClock>().SingleInstance();
            builder.RegisterType<PriceCalculator>().As<IPriceCalculator>().SingleInstance();
            builder.RegisterType<BookingReferenceGenerator>().As<IBookingReferenceGenerator>().SingleInstance();
            builder.RegisterType<HallLayoutBuilder>().AsSelf().SingleInstance();

            // Persistence (per request)
            var connection = ConnectionStrings.Cinema();
            builder.Register(c => new CinemaDbContext(connection)).AsSelf().InstancePerRequest();
            builder.RegisterType<EfUnitOfWork>().As<IUnitOfWork>().InstancePerRequest();
            builder.RegisterType<EfMovieRepository>().As<IMovieRepository>().InstancePerRequest();
            builder.RegisterType<EfHallRepository>().As<IHallRepository>().InstancePerRequest();
            builder.RegisterType<EfScreeningRepository>().As<IScreeningRepository>().InstancePerRequest();
            builder.RegisterType<EfSeatHoldRepository>().As<ISeatHoldRepository>().InstancePerRequest();
            builder.RegisterType<EfBookingRepository>().As<IBookingRepository>().InstancePerRequest();

            // Application services
            builder.RegisterType<SeatMapService>().As<ISeatMapService>().InstancePerRequest();
            builder.RegisterType<SeatHoldService>().As<ISeatHoldService>().InstancePerRequest();
            builder.RegisterType<BookingService>().As<IBookingService>().InstancePerRequest();
            builder.RegisterType<TicketService>().As<ITicketService>().InstancePerRequest();
            builder.RegisterType<ScreeningScheduler>().As<IScreeningScheduler>().InstancePerRequest();

            // Identity
            builder.Register(c => new UserStore<ApplicationUser>(c.Resolve<CinemaDbContext>()))
                .As<IUserStore<ApplicationUser>>().InstancePerRequest();
            builder.RegisterType<ApplicationUserManager>().AsSelf().InstancePerRequest();
            builder.Register(c => c.Resolve<HttpContextBase>().GetOwinContext().Authentication)
                .As<IAuthenticationManager>().InstancePerRequest();
            builder.RegisterType<ApplicationSignInManager>().AsSelf().InstancePerRequest();

            var container = builder.Build();
            DependencyResolver.SetResolver(new AutofacDependencyResolver(container));
            return container;
        }
    }
}
