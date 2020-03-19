using System;
using System.ComponentModel.DataAnnotations;
using Cinema.Core.Services;

namespace Cinema.Web.Models
{
    public class CheckoutForm
    {
        [Required]
        public Guid HoldId { get; set; }

        [Required(ErrorMessage = "We need an email address to send your e-ticket.")]
        [EmailAddress]
        [StringLength(256)]
        [Display(Name = "Email for your e-ticket")]
        public string Email { get; set; }

        [Range(typeof(bool), "true", "true", ErrorMessage = "Please accept the booking terms.")]
        [Display(Name = "I accept the booking terms")]
        public bool AcceptTerms { get; set; }
    }

    public class CheckoutViewModel
    {
        public BookingQuote Quote { get; set; }

        public CheckoutForm Form { get; set; }
    }
}
