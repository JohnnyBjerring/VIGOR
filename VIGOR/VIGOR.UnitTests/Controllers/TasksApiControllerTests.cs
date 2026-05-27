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

public class TasksApiControllerTests
{
    [Fact]
    public async Task GetTasks_ReturnsUnauthorized_WhenNoUser()
    {
        using var context = CreateContext();
        var controller = CreateController(context);

        var result = await controller.GetTasks(1, CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task GetTasks_ReturnsForbid_WhenEmployeeMissingOrDepartmentMissing()
    {
        using var context = CreateContext();
        var controller = CreateController(context, "invalid-user");

        var result = await controller.GetTasks(1, CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetTasks_ReturnsNotFound_WhenCitizenNotInDepartment()
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

        var result = await controller.GetTasks(citizen.CitizenId, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetTasks_ReturnsOk_ForCitizenInDepartment()
    {
        using var context = CreateContext();

        var (department, employee, citizen) = await CreateDepartmentEmployeeAndCitizenAsync(context);
        context.CitizenTasks.Add(new CitizenTask
        {
            CitizenId = citizen.CitizenId,
            DepartmentId = department.DepartmentId,
            ShiftType = ShiftType.Day,
            Title = "Husk væskeskema",
            Description = "Beskrivelse",
            CreatedByUserId = employee.IdentityUserId,
            CreatedAtUtc = new DateTime(2026, 5, 25, 8, 0, 0, DateTimeKind.Utc),
            IsCompleted = false
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context, employee.IdentityUserId);

        var result = await controller.GetTasks(citizen.CitizenId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var tasks = Assert.IsAssignableFrom<IReadOnlyList<CitizenTaskDto>>(ok.Value);
        Assert.Single(tasks);
    }

    [Fact]
    public async Task CreateTask_ReturnsUnauthorized_WhenNoUser()
    {
        using var context = CreateContext();
        var controller = CreateController(context);

        var result = await controller.CreateTask(1, CreateValidRequest(), CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task CreateTask_ReturnsForbid_WhenEmployeeMissingOrDepartmentMissing()
    {
        using var context = CreateContext();
        var controller = CreateController(context, "invalid-user");

        var result = await controller.CreateTask(1, CreateValidRequest(), CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task CreateTask_ReturnsNotFound_WhenCitizenNotInDepartment()
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

        var result = await controller.CreateTask(citizen.CitizenId, CreateValidRequest(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task CreateTask_ReturnsBadRequest_WhenRequestInvalid()
    {
        using var context = CreateContext();

        var (_, employee, citizen) = await CreateDepartmentEmployeeAndCitizenAsync(context);
        var controller = CreateController(context, employee.IdentityUserId);
        var request = CreateValidRequest();
        request.Title = string.Empty;

        var result = await controller.CreateTask(citizen.CitizenId, request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateTask_ReturnsCreated_WhenRequestIsValid()
    {
        using var context = CreateContext();

        var (department, employee, citizen) = await CreateDepartmentEmployeeAndCitizenAsync(context);
        var controller = CreateController(context, employee.IdentityUserId);
        var request = new CreateCitizenTaskRequest
        {
            Title = "Husk væskeskema",
            Description = "Følg op efter aftensmad.",
            ShiftType = ShiftType.Evening
        };

        var result = await controller.CreateTask(citizen.CitizenId, request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var dto = Assert.IsType<CitizenTaskDto>(created.Value);

        Assert.Equal(citizen.CitizenId, dto.CitizenId);
        Assert.Equal(department.DepartmentId, dto.DepartmentId);
        Assert.Equal(ShiftType.Evening, dto.ShiftType);
        Assert.Equal("Husk væskeskema", dto.Title);
        Assert.Equal("Følg op efter aftensmad.", dto.Description);
        Assert.Equal(employee.IdentityUserId, dto.CreatedByUserId);
        Assert.False(dto.IsCompleted);

        var dbTask = await context.CitizenTasks.SingleAsync();
        Assert.Equal("Husk væskeskema", dbTask.Title);
        Assert.Equal(employee.IdentityUserId, dbTask.CreatedByUserId);
    }

    [Fact]
    public async Task CompleteTask_ReturnsUnauthorized_WhenNoUser()
    {
        using var context = CreateContext();
        var controller = CreateController(context);

        var result = await controller.CompleteTask(1, 1, CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task CompleteTask_ReturnsNotFound_WhenTaskNotInDepartment()
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

        var result = await controller.CompleteTask(citizen.CitizenId, 123, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task CompleteTask_ReturnsOk_WhenTaskExists()
    {
        using var context = CreateContext();

        var (department, employee, citizen) = await CreateDepartmentEmployeeAndCitizenAsync(context);
        var task = new CitizenTask
        {
            CitizenId = citizen.CitizenId,
            DepartmentId = department.DepartmentId,
            ShiftType = ShiftType.Day,
            Title = "Husk væskeskema",
            Description = "Beskrivelse",
            CreatedByUserId = employee.IdentityUserId,
            CreatedAtUtc = DateTime.UtcNow,
            IsCompleted = false
        };
        context.CitizenTasks.Add(task);
        await context.SaveChangesAsync();

        var controller = CreateController(context, employee.IdentityUserId);

        var result = await controller.CompleteTask(citizen.CitizenId, task.CitizenTaskId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<CitizenTaskDto>(ok.Value);

        Assert.True(dto.IsCompleted);
        Assert.Equal(employee.IdentityUserId, dto.CompletedByUserId);

        var dbTask = await context.CitizenTasks.SingleAsync();
        Assert.True(dbTask.IsCompleted);
        Assert.Equal(employee.IdentityUserId, dbTask.CompletedByUserId);
    }

    private static CreateCitizenTaskRequest CreateValidRequest()
    {
        return new CreateCitizenTaskRequest
        {
            Title = "Husk væskeskema",
            Description = "Følg op senere.",
            ShiftType = ShiftType.Day
        };
    }

    private static TasksApiController CreateController(AppDbContext context, string? userId = null, string role = "Personale")
    {
        var service = new CitizenTaskService(context);

        var controller = new TasksApiController(
            service,
            context,
            NullLogger<TasksApiController>.Instance);

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
