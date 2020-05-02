using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using Cinema.Core.Domain;

namespace Cinema.Web.Areas.Admin.Models
{
    public class ScreeningForm
    {
        [Required]
        [Display(Name = "Movie")]
        public int MovieId { get; set; }

        [Required]
        [Display(Name = "Hall")]
        public int HallId { get; set; }

        /// <summary>Venue-local start time; converted to UTC before scheduling.</summary>
        [Required]
        [Display(Name = "Starts (venue time)")]
        public DateTime? StartsAtLocal { get; set; }

        [Range(typeof(decimal), "0.50", "100")]
        [Display(Name = "Base price")]
        public decimal BasePrice { get; set; }

        public ScreeningFormat Format { get; set; }

        public IEnumerable<SelectListItem> Movies { get; set; }

        public IEnumerable<SelectListItem> Halls { get; set; }
    }

    public class ScreeningBookingsViewModel
    {
        public Screening Screening { get; set; }

        public IReadOnlyList<Booking> Bookings { get; set; }
    }
}
