using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.Infrastructure.Annotations;
using System.Data.Entity.ModelConfiguration.Configuration;
using Cinema.Core.Domain;
using Cinema.Core.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace Cinema.Core.Data
{
    public class CinemaDbContext : IdentityDbContext<ApplicationUser>
    {
        public const string ConnectionStringName = "CinemaDb";

        public CinemaDbContext()
            : this("name=" + ConnectionStringName)
        {
        }

        public CinemaDbContext(string nameOrConnectionString)
            : base(nameOrConnectionString, throwIfV1Schema: false)
        {
        }

        public virtual IDbSet<Movie> Movies { get; set; }

        public virtual IDbSet<Hall> Halls { get; set; }

        public virtual IDbSet<Seat> Seats { get; set; }

        public virtual IDbSet<Screening> Screenings { get; set; }

        public virtual IDbSet<SeatHold> SeatHolds { get; set; }

        public virtual IDbSet<SeatHoldItem> SeatHoldItems { get; set; }

        public virtual IDbSet<Booking> Bookings { get; set; }

        public virtual IDbSet<BookedSeat> BookedSeats { get; set; }

        public static CinemaDbContext Create()
        {
            return new CinemaDbContext();
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Properties<System.DateTime>().Configure(c => c.HasColumnType("datetime2"));

            var movie = modelBuilder.Entity<Movie>();
            movie.Property(m => m.Title).IsRequired().HasMaxLength(200);
            movie.Property(m => m.Synopsis).HasMaxLength(2000);
            movie.Property(m => m.PosterUrl).HasMaxLength(500);
            movie.Ignore(m => m.Duration);

            var hall = modelBuilder.Entity<Hall>();
            hall.Property(h => h.Name).IsRequired().HasMaxLength(100);
            Unique(hall.Property(h => h.Name), "UX_Hall_Name", 1);
            hall.Ignore(h => h.Capacity);

            var seat = modelBuilder.Entity<Seat>();
            seat.Property(s => s.Row).IsRequired().HasMaxLength(3);
            Unique(seat.Property(s => s.HallId), "UX_Seat_Position", 1);
            Unique(seat.Property(s => s.RowIndex), "UX_Seat_Position", 2);
            Unique(seat.Property(s => s.Number), "UX_Seat_Position", 3);
            seat.Ignore(s => s.Label);
            seat.HasRequired(s => s.Hall).WithMany(h => h.Seats).HasForeignKey(s => s.HallId).WillCascadeOnDelete(true);

            var screening = modelBuilder.Entity<Screening>();
            screening.Property(s => s.BasePrice).HasPrecision(8, 2);
            screening.Property(s => s.SeatVersion).IsConcurrencyToken();
            Index(screening.Property(s => s.HallId), "IX_Screening_Hall_Start", 1);
            Index(screening.Property(s => s.StartsAtUtc), "IX_Screening_Hall_Start", 2);
            screening.Ignore(s => s.EndsAtUtc);
            screening.HasRequired(s => s.Movie).WithMany(m => m.Screenings).HasForeignKey(s => s.MovieId).WillCascadeOnDelete(false);
            screening.HasRequired(s => s.Hall).WithMany().HasForeignKey(s => s.HallId).WillCascadeOnDelete(false);

            var hold = modelBuilder.Entity<SeatHold>();
            hold.HasKey(h => h.Id);
            hold.Property(h => h.Id).HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            hold.Property(h => h.HolderId).IsRequired().HasMaxLength(128);
            Index(hold.Property(h => h.ScreeningId), "IX_SeatHold_Screening_Status", 1);
            Index(hold.Property(h => h.Status), "IX_SeatHold_Screening_Status", 2);
            Index(hold.Property(h => h.ExpiresUtc), "IX_SeatHold_Expires", 1);
            hold.Ignore(h => h.SeatIds);
            hold.HasRequired(h => h.Screening).WithMany(s => s.Holds).HasForeignKey(h => h.ScreeningId).WillCascadeOnDelete(true);

            var holdItem = modelBuilder.Entity<SeatHoldItem>();
            holdItem.HasRequired(i => i.SeatHold).WithMany(h => h.Items).HasForeignKey(i => i.SeatHoldId).WillCascadeOnDelete(true);
            holdItem.HasRequired(i => i.Seat).WithMany().HasForeignKey(i => i.SeatId).WillCascadeOnDelete(false);

            var booking = modelBuilder.Entity<Booking>();
            booking.Property(b => b.Reference).IsRequired().HasMaxLength(8).IsFixedLength().IsUnicode(false);
            Unique(booking.Property(b => b.Reference), "UX_Booking_Reference", 1);
            booking.Property(b => b.UserId).HasMaxLength(128);
            booking.Property(b => b.CustomerEmail).IsRequired().HasMaxLength(256);
            booking.Property(b => b.ServiceFee).HasPrecision(8, 2);
            booking.Property(b => b.Total).HasPrecision(10, 2);
            Index(booking.Property(b => b.UserId), "IX_Booking_User", 1);
            booking.Ignore(b => b.SeatLabels);
            booking.HasRequired(b => b.Screening).WithMany(s => s.Bookings).HasForeignKey(b => b.ScreeningId).WillCascadeOnDelete(false);

            var bookedSeat = modelBuilder.Entity<BookedSeat>();
            bookedSeat.Property(s => s.Price).HasPrecision(8, 2);
            // The filtered unique index (ScreeningId, SeatId) WHERE IsActive = 1 is created by
            // Migrations.Configuration.Seed because EF6 cannot express index filters.
            Index(bookedSeat.Property(s => s.ScreeningId), "IX_BookedSeat_Screening", 1);
            bookedSeat.HasRequired(s => s.Booking).WithMany(b => b.Seats).HasForeignKey(s => s.BookingId).WillCascadeOnDelete(true);
            bookedSeat.HasRequired(s => s.Seat).WithMany().HasForeignKey(s => s.SeatId).WillCascadeOnDelete(false);
        }

        private static void Index(PrimitivePropertyConfiguration property, string name, int order)
        {
            property.HasColumnAnnotation(IndexAnnotation.AnnotationName,
                new IndexAnnotation(new IndexAttribute(name, order)));
        }

        private static void Unique(PrimitivePropertyConfiguration property, string name, int order)
        {
            property.HasColumnAnnotation(IndexAnnotation.AnnotationName,
                new IndexAnnotation(new IndexAttribute(name, order) { IsUnique = true }));
        }
    }
}
