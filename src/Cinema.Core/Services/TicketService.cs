using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Cinema.Core.Abstractions;
using Cinema.Core.Domain;
using QRCoder;

namespace Cinema.Core.Services
{
    public interface ITicketService
    {
        /// <summary>The string encoded in the QR code.</summary>
        string BuildPayload(Booking booking);

        byte[] RenderQrPng(Booking booking, int pixelsPerModule = 8);

        TicketCheck Verify(string payload);

        /// <summary>Verifies the payload and marks the booking as checked in (door scanning).</summary>
        TicketCheck CheckIn(string payload);
    }

    public enum TicketCheckOutcome
    {
        Valid = 0,
        Malformed = 1,
        BadSignature = 2,
        UnknownBooking = 3,
        Cancelled = 4,
        AlreadyUsed = 5,
        WrongDay = 6
    }

    public sealed class TicketCheck
    {
        public TicketCheckOutcome Outcome { get; set; }

        public Booking Booking { get; set; }

        public bool IsValid
        {
            get { return Outcome == TicketCheckOutcome.Valid; }
        }
    }

    /// <summary>
    /// E-tickets carry a compact, HMAC-signed payload: <c>CNM1.{reference}.{screeningId}.{seatCount}.{signature}</c>.
    /// The door scanner can reject forged or altered codes before touching the database.
    /// </summary>
    public class TicketService : ITicketService
    {
        public const string Prefix = "CNM1";
        private const int SignatureBytes = 12;

        private readonly IBookingRepository _bookings;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClock _clock;
        private readonly CinemaOptions _options;

        public TicketService(IBookingRepository bookings, IUnitOfWork unitOfWork, IClock clock, CinemaOptions options)
        {
            _bookings = bookings;
            _unitOfWork = unitOfWork;
            _clock = clock;
            _options = options;
        }

        public string BuildPayload(Booking booking)
        {
            var body = string.Join(".",
                Prefix,
                booking.Reference,
                booking.ScreeningId.ToString(CultureInfo.InvariantCulture),
                booking.Seats.Count.ToString(CultureInfo.InvariantCulture));
            return body + "." + Sign(body);
        }

        public byte[] RenderQrPng(Booking booking, int pixelsPerModule = 8)
        {
            using (var generator = new QRCodeGenerator())
            using (var data = generator.CreateQrCode(BuildPayload(booking), QRCodeGenerator.ECCLevel.Q))
            {
                var png = new PngByteQRCode(data);
                return png.GetGraphic(pixelsPerModule);
            }
        }

        public TicketCheck Verify(string payload)
        {
            var parts = (payload ?? string.Empty).Trim().Split('.');
            int screeningId, seatCount;
            if (parts.Length != 5 || parts[0] != Prefix
                || !int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out screeningId)
                || !int.TryParse(parts[3], NumberStyles.None, CultureInfo.InvariantCulture, out seatCount))
            {
                return new TicketCheck { Outcome = TicketCheckOutcome.Malformed };
            }

            var body = string.Join(".", parts.Take(4));
            if (!FixedTimeEquals(Sign(body), parts[4]))
            {
                return new TicketCheck { Outcome = TicketCheckOutcome.BadSignature };
            }

            var booking = _bookings.FindByReference(parts[1]);
            if (booking == null || booking.ScreeningId != screeningId)
            {
                return new TicketCheck { Outcome = TicketCheckOutcome.UnknownBooking };
            }

            var check = new TicketCheck { Booking = booking };
            if (booking.Status == BookingStatus.Cancelled)
            {
                check.Outcome = TicketCheckOutcome.Cancelled;
            }
            else if (booking.Status == BookingStatus.CheckedIn)
            {
                check.Outcome = TicketCheckOutcome.AlreadyUsed;
            }
            else if (booking.Screening != null && !IsAdmissionWindow(booking.Screening))
            {
                check.Outcome = TicketCheckOutcome.WrongDay;
            }
            else
            {
                check.Outcome = TicketCheckOutcome.Valid;
            }

            return check;
        }

        public TicketCheck CheckIn(string payload)
        {
            var check = Verify(payload);
            if (check.IsValid)
            {
                check.Booking.Status = BookingStatus.CheckedIn;
                check.Booking.CheckedInUtc = _clock.UtcNow;
                _unitOfWork.SaveChanges();
            }

            return check;
        }

        /// <summary>Doors open an hour before; late entry allowed until the film ends.</summary>
        private bool IsAdmissionWindow(Screening screening)
        {
            var now = _clock.UtcNow;
            return now >= screening.StartsAtUtc.AddHours(-1) && now <= screening.EndsAtUtc;
        }

        private string Sign(string body)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.TicketSigningKey)))
            {
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
                return Base64Url(hash.Take(SignatureBytes).ToArray());
            }
        }

        private static string Base64Url(byte[] bytes)
        {
            return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        private static bool FixedTimeEquals(string a, string b)
        {
            if (a == null || b == null || a.Length != b.Length)
            {
                return false;
            }

            var diff = 0;
            for (var i = 0; i < a.Length; i++)
            {
                diff |= a[i] ^ b[i];
            }

            return diff == 0;
        }
    }
}
