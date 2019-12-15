using System;
using System.Collections.Generic;
using System.Linq;

namespace Cinema.Core.Domain
{
    /// <summary>
    /// A short-lived reservation of seats while the customer completes checkout.
    /// </summary>
    public class SeatHold
    {
        public SeatHold()
        {
            Items = new List<SeatHoldItem>();
        }

        public Guid Id { get; set; }

        public int ScreeningId { get; set; }

        public virtual Screening Screening { get; set; }

        /// <summary>Anonymous session token or user id that owns the hold.</summary>
        public string HolderId { get; set; }

        public DateTime CreatedUtc { get; set; }

        public DateTime ExpiresUtc { get; set; }

        public SeatHoldStatus Status { get; set; }

        public virtual ICollection<SeatHoldItem> Items { get; set; }

        public IEnumerable<int> SeatIds
        {
            get { return Items.Select(i => i.SeatId); }
        }

        public bool IsLive(DateTime nowUtc)
        {
            return Status == SeatHoldStatus.Active && ExpiresUtc > nowUtc;
        }

        public TimeSpan Remaining(DateTime nowUtc)
        {
            var left = ExpiresUtc - nowUtc;
            return left < TimeSpan.Zero ? TimeSpan.Zero : left;
        }
    }

    public class SeatHoldItem
    {
        public int Id { get; set; }

        public Guid SeatHoldId { get; set; }

        public virtual SeatHold SeatHold { get; set; }

        public int SeatId { get; set; }

        public virtual Seat Seat { get; set; }
    }
}
