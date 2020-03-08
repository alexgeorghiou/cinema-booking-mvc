using Cinema.Core.Domain;
using Cinema.Core.Services;

namespace Cinema.Web.Infrastructure
{
    public static class DisplayLabels
    {
        public static string Rating(AgeRating rating)
        {
            switch (rating)
            {
                case AgeRating.PG:
                    return "PG";
                case AgeRating.Twelve:
                    return "12A";
                case AgeRating.Fifteen:
                    return "15";
                case AgeRating.Eighteen:
                    return "18";
                default:
                    return "U";
            }
        }

        public static string Format(ScreeningFormat format)
        {
            switch (format)
            {
                case ScreeningFormat.ThreeD:
                    return "3D";
                case ScreeningFormat.Imax:
                    return "IMAX";
                default:
                    return "2D";
            }
        }

        public static string Outcome(TicketCheckOutcome outcome)
        {
            switch (outcome)
            {
                case TicketCheckOutcome.Valid:
                    return "Valid - admit";
                case TicketCheckOutcome.BadSignature:
                    return "Forged or altered code";
                case TicketCheckOutcome.UnknownBooking:
                    return "Booking not found";
                case TicketCheckOutcome.Cancelled:
                    return "Booking was cancelled";
                case TicketCheckOutcome.AlreadyUsed:
                    return "Ticket already used";
                case TicketCheckOutcome.WrongDay:
                    return "Not valid for this showing time";
                default:
                    return "Unreadable code";
            }
        }
    }
}
