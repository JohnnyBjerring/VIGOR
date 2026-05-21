using VIGOR.Shared.Enums;
using VIGOR.Web.Services;
using Xunit;

namespace VIGOR.UnitTests.Services;

public class ShiftSelectionServiceTests
{
    [Fact]
    public async Task SelectShiftAsync_ReturnsActiveShiftContext_WhenInputIsValid()
    {
        var service = new ShiftSelectionService();

        var result = await service.SelectShiftAsync(ShiftType.Evening, "user-1", 42);

        Assert.Equal(ShiftType.Evening, result.ShiftType);
        Assert.Equal("Aftenvagt", result.DisplayName);
        Assert.Equal("user-1", result.SelectedByUserId);
        Assert.Equal(42, result.DepartmentId);
        Assert.True(result.SelectedAtUtc <= DateTime.UtcNow);
    }

    [Fact]
    public async Task SelectShiftAsync_ThrowsArgumentException_WhenShiftTypeIsInvalid()
    {
        var service = new ShiftSelectionService();

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.SelectShiftAsync((ShiftType)999, "user-1", 1));

        Assert.Contains("ugyldig", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SelectShiftAsync_ThrowsArgumentException_WhenUserIdIsMissing()
    {
        var service = new ShiftSelectionService();

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.SelectShiftAsync(ShiftType.Day, "", 1));

        Assert.Contains("Bruger", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SelectShiftAsync_ThrowsArgumentException_WhenDepartmentIsInvalid()
    {
        var service = new ShiftSelectionService();

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.SelectShiftAsync(ShiftType.Day, "user-1", 0));

        Assert.Contains("Afdeling", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
