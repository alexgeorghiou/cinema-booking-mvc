using System;
using System.Collections.Generic;

namespace Cinema.Core.Domain
{
    public class Movie
    {
        public Movie()
        {
            Screenings = new List<Screening>();
            IsActive = true;
        }

        public int Id { get; set; }

        public string Title { get; set; }

        public string Synopsis { get; set; }

        public int DurationMinutes { get; set; }

        public AgeRating Rating { get; set; }

        public string PosterUrl { get; set; }

        public DateTime? ReleaseDate { get; set; }

        /// <summary>Inactive titles stay in history but cannot be scheduled.</summary>
        public bool IsActive { get; set; }

        public virtual ICollection<Screening> Screenings { get; set; }

        public TimeSpan Duration
        {
            get { return TimeSpan.FromMinutes(DurationMinutes); }
        }
    }
}
