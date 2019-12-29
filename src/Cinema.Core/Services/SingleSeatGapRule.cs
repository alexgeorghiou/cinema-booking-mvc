using System;
using System.Collections.Generic;
using System.Linq;
using Cinema.Core.Domain;

namespace Cinema.Core.Services
{
    /// <summary>
    /// Box-office rule: a selection must not strand a single empty seat between two occupied
    /// seats in the same row, because such seats are almost impossible to sell. Only gaps that
    /// the new selection creates are reported – pre-existing gaps are not the customer's fault.
    /// </summary>
    public static class SingleSeatGapRule
    {
        public static IReadOnlyList<int> FindStrandedSeats(
            IEnumerable<Seat> hallSeats, IReadOnlyCollection<int> selection, Func<int, bool> isOccupied)
        {
            var selected = new HashSet<int>(selection);
            var stranded = new List<int>();

            foreach (var row in hallSeats.GroupBy(s => s.RowIndex))
            {
                var byNumber = row.ToDictionary(s => s.Number);
                foreach (var seat in row)
                {
                    if (selected.Contains(seat.Id) || isOccupied(seat.Id))
                    {
                        continue;
                    }

                    Seat left, right;
                    if (!byNumber.TryGetValue(seat.Number - 1, out left) || !byNumber.TryGetValue(seat.Number + 1, out right))
                    {
                        continue;
                    }

                    var leftTaken = selected.Contains(left.Id) || isOccupied(left.Id);
                    var rightTaken = selected.Contains(right.Id) || isOccupied(right.Id);
                    var causedBySelection = selected.Contains(left.Id) || selected.Contains(right.Id);

                    if (leftTaken && rightTaken && causedBySelection)
                    {
                        stranded.Add(seat.Id);
                    }
                }
            }

            return stranded;
        }
    }
}
