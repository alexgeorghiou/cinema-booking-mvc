using System.Collections.Generic;
using System.Linq;

namespace Cinema.Core.Domain
{
    public class Hall
    {
        public Hall()
        {
            Seats = new List<Seat>();
        }

        public int Id { get; set; }

        public string Name { get; set; }

        public virtual ICollection<Seat> Seats { get; set; }

        public int Capacity
        {
            get { return Seats.Count; }
        }

        public IEnumerable<IGrouping<string, Seat>> Rows()
        {
            return Seats
                .OrderBy(s => s.RowIndex)
                .ThenBy(s => s.Number)
                .GroupBy(s => s.Row);
        }
    }
}
