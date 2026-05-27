using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VIGOR.Web.Data;
using VIGOR.Web.Services;

namespace VIGOR.Web.Controllers.Api
{
    [ApiController]
    [Route("api/statistics")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Leder,Superbruger")]
    public class StatisticsApiController : ControllerBase
    {
        private readonly IStatisticsService _statisticsService;
        private readonly AppDbContext _context;
        private readonly ILogger<StatisticsApiController> _logger;

        public StatisticsApiController(
            IStatisticsService statisticsService,
            AppDbContext context,
            ILogger<StatisticsApiController> logger)
        {
            _statisticsService = statisticsService;
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetStatistics(
            [FromQuery] DateTime? fromUtc,
            [FromQuery] DateTime? toUtc,
            CancellationToken cancellationToken)
        {
            if (fromUtc.HasValue && toUtc.HasValue && fromUtc.Value >= toUtc.Value)
            {
                return BadRequest("Startdato skal ligge før slutdato.");
            }

            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Unauthorized();
                }

                var isLeader = User.IsInRole("Leder");
                var isSuperUser = User.IsInRole("Superbruger");

                if (isLeader)
                {
                    var employee = await _context.Employees
                        .AsNoTracking()
                        .FirstOrDefaultAsync(e => e.IdentityUserId == userId, cancellationToken);

                    if (employee?.DepartmentId == null)
                    {
                        return Forbid();
                    }

                    var department = await _context.Departments
                        .AsNoTracking()
                        .FirstOrDefaultAsync(d => d.DepartmentId == employee.DepartmentId.Value, cancellationToken);

                    if (department == null)
                    {
                        return Forbid();
                    }

                    var statistics = await _statisticsService.GetDepartmentStatisticsAsync(
                        department.DepartmentId,
                        department.Name,
                        fromUtc?.ToUniversalTime(),
                        toUtc?.ToUniversalTime(),
                        cancellationToken);

                    return Ok(statistics);
                }

                if (isSuperUser)
                {
                    var statistics = await _statisticsService.GetSystemStatisticsAsync(
                        fromUtc?.ToUniversalTime(),
                        toUtc?.ToUniversalTime(),
                        cancellationToken);

                    return Ok(statistics);
                }

                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while getting statistics");
                return StatusCode(500);
            }
        }
    }
}
