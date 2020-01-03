using Cinema.Core.Abstractions;
using Cinema.Core.Tests.Fakes;
using Xunit;

namespace Cinema.Core.Tests
{
    public class SingleSeatGapRuleTests
    {
        private readonly TestCinema _cinema = new TestCinema();

        [Fact]
        public void Selection_leaving_one_seat_next_to_another_party_is_rejected()
        {
            var service = _cinema.Holds();
            service.Hold(_cinema.Screening.Id, "alice", _cinema.Seats("B1", "B2"));

            // B3 would be stranded between alice (B2) and bob (B4).
            var ex = Assert.Throws<InvalidSeatSelectionException>(
                () => service.Hold(_cinema.Screening.Id, "bob", _cinema.Seats("B4", "B5")));

            Assert.Contains("B3", ex.Message);
        }

        [Fact]
        public void Selection_with_a_hole_in_the_middle_is_rejected()
        {
            Assert.Throws<InvalidSeatSelectionException>(
                () => _cinema.Holds().Hold(_cinema.Screening.Id, "alice", _cinema.Seats("C2", "C4")));
        }

        [Fact]
        public void Adjacent_selection_is_allowed()
        {
            var service = _cinema.Holds();
            service.Hold(_cinema.Screening.Id, "alice", _cinema.Seats("B1", "B2"));

            var hold = service.Hold(_cinema.Screening.Id, "bob", _cinema.Seats("B3", "B4"));

            Assert.Equal(2, hold.Items.Count);
        }

        [Fact]
        public void Gaps_at_the_row_end_are_allowed()
        {
            // Leaves C1 free next to the aisle - that seat is still sellable.
            var hold = _cinema.Holds().Hold(_cinema.Screening.Id, "alice", _cinema.Seats("C2", "C3"));

            Assert.Equal(2, hold.Items.Count);
        }

        [Fact]
        public void Pre_existing_gaps_are_not_blamed_on_the_customer()
        {
            _cinema.Options.EnforceNoSingleSeatGap = false;
            var service = _cinema.Holds();
            service.Hold(_cinema.Screening.Id, "alice", _cinema.Seats("D1"));
            service.Hold(_cinema.Screening.Id, "bob", _cinema.Seats("D3"));
            _cinema.Options.EnforceNoSingleSeatGap = true;

            // D2 is already stranded; carol sitting next to bob is fine.
            var hold = service.Hold(_cinema.Screening.Id, "carol", _cinema.Seats("D4", "D5"));

            Assert.Equal(2, hold.Items.Count);
        }

        [Fact]
        public void Rule_can_be_switched_off()
        {
            _cinema.Options.EnforceNoSingleSeatGap = false;

            var hold = _cinema.Holds().Hold(_cinema.Screening.Id, "alice", _cinema.Seats("C2", "C4"));

            Assert.Equal(2, hold.Items.Count);
        }
    }
}
