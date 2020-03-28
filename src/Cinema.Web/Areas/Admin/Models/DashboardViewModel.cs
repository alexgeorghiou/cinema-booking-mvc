using System.Collections.Generic;
using Cinema.Core.Domain;

namespace Cinema.Web.Areas.Admin.Models
{
    public class DashboardViewModel
    {
        public DashboardViewModel()
        {
            Upcoming = new List<ScreeningOccupancy>();
        }

        public List<ScreeningOccupancy> Upcoming { get; set; }
    }

    public class ScreeningOccupancy
    {
        public Screening Screening { get; set; }

        public int Sold { get; set; }

        public int Capacity { get; set; }

        public decimal Revenue { get; set; }

        public int Percent
        {
            get { return Capacity == 0 ? 0 : (int)System.Math.Round(100.0 * Sold / Capacity); }
        }
    }
}
