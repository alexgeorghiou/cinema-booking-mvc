using System;

namespace Cinema.Web.Models
{
    public class HoldRequest
    {
        public int[] SeatIds { get; set; }
    }

    public class ReleaseRequest
    {
        public Guid HoldId { get; set; }
    }
}
