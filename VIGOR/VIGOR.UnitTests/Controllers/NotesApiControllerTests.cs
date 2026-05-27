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

public class NotesApiControllerTests
{
    [Fact]
    public async Task GetNotes_ReturnsUnauthorized_WhenNoUser()
    {
        using var context = CreateContext();
        var controller = CreateController(context);

        var result = await controller.GetNotes(1, CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task GetNotes_ReturnsForbid_WhenEmployeeMissingOrDepartmentMissing()
    {
        using var context = CreateContext();
        var controller = CreateController(context, "invalid-user");

        var result = await controller.GetNotes(1, CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetNotes_ReturnsNotFound_WhenCitizenNotInDepartment()
    {
        using var context = CreateContext();

        var depA = new Department { Name = "A" };
        var depB = new Department { Name = "B" };
        context.Departments.AddRange(depA, depB);
        await context.SaveChangesAsync();

        var citizen = new Citizen { Name = "Citizen", DepartmentId = depB.DepartmentId };
        var employee = new Employee
        {
            IdentityUserId = "user-1",
            Name = "Employee",
            DepartmentId = depA.DepartmentId
        };

        context.Citizens.Add(citizen);
        context.Employees.Add(employee);
        await context.SaveChangesAsync();

        var controller = CreateController(context, employee.IdentityUserId);

        var result = await controller.GetNotes(citizen.CitizenId, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetNotes_ReturnsOk_ForCitizenInDepartment()
    {
        using var context = CreateContext();

        var (department, employee, citizen) = await CreateDepartmentEmployeeAndCitizenAsync(context);
        context.Notes.Add(new Note
        {
            CitizenId = citizen.CitizenId,
            DepartmentId = department.DepartmentId,
            ShiftType = ShiftType.Day,
            Content = "Borger virker rolig.",
            CreatedByUserId = employee.IdentityUserId,
            CreatedAtUtc = new DateTime(2026, 5, 25, 8, 0, 0, DateTimeKind.Utc)
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context, employee.IdentityUserId);

        var result = await controller.GetNotes(citizen.CitizenId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var notes = Assert.IsAssignableFrom<IReadOnlyList<NoteDto>>(ok.Value);
        Assert.Single(notes);
    }

    [Fact]
    public async Task CreateNote_ReturnsUnauthorized_WhenNoUser()
    {
        using var context = CreateContext();
        var controller = CreateController(context);

        var result = await controller.CreateNote(1, CreateValidRequest(), CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task CreateNote_ReturnsForbid_WhenEmployeeMissingOrDepartmentMissing()
    {
        using var context = CreateContext();
        var controller = CreateController(context, "invalid-user");

        var result = await controller.CreateNote(1, CreateValidRequest(), CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task CreateNote_ReturnsNotFound_WhenCitizenNotInDepartment()
    {
        using var context = CreateContext();

        var depA = new Department { Name = "A" };
        var depB = new Department { Name = "B" };
        context.Departments.AddRange(depA, depB);
        await context.SaveChangesAsync();

        var citizen = new Citizen { Name = "Citizen", DepartmentId = depB.DepartmentId };
        var employee = new Employee
        {
            IdentityUserId = "user-1",
            Name = "Employee",
            DepartmentId = depA.DepartmentId
        };

        context.Citizens.Add(citizen);
        context.Employees.Add(employee);
        await context.SaveChangesAsync();

        var controller = CreateController(context, employee.IdentityUserId);

        var result = await controller.CreateNote(citizen.CitizenId, CreateValidRequest(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task CreateNote_ReturnsBadRequest_WhenRequestInvalid()
    {
        using var context = CreateContext();

        var (_, employee, citizen) = await CreateDepartmentEmployeeAndCitizenAsync(context);
        var controller = CreateController(context, employee.IdentityUserId);
        var request = CreateValidRequest();
        request.Content = string.Empty;

        var result = await controller.CreateNote(citizen.CitizenId, request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateNote_ReturnsCreated_WhenRequestIsValid()
    {
        using var context = CreateContext();

        var (department, employee, citizen) = await CreateDepartmentEmployeeAndCitizenAsync(context);
        var controller = CreateController(context, employee.IdentityUserId);
        var request = new CreateNoteRequest
        {
            Content = "Borger virker rolig efter aftensmad.",
            ShiftType = ShiftType.Evening
        };

        var result = await controller.CreateNote(citizen.CitizenId, request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var dto = Assert.IsType<NoteDto>(created.Value);

        Assert.Equal(citizen.CitizenId, dto.CitizenId);
        Assert.Equal(department.DepartmentId, dto.DepartmentId);
        Assert.Equal(ShiftType.Evening, dto.ShiftType);
        Assert.Equal("Borger virker rolig efter aftensmad.", dto.Content);
        Assert.Equal(employee.IdentityUserId, dto.CreatedByUserId);

        var dbNote = await context.Notes.SingleAsync();
        Assert.Equal("Borger virker rolig efter aftensmad.", dbNote.Content);
        Assert.Equal(employee.IdentityUserId, dbNote.CreatedByUserId);
    }

    private static CreateNoteRequest CreateValidRequest()
    {
        return new CreateNoteRequest
        {
            Content = "Borger virker rolig.",
            ShiftType = ShiftType.Day
        };
    }

    private static NotesApiController CreateController(AppDbContext context, string? userId = null, string role = "Personale")
    {
        var service = new NoteService(context);

        var controller = new NotesApiController(
            service,
            context,
            NullLogger<NotesApiController>.Instance);

        var httpContext = new DefaultHttpContext();

        if (!string.IsNullOrWhiteSpace(userId))
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, role)
            }, "jwt"));
        }

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        return controller;
    }

    private static async Task<(Department Department, Employee Employee, Citizen Citizen)> CreateDepartmentEmployeeAndCitizenAsync(AppDbContext context)
    {
        var department = new Department { Name = "A" };
        context.Departments.Add(department);
        await context.SaveChangesAsync();

        var employee = new Employee
        {
            IdentityUserId = "user-1",
            Name = "Employee",
            DepartmentId = department.DepartmentId
        };

        var citizen = new Citizen
        {
            Name = "Citizen",
            DepartmentId = department.DepartmentId
        };

        context.Employees.Add(employee);
        context.Citizens.Add(citizen);
        await context.SaveChangesAsync();

        return (department, employee, citizen);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
