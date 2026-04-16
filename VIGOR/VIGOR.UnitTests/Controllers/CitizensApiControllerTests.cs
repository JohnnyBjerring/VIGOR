using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using VIGOR.Shared.DTOs;
using VIGOR.Shared.Enums;
using VIGOR.Shared.Models;
using VIGOR.Web.Controllers.Api;
using VIGOR.Web.Data;
using VIGOR.Web.Services;
using Xunit;

namespace VIGOR.UnitTests.Controllers;

public class CitizensApiControllerTests
{
    [Fact]
    public async Task GetCitizens_ReturnsUnauthorized_WhenNoUser()
    {
        using var context = CreateContext();
        var service = new CitizenService(context);
        var controller = new CitizensApiController(service, context, NullLogger<CitizensApiController>.Instance);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var result = await controller.GetCitizens(CancellationToken.None);
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task GetCitizens_ReturnsOk_ForEmployeeWithDepartment()
    {
        using var context = CreateContext();
        var department = new Department { Name = "A" };
        context.Departments.Add(department);
        await context.SaveChangesAsync();

        var employee = new Employee { IdentityUserId = "user-1", Name = "Employee 1", DepartmentId = department.DepartmentId };
        context.Employees.Add(employee);
        context.Citizens.Add(new Citizen { Name = "Citizen", DepartmentId = department.DepartmentId });
        await context.SaveChangesAsync();

        var service = new CitizenService(context);
        var controller = new CitizensApiController(service, context, NullLogger<CitizensApiController>.Instance);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, employee.IdentityUserId),
                    new Claim(ClaimTypes.Role, "Personale")
                }, "jwt"))
            }
        };

        var result = await controller.GetCitizens(CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result);
        var citizens = Assert.IsAssignableFrom<IEnumerable<Citizen>>(ok.Value);
        Assert.Single(citizens);
        Assert.Equal(department.DepartmentId, citizens.First().DepartmentId);
    }

    [Fact]
    public async Task UpdateStatus_ReturnsUnauthorized_WhenNoUser()
    {
        using var context = CreateContext();
        var service = new CitizenService(context);
        var controller = new CitizensApiController(service, context, NullLogger<CitizensApiController>.Instance);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var request = new UpdateCitizenStatusRequest { Status = CitizenStatus.Red };
        var result = await controller.UpdateStatus(1, request, CancellationToken.None);
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task UpdateStatus_ReturnsForbid_WhenEmployeeMissingOrDepartmentMissing()
    {
        using var context = CreateContext();
        var service = new CitizenService(context);
        var controller = new CitizensApiController(service, context, NullLogger<CitizensApiController>.Instance);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "invalid-user"),
                    new Claim(ClaimTypes.Role, "Personale")
                }, "jwt"))
            }
        };

        var request = new UpdateCitizenStatusRequest { Status = CitizenStatus.Red };
        var result = await controller.UpdateStatus(1, request, CancellationToken.None);
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task UpdateStatus_ReturnsBadRequest_WhenStatusInvalid()
    {
        using var context = CreateContext();
        var service = new CitizenService(context);
        var controller = new CitizensApiController(service, context, NullLogger<CitizensApiController>.Instance);

        var request = new UpdateCitizenStatusRequest { Status = (CitizenStatus)999 };
        var result = await controller.UpdateStatus(1, request, CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateStatus_ReturnsNotFound_WhenCitizenNotFound()
    {
        using var context = CreateContext();
        var department = new Department { Name = "A" };
        context.Departments.Add(department);
        await context.SaveChangesAsync();

        var employee = new Employee { IdentityUserId = "user-1", Name = "Employee 1", DepartmentId = department.DepartmentId };
        context.Employees.Add(employee);
        await context.SaveChangesAsync();

        var service = new CitizenService(context);
        var controller = new CitizensApiController(service, context, NullLogger<CitizensApiController>.Instance);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, employee.IdentityUserId),
                    new Claim(ClaimTypes.Role, "Personale")
                }, "jwt"))
            }
        };

        var request = new UpdateCitizenStatusRequest { Status = CitizenStatus.Red };
        var result = await controller.UpdateStatus(999, request, CancellationToken.None);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task UpdateStatus_ReturnsOk_WhenStatusUpdated()
    {
        using var context = CreateContext();
        var department = new Department { Name = "A" };
        context.Departments.Add(department);
        await context.SaveChangesAsync();

        var employee = new Employee { IdentityUserId = "user-1", Name = "Employee 1", DepartmentId = department.DepartmentId };
        context.Employees.Add(employee);
        
        var citizen = new Citizen { Name = "Citizen", DepartmentId = department.DepartmentId, Status = CitizenStatus.Green };
        context.Citizens.Add(citizen);
        
        await context.SaveChangesAsync();

        var service = new CitizenService(context);
        var controller = new CitizensApiController(service, context, NullLogger<CitizensApiController>.Instance);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, employee.IdentityUserId),
                    new Claim(ClaimTypes.Role, "Personale")
                }, "jwt"))
            }
        };

        var request = new UpdateCitizenStatusRequest { Status = CitizenStatus.Red };
        var result = await controller.UpdateStatus(citizen.CitizenId, request, CancellationToken.None);
        
        var ok = Assert.IsType<OkObjectResult>(result);
        var updatedCitizen = Assert.IsType<Citizen>(ok.Value);
        Assert.Equal(CitizenStatus.Red, updatedCitizen.Status);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}
