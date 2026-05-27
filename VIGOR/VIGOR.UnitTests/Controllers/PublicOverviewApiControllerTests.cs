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

public class PublicOverviewApiControllerTests
{
    [Fact]
    public async Task GetPublicOverview_ReturnsOk_WithoutAuthenticatedUser()
    {
        using var context = CreateContext();
        var department = new Department { Name = "A" };
        context.Departments.Add(department);
        await context.SaveChangesAsync();

        context.Citizens.Add(new Citizen
        {
            Name = "Anna",
            DepartmentId = department.DepartmentId,
            Status = CitizenStatus.Green
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);

        var result = await controller.GetPublicOverview(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<PublicOverviewDto>(ok.Value);
        Assert.Equal(1, dto.TotalCitizenCount);
        Assert.Single(dto.Citizens);
        Assert.Equal("Borger 1", dto.Citizens.Single().DisplayLabel);
    }

    [Fact]
    public async Task GetPublicOverview_ReturnsAnonymousDto_WithoutCitizenNames()
    {
        using var context = CreateContext();
        var department = new Department { Name = "A" };
        context.Departments.Add(department);
        await context.SaveChangesAsync();

        context.Citizens.Add(new Citizen
        {
            Name = "Meget Hemmeligt Navn",
            DepartmentId = department.DepartmentId,
            Status = CitizenStatus.Yellow
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);

        var result = await controller.GetPublicOverview(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<PublicOverviewDto>(ok.Value);
        var citizen = Assert.Single(dto.Citizens);
        Assert.Equal("Borger 1", citizen.DisplayLabel);
        Assert.Equal(CitizenStatus.Yellow, citizen.Status);
    }

    private static PublicOverviewApiController CreateController(AppDbContext context)
    {
        var service = new PublicOverviewService(context);
        return new PublicOverviewApiController(
            service,
            NullLogger<PublicOverviewApiController>.Instance);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
