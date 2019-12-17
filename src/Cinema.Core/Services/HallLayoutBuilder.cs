using System;
using System.Collections.Generic;
using System.Linq;
using Cinema.Core.Abstractions;
using Cinema.Core.Domain;

namespace Cinema.Core.Services
{
    /// <summary>
    /// Generates a rectangular seating plan. Rows are lettered from the screen backwards
    /// (A, B, ... Z, AA, AB ...). The rearmost rows can be marked premium, and individual
    /// seats can be flagged as wheelchair-accessible spaces.
    /// </summary>
    public class HallLayoutBuilder
    {
        public const int MaxRows = 40;
        public const int MaxSeatsPerRow = 60;

        public Hall Build(string name, int rows, int seatsPerRow, int premiumRows, IEnumerable<string> accessibleSeatLabels)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new InvalidSeatSelectionException("A hall needs a name.");
            }

            if (rows < 1 || rows > MaxRows)
            {
                throw new InvalidSeatSelectionException(string.Format("Rows must be between 1 and {0}.", MaxRows));
            }

            if (seatsPerRow < 1 || seatsPerRow > MaxSeatsPerRow)
            {
                throw new InvalidSeatSelectionException(string.Format("Seats per row must be between 1 and {0}.", MaxSeatsPerRow));
            }

            if (premiumRows < 0 || premiumRows > rows)
            {
                throw new InvalidSeatSelectionException("Premium rows cannot exceed the number of rows.");
            }

            var accessible = new HashSet<string>(
                (accessibleSeatLabels ?? Enumerable.Empty<string>())
                    .Select(l => l.Trim().ToUpperInvariant())
                    .Where(l => l.Length > 0),
                StringComparer.OrdinalIgnoreCase);

            var hall = new Hall { Name = name.Trim() };
            for (var r = 0; r < rows; r++)
            {
                var rowLabel = RowLabel(r);
                var isPremiumRow = r >= rows - premiumRows;
                for (var n = 1; n <= seatsPerRow; n++)
                {
                    var seat = new Seat
                    {
                        Row = rowLabel,
                        RowIndex = r,
                        Number = n,
                        Type = isPremiumRow ? SeatType.Premium : SeatType.Standard
                    };

                    if (accessible.Remove(seat.Label))
                    {
                        seat.Type = SeatType.Accessible;
                    }

                    hall.Seats.Add(seat);
                }
            }

            if (accessible.Count > 0)
            {
                throw new InvalidSeatSelectionException(
                    "Unknown accessible seat(s): " + string.Join(", ", accessible.OrderBy(a => a)));
            }

            return hall;
        }

        /// <summary>0 -> A, 25 -> Z, 26 -> AA, 27 -> AB ...</summary>
        public static string RowLabel(int index)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            var label = string.Empty;
            var i = index;
            do
            {
                label = (char)('A' + (i % 26)) + label;
                i = (i / 26) - 1;
            }
            while (i >= 0);

            return label;
        }

        public static IEnumerable<string> ParseSeatList(string csv)
        {
            if (string.IsNullOrWhiteSpace(csv))
            {
                return Enumerable.Empty<string>();
            }

            return csv.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
