namespace Cinema.Core.Domain
{
    public class Seat
    {
        public int Id { get; set; }

        public int HallId { get; set; }

        public virtual Hall Hall { get; set; }

        /// <summary>Row label as printed on the seat, e.g. "A", "B" ... "AA".</summary>
        public string Row { get; set; }

        /// <summary>Zero-based row position from the screen, used for ordering.</summary>
        public int RowIndex { get; set; }

        /// <summary>Seat number within the row, starting at 1 from the left aisle.</summary>
        public int Number { get; set; }

        public SeatType Type { get; set; }

        public string Label
        {
            get { return Row + Number; }
        }
    }
}
