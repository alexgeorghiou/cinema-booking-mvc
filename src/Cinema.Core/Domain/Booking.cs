using System;
using System.Collections.Generic;
using System.Linq;

namespace Cinema.Core.Domain
{
    public class Booking
    {
        public Booking()
        {
            Seats = new List<BookedSeat>();
        }

        public int Id { get; set; }

        /// <summary>Human friendly code printed on the ticket, e.g. "K7M2QX9D".</summary>
        public string Reference { get; set; }

        public int ScreeningId { get; set; }

        public virtual Screening Screening { get; set; }

        public string UserId { get; set; }

        public string CustomerEmail { get; set; }

        public DateTime CreatedUtc { get; set; }

        public DateTime? CancelledUtc { get; set; }

        public DateTime? CheckedInUtc { get; set; }

        public BookingStatus Status { get; set; }

        public decimal ServiceFee { get; set; }

        public decimal Total { get; set; }

        public virtual ICollection<BookedSeat> Seats { get; set; }

        public string SeatLabels
        {
            get { return string.Join(", ", Seats.Where(s => s.Seat != null).Select(s => s.Seat.Label)); }
        }
    }

    /// <summary>
    /// One seat in a booking. <see cref="ScreeningId"/> is denormalised so the database can
    /// enforce a unique (ScreeningId, SeatId) index – the last line of defence against
    /// double-selling a seat.
    /// </summary>
    public class BookedSeat
    {
        public BookedSeat()
        {
            IsActive = true;
        }

        public int Id { get; set; }

        public int BookingId { get; set; }

        public virtual Booking Booking { get; set; }

        public int ScreeningId { get; set; }

        public int SeatId { get; set; }

        public virtual Seat Seat { get; set; }

        public decimal Price { get; set; }

        /// <summary>
        /// False once the booking is cancelled. The unique index is filtered on this flag so a
        /// cancelled seat can be sold again while the booking keeps its history.
        /// </summary>
        public bool IsActive { get; set; }
    }
}
