using System;
using System.Collections.Generic;

namespace Cinema.Core.Domain
{
    public class Screening
    {
        public Screening()
        {
            Holds = new List<SeatHold>();
            Bookings = new List<Booking>();
        }

        public int Id { get; set; }

        public int MovieId { get; set; }

        public virtual Movie Movie { get; set; }

        public int HallId { get; set; }

        public virtual Hall Hall { get; set; }

        public DateTime StartsAtUtc { get; set; }

        /// <summary>Price of a standard seat before format surcharges and discounts.</summary>
        public decimal BasePrice { get; set; }

        public ScreeningFormat Format { get; set; }

        /// <summary>
        /// Incremented every time seat availability changes (hold, release, booking).
        /// Mapped as an optimistic concurrency token so two requests racing for seats
        /// in the same screening cannot both commit.
        /// </summary>
        public int SeatVersion { get; set; }

        public virtual ICollection<SeatHold> Holds { get; set; }

        public virtual ICollection<Booking> Bookings { get; set; }

        public DateTime EndsAtUtc
        {
            get { return Movie == null ? StartsAtUtc : StartsAtUtc.Add(Movie.Duration); }
        }

        public void TouchSeats()
        {
            SeatVersion++;
        }
    }
}
