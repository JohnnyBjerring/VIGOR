using VIGOR.Shared.DTOs;
using VIGOR.Shared.Enums;
using VIGOR.Shared.Services;
using Xunit;

namespace VIGOR.UnitTests.Services;

public class SelectedShiftStateTests
{
    [Fact]
    public void CurrentShift_IsNull_WhenStateIsNew()
    {
        var state = new SelectedShiftState();

        Assert.False(state.HasSelectedShift);
        Assert.Null(state.CurrentShift);
    }

    [Fact]
    public void SetSelectedShift_StoresCurrentShift()
    {
        var state = new SelectedShiftState();
        var selectedShift = new SelectedShiftDto
        {
            ShiftType = ShiftType.Day,
            DisplayName = "Dagvagt",
            SelectedAtUtc = new DateTime(2026, 5, 16, 7, 0, 0, DateTimeKind.Utc),
            SelectedByUserId = "user-1",
            DepartmentId = 1
        };

        state.SetSelectedShift(selectedShift);

        Assert.True(state.HasSelectedShift);
        Assert.Same(selectedShift, state.CurrentShift);
    }

    [Fact]
    public void Clear_RemovesCurrentShift()
    {
        var state = new SelectedShiftState();
        state.SetSelectedShift(new SelectedShiftDto
        {
            ShiftType = ShiftType.Night,
            DisplayName = "Nattevagt",
            SelectedAtUtc = new DateTime(2026, 5, 16, 23, 0, 0, DateTimeKind.Utc),
            SelectedByUserId = "user-1",
            DepartmentId = 1
        });

        state.Clear();

        Assert.False(state.HasSelectedShift);
        Assert.Null(state.CurrentShift);
    }
}
