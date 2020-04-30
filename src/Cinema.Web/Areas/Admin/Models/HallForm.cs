using System.ComponentModel.DataAnnotations;

namespace Cinema.Web.Areas.Admin.Models
{
    public class HallForm
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Range(1, 40)]
        public int Rows { get; set; }

        [Range(1, 60)]
        [Display(Name = "Seats per row")]
        public int SeatsPerRow { get; set; }

        [Range(0, 40)]
        [Display(Name = "Premium rows (from the back)")]
        public int PremiumRows { get; set; }

        [StringLength(500)]
        [Display(Name = "Wheelchair spaces", Description = "Comma separated, e.g. A1, A2")]
        public string AccessibleSeats { get; set; }
    }
}
