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

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}
