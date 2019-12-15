using System;

namespace Cinema.Core.Abstractions
{
    /// <summary>Time source; injected so hold expiry and cut-offs are testable.</summary>
    public interface IClock
    {
        DateTime UtcNow { get; }
    }

    public sealed class SystemClock : IClock
    {
        public DateTime UtcNow
        {
            get { return DateTime.UtcNow; }
        }
    }
}
