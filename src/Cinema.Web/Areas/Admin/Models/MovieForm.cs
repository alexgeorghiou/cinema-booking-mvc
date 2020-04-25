using System;
using System.ComponentModel.DataAnnotations;
using Cinema.Core.Domain;

namespace Cinema.Web.Areas.Admin.Models
{
    public class MovieForm
    {
        public int? Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [StringLength(2000)]
        [DataType(DataType.MultilineText)]
        public string Synopsis { get; set; }

        [Range(1, 300)]
        [Display(Name = "Duration (minutes)")]
        public int DurationMinutes { get; set; }

        [Display(Name = "Age rating")]
        public AgeRating Rating { get; set; }

        [Url]
        [StringLength(500)]
        [Display(Name = "Poster URL")]
        public string PosterUrl { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Release date")]
        public DateTime? ReleaseDate { get; set; }

        public static MovieForm From(Movie m)
        {
            return new MovieForm
            {
                Id = m.Id,
                Title = m.Title,
                Synopsis = m.Synopsis,
                DurationMinutes = m.DurationMinutes,
                Rating = m.Rating,
                PosterUrl = m.PosterUrl,
                ReleaseDate = m.ReleaseDate
            };
        }

        public void ApplyTo(Movie m)
        {
            m.Title = Title.Trim();
            m.Synopsis = Synopsis;
            m.DurationMinutes = DurationMinutes;
            m.Rating = Rating;
            m.PosterUrl = PosterUrl;
            m.ReleaseDate = ReleaseDate;
        }
    }
}
