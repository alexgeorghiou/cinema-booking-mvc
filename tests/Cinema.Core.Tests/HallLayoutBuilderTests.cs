using System.Linq;
using Cinema.Core.Abstractions;
using Cinema.Core.Domain;
using Cinema.Core.Services;
using Xunit;

namespace Cinema.Core.Tests
{
    public class HallLayoutBuilderTests
    {
        private readonly HallLayoutBuilder _builder = new HallLayoutBuilder();

        [Theory]
        [InlineData(0, "A")]
        [InlineData(1, "B")]
        [InlineData(25, "Z")]
        [InlineData(26, "AA")]
        [InlineData(27, "AB")]
        [InlineData(51, "AZ")]
        [InlineData(52, "BA")]
        public void RowLabel_uses_spreadsheet_style_lettering(int index, string expected)
        {
            Assert.Equal(expected, HallLayoutBuilder.RowLabel(index));
        }

        [Fact]
        public void Build_creates_rows_times_seats()
        {
            var hall = _builder.Build("Screen 1", 5, 8, 0, null);

            Assert.Equal(40, hall.Capacity);
            Assert.Equal(5, hall.Rows().Count());
            Assert.Equal(new[] { "A1", "A2", "A3" }, hall.Rows().First().Take(3).Select(s => s.Label));
        }

        [Fact]
        public void Build_marks_rearmost_rows_as_premium()
        {
            var hall = _builder.Build("Screen 2", 6, 4, 2, null);

            var premiumRows = hall.Seats.Where(s => s.Type == SeatType.Premium).Select(s => s.Row).Distinct().OrderBy(r => r);
            Assert.Equal(new[] { "E", "F" }, premiumRows);
        }

        [Fact]
        public void Build_flags_accessible_seats_case_insensitively()
        {
            var hall = _builder.Build("Screen 3", 3, 5, 0, new[] { "a1", " A2 ", "c5" });

            var accessible = hall.Seats.Where(s => s.Type == SeatType.Accessible).Select(s => s.Label).OrderBy(l => l);
            Assert.Equal(new[] { "A1", "A2", "C5" }, accessible);
        }

        [Fact]
        public void Build_rejects_accessible_seat_outside_the_plan()
        {
            var ex = Assert.Throws<InvalidSeatSelectionException>(() => _builder.Build("Screen 4", 2, 2, 0, new[] { "Z9" }));
            Assert.Contains("Z9", ex.Message);
        }

        [Theory]
        [InlineData(0, 10, 0)]
        [InlineData(41, 10, 0)]
        [InlineData(5, 0, 0)]
        [InlineData(5, 61, 0)]
        [InlineData(5, 10, 6)]
        public void Build_validates_dimensions(int rows, int seats, int premium)
        {
            Assert.Throws<InvalidSeatSelectionException>(() => _builder.Build("X", rows, seats, premium, null));
        }

        [Fact]
        public void ParseSeatList_accepts_commas_semicolons_and_spaces()
        {
            Assert.Equal(new[] { "A1", "B2", "C3" }, HallLayoutBuilder.ParseSeatList("A1, B2;C3"));
            Assert.Empty(HallLayoutBuilder.ParseSeatList("  "));
        }
    }
}
