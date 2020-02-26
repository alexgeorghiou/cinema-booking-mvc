using System;
using System.Configuration;
using System.Data.Entity.Migrations;
using System.Linq;
using Cinema.Core.Data;
using Cinema.Core.Domain;
using Cinema.Core.Identity;
using Cinema.Core.Services;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace Cinema.Core.Migrations
{
    /// <summary>
    /// Automatic migrations keep the demo self-contained (the database is created and evolved
    /// on first request). Seed is idempotent and runs after every migration.
    /// </summary>
    public sealed class Configuration : DbMigrationsConfiguration<CinemaDbContext>
    {
        public const string LiveSeatIndexSql = @"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_BookedSeat_Live')
    CREATE UNIQUE INDEX UX_BookedSeat_Live ON dbo.BookedSeats (ScreeningId, SeatId) WHERE IsActive = 1;";

        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
            AutomaticMigrationDataLossAllowed = false;
            ContextKey = "Cinema.Core.Data.CinemaDbContext";
        }

        protected override void Seed(CinemaDbContext db)
        {
            db.Database.ExecuteSqlCommand(LiveSeatIndexSql);

            SeedRoles(db);
            SeedAdmin(db);

            if (db.Movies.Any())
            {
                return;
            }

            var layout = new HallLayoutBuilder();
            var screen1 = layout.Build("Screen 1", 10, 14, 2, new[] { "A1", "A2", "A13", "A14" });
            var screen2 = layout.Build("Screen 2 (IMAX)", 12, 18, 3, new[] { "A1", "A18" });
            db.Halls.Add(screen1);
            db.Halls.Add(screen2);

            var movies = new[]
            {
                new Movie { Title = "Signal Over Ganymede", DurationMinutes = 128, Rating = AgeRating.Twelve, Synopsis = "A relay engineer on Jupiter's largest moon picks up a transmission that should not exist." },
                new Movie { Title = "The Lighthouse Keeper's Cat", DurationMinutes = 94, Rating = AgeRating.U, Synopsis = "An animated adventure along a stormy coastline." },
                new Movie { Title = "Midnight in Lisbon", DurationMinutes = 112, Rating = AgeRating.Fifteen, Synopsis = "Two strangers, one missed tram and a city that never quite sleeps." }
            };
            foreach (var m in movies)
            {
                db.Movies.Add(m);
            }

            db.SaveChanges();

            var today = DateTime.UtcNow.Date;
            for (var day = 0; day < 7; day++)
            {
                var date = today.AddDays(day);
                AddScreening(db, movies[1], screen1, date.AddHours(11), 7.50m, ScreeningFormat.TwoD);
                AddScreening(db, movies[2], screen1, date.AddHours(14), 9.00m, ScreeningFormat.TwoD);
                AddScreening(db, movies[0], screen1, date.AddHours(18), 10.50m, ScreeningFormat.ThreeD);
                AddScreening(db, movies[0], screen2, date.AddHours(13), 12.00m, ScreeningFormat.Imax);
                AddScreening(db, movies[0], screen2, date.AddHours(20), 13.50m, ScreeningFormat.Imax);
            }

            db.SaveChanges();
        }

        private static void AddScreening(CinemaDbContext db, Movie movie, Hall hall, DateTime startUtc, decimal price, ScreeningFormat format)
        {
            if (startUtc <= DateTime.UtcNow)
            {
                return;
            }

            db.Screenings.Add(new Screening { Movie = movie, Hall = hall, StartsAtUtc = startUtc, BasePrice = price, Format = format });
        }

        private static void SeedRoles(CinemaDbContext db)
        {
            var roles = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(db));
            foreach (var name in new[] { Roles.Admin, Roles.BoxOffice })
            {
                if (!roles.RoleExists(name))
                {
                    roles.Create(new IdentityRole(name));
                }
            }
        }

        /// <summary>Creates an admin account only when credentials are supplied via appSettings.</summary>
        private static void SeedAdmin(CinemaDbContext db)
        {
            var email = ConfigurationManager.AppSettings["Seed:AdminEmail"];
            var password = ConfigurationManager.AppSettings["Seed:AdminPassword"];
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return;
            }

            var users = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));
            var admin = users.FindByEmail(email);
            if (admin == null)
            {
                admin = new ApplicationUser { UserName = email, Email = email, DisplayName = "Administrator", EmailConfirmed = true };
                var result = users.Create(admin, password);
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException("Could not seed admin: " + string.Join("; ", result.Errors));
                }
            }

            if (!users.IsInRole(admin.Id, Roles.Admin))
            {
                users.AddToRole(admin.Id, Roles.Admin);
            }
        }
    }
}
