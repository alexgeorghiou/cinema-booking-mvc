using System;
using System.Collections.Generic;
using Cinema.Core.Domain;

namespace Cinema.Web.Models
{
    public class ListingsViewModel
    {
        public ListingsViewModel()
        {
            Days = new List<DateTime>();
            Movies = new List<MovieListing>();
        }

        /// <summary>Selected venue-local date.</summary>
        public DateTime Date { get; set; }

        public List<DateTime> Days { get; set; }

        public List<MovieListing> Movies { get; set; }
    }

    public class MovieListing
    {
        public Movie Movie { get; set; }

        public List<Screening> Screenings { get; set; }
    }
}
