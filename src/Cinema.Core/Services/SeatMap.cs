using System;
using System.Collections.Generic;

namespace Cinema.Core.Services
{
    public enum SeatState
    {
        Available = 0,
        Held = 1,
        HeldByYou = 2,
        Sold = 3
    }

    public sealed class SeatMap
    {
        public SeatMap()
        {
            Rows = new List<SeatMapRow>();
        }

        public int ScreeningId { get; set; }

        public string MovieTitle { get; set; }

        public string HallName { get; set; }

        public DateTime StartsAtUtc { get; set; }

        public int SeatVersion { get; set; }

        public List<SeatMapRow> Rows { get; set; }

        public Guid? YourHoldId { get; set; }

        public DateTime? YourHoldExpiresUtc { get; set; }

        public int AvailableCount { get; set; }
    }

    public sealed class SeatMapRow
    {
        public SeatMapRow()
        {
            Seats = new List<SeatMapSeat>();
        }

        public string Row { get; set; }

        public List<SeatMapSeat> Seats { get; set; }
    }

    public sealed class SeatMapSeat
    {
        public int SeatId { get; set; }

        public string Label { get; set; }

        public int Number { get; set; }

        public string Type { get; set; }

        public SeatState State { get; set; }

        public decimal Price { get; set; }
    }
}
