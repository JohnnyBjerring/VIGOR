using System.Threading;
using Microsoft.EntityFrameworkCore;
using VIGOR.Shared.Models;
using VIGOR.Web.Data;
using VIGOR.Web.Services;
using Xunit;

namespace VIGOR.UnitTests.Services;

public class CitizenServiceTests
{
    [Fact]
    public async Task GetCitizensByDepartmentAsync_ReturnsMatchingDepartment()
    {
        using var context = CreateContext();
        var departmentA = new Department { Name = "A" };
        var departmentB = new Department { Name = "B" };
        context.Departments.AddRange(departmentA, departmentB);
        await context.SaveChangesAsync();

        context.Citizens.AddRange(
            new Citizen { Name = "One", DepartmentId = departmentA.DepartmentId },
            new Citizen { Name = "Two", DepartmentId = departmentA.DepartmentId },
            new Citizen { Name = "Three", DepartmentId = departmentB.DepartmentId });
        await context.SaveChangesAsync();

        var service = new CitizenService(context);
        var result = await service.GetCitizensByDepartmentAsync(departmentA.DepartmentId, CancellationToken.None);

        Assert.Equal(2, result.Count());
        Assert.All(result, citizen => Assert.Equal(departmentA.DepartmentId, citizen.DepartmentId));
    }

    [Fact]
    public async Task GetCitizensByDepartmentAsync_ReturnsEmpty_WhenNoCitizens()
    {
        using var context = CreateContext();
        var department = new Department { Name = "A" };
        context.Departments.Add(department);
        await context.SaveChangesAsync();

        var service = new CitizenService(context);
        var result = await service.GetCitizensByDepartmentAsync(department.DepartmentId, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task UpdateCitizenStatusAsync_UpdatesCitizen_WhenCitizenExistsInDepartment()
    {
        using var context = CreateContext();
        var department = new Department { Name = "A" };
        context.Departments.Add(department);
        await context.SaveChangesAsync();

        var citizen = new Citizen { Name = "Test", DepartmentId = department.DepartmentId, Status = VIGOR.Shared.Enums.CitizenStatus.Green };
        context.Citizens.Add(citizen);
        await context.SaveChangesAsync();

        var service = new CitizenService(context);
        var result = await service.UpdateCitizenStatusAsync(citizen.CitizenId, department.DepartmentId, VIGOR.Shared.Enums.CitizenStatus.Red, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(VIGOR.Shared.Enums.CitizenStatus.Red, result.Status);
        
        var dbCitizen = await context.Citizens.FindAsync(citizen.CitizenId);
        Assert.Equal(VIGOR.Shared.Enums.CitizenStatus.Red, dbCitizen!.Status);
    }

    [Fact]
    public async Task UpdateCitizenStatusAsync_ReturnsNull_WhenCitizenDoesNotExist()
    {
        using var context = CreateContext();
        var department = new Department { Name = "A" };
        context.Departments.Add(department);
        await context.SaveChangesAsync();

        var service = new CitizenService(context);
        var result = await service.UpdateCitizenStatusAsync(999, department.DepartmentId, VIGOR.Shared.Enums.CitizenStatus.Red, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateCitizenStatusAsync_ReturnsNull_WhenCitizenExistsInAnotherDepartment()
    {
        using var context = CreateContext();
        var department1 = new Department { Name = "A" };
        var department2 = new Department { Name = "B" };
        context.Departments.AddRange(department1, department2);
        await context.SaveChangesAsync();

        var citizen = new Citizen { Name = "Test", DepartmentId = department1.DepartmentId, Status = VIGOR.Shared.Enums.CitizenStatus.Green };
        context.Citizens.Add(citizen);
        await context.SaveChangesAsync();

        var service = new CitizenService(context);
        var result = await service.UpdateCitizenStatusAsync(citizen.CitizenId, department2.DepartmentId, VIGOR.Shared.Enums.CitizenStatus.Red, CancellationToken.None);

        Assert.Null(result);
        
        var dbCitizen = await context.Citizens.FindAsync(citizen.CitizenId);
        Assert.Equal(VIGOR.Shared.Enums.CitizenStatus.Green, dbCitizen!.Status);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}
