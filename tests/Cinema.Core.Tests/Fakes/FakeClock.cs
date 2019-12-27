using System;
using Cinema.Core.Abstractions;

namespace Cinema.Core.Tests.Fakes
{
    public sealed class FakeClock : IClock
    {
        public FakeClock(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; set; }

        public void Advance(TimeSpan by)
        {
            UtcNow = UtcNow.Add(by);
        }
    }
}
