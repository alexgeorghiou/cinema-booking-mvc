using System.Security.Cryptography;
using System.Text;

namespace Cinema.Core.Services
{
    public interface IBookingReferenceGenerator
    {
        string Next();
    }

    /// <summary>
    /// 8-character references from an alphabet without look-alike characters (no 0/O, 1/I),
    /// so they can be read out over the counter. 32^8 ~ 1.1e12 combinations.
    /// </summary>
    public class BookingReferenceGenerator : IBookingReferenceGenerator
    {
        public const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        public const int Length = 8;

        public string Next()
        {
            var bytes = new byte[Length];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }

            var sb = new StringBuilder(Length);
            foreach (var b in bytes)
            {
                // 256 is a multiple of 32, so the modulo is unbiased.
                sb.Append(Alphabet[b % Alphabet.Length]);
            }

            return sb.ToString();
        }
    }
}
