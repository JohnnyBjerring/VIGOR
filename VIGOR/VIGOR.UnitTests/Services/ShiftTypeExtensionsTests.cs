using VIGOR.Shared.Enums;
using Xunit;

namespace VIGOR.UnitTests.Services;

public class ShiftTypeExtensionsTests
{
    [Theory]
    [InlineData(ShiftType.Day, "Dagvagt")]
    [InlineData(ShiftType.Evening, "Aftenvagt")]
    [InlineData(ShiftType.Night, "Nattevagt")]
    public void ToDanishDisplayName_ReturnsExpectedLabel(ShiftType shiftType, string expected)
    {
        var result = shiftType.ToDanishDisplayName();

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(7, 0, ShiftType.Day)]
    [InlineData(14, 59, ShiftType.Day)]
    [InlineData(15, 0, ShiftType.Evening)]
    [InlineData(22, 59, ShiftType.Evening)]
    [InlineData(23, 0, ShiftType.Night)]
    [InlineData(6, 59, ShiftType.Night)]
    public void SuggestFromLocalTime_ReturnsExpectedShift(int hour, int minute, ShiftType expected)
    {
        var result = ShiftTypeExtensions.SuggestFromLocalTime(new TimeSpan(hour, minute, 0));

        Assert.Equal(expected, result);
    }
}
