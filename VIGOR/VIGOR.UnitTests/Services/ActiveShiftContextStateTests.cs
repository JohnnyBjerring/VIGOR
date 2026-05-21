using VIGOR.Shared.DTOs;
using VIGOR.Shared.Enums;
using VIGOR.Shared.Services;
using Xunit;

namespace VIGOR.UnitTests.Services;

public class ActiveShiftContextStateTests
{
    [Fact]
    public async Task SetAsync_StoresContextAndRaisesOnChange()
    {
        var state = new ActiveShiftContextState();
        var changeCount = 0;
        state.OnChange += () => changeCount++;

        var context = new ActiveShiftContextDto
        {
            ShiftType = ShiftType.Day,
            DisplayName = "Dagvagt",
            SelectedAtUtc = DateTime.UtcNow,
            SelectedByUserId = "user-1",
            DepartmentId = 1
        };

        await state.SetAsync(context);

        Assert.True(state.HasActiveShift);
        Assert.Same(context, state.Current);
        Assert.Equal(1, changeCount);
    }

    [Fact]
    public async Task ClearAsync_RemovesContextAndRaisesOnChange()
    {
        var state = new ActiveShiftContextState();
        var changeCount = 0;
        state.OnChange += () => changeCount++;

        await state.SetAsync(new ActiveShiftContextDto
        {
            ShiftType = ShiftType.Night,
            DisplayName = "Nattevagt",
            SelectedAtUtc = DateTime.UtcNow,
            SelectedByUserId = "user-1",
            DepartmentId = 1
        });

        await state.ClearAsync();

        Assert.False(state.HasActiveShift);
        Assert.Null(state.Current);
        Assert.Equal(2, changeCount);
    }
}
