using System.Collections.Generic;
using Cinema.Core.Services;

namespace Cinema.Core.Tests.Fakes
{
    public sealed class SequenceReferenceGenerator : IBookingReferenceGenerator
    {
        private readonly Queue<string> _values;

        public SequenceReferenceGenerator(params string[] values)
        {
            _values = new Queue<string>(values);
        }

        public string Next() => _values.Dequeue();
    }
}
