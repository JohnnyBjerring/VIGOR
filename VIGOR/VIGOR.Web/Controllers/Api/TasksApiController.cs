using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VIGOR.Shared.DTOs;
using VIGOR.Web.Data;
using VIGOR.Web.Services;

namespace VIGOR.Web.Controllers.Api
{
    [ApiController]
    [Route("api/citizens/{citizenId:int}/tasks")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Leder,Vagtansvarlig,Personale")]
    public class TasksApiController : ControllerBase
    {
        private readonly ICitizenTaskService _taskService;
        private readonly AppDbContext _context;
        private readonly ILogger<TasksApiController> _logger;

        public TasksApiController(
            ICitizenTaskService taskService,
            AppDbContext context,
            ILogger<TasksApiController> logger)
        {
            _taskService = taskService;
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetTasks(int citizenId, CancellationToken cancellationToken)
        {
            try
            {
                var access = await ResolveEmployeeAccessAsync(cancellationToken);
                if (access.Result != null)
                {
                    return access.Result;
                }

                var tasks = await _taskService.GetForCitizenAsync(citizenId, access.DepartmentId!.Value, cancellationToken);
                if (tasks == null)
                {
                    return NotFound();
                }

                return Ok(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while getting citizen tasks");
                return StatusCode(500);
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateTask(
            int citizenId,
            [FromBody] CreateCitizenTaskRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                if (request == null || !ModelState.IsValid)
                {
                    return BadRequest();
                }

                var access = await ResolveEmployeeAccessAsync(cancellationToken);
                if (access.Result != null)
                {
                    return access.Result;
                }

                try
                {
                    var created = await _taskService.CreateAsync(
                        citizenId,
                        access.DepartmentId!.Value,
                        access.UserId!,
                        request,
                        cancellationToken,
                        access.UserDisplayNameSnapshot);

                    if (created == null)
                    {
                        return NotFound();
                    }

                    return CreatedAtAction(nameof(GetTasks), new { citizenId }, created);
                }
                catch (ArgumentException ex)
                {
                    return BadRequest(ex.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating citizen task");
                return StatusCode(500);
            }
        }

        [HttpPost("{citizenTaskId:int}/complete")]
        public async Task<IActionResult> CompleteTask(
            int citizenId,
            int citizenTaskId,
            CancellationToken cancellationToken)
        {
            try
            {
                var access = await ResolveEmployeeAccessAsync(cancellationToken);
                if (access.Result != null)
                {
                    return access.Result;
                }

                try
                {
                    var completed = await _taskService.CompleteAsync(
                        citizenId,
                        citizenTaskId,
                        access.DepartmentId!.Value,
                        access.UserId!,
                        cancellationToken,
                        access.UserDisplayNameSnapshot);

                    if (completed == null)
                    {
                        return NotFound();
                    }

                    return Ok(completed);
                }
                catch (ArgumentException ex)
                {
                    return BadRequest(ex.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while completing citizen task");
                return StatusCode(500);
            }
        }

        private async Task<EmployeeAccessResult> ResolveEmployeeAccessAsync(CancellationToken cancellationToken)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return EmployeeAccessResult.Unauthorized();
            }

            var employee = await _context.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.IdentityUserId == userId, cancellationToken);

            if (employee == null || employee.DepartmentId == null)
            {
                return EmployeeAccessResult.Forbidden();
            }

            return EmployeeAccessResult.Allowed(userId, employee.DepartmentId.Value, employee.Name);
        }

        private sealed class EmployeeAccessResult
        {
            private EmployeeAccessResult(string? userId, int? departmentId, string? userDisplayNameSnapshot, IActionResult? result)
            {
                UserId = userId;
                DepartmentId = departmentId;
                UserDisplayNameSnapshot = userDisplayNameSnapshot;
                Result = result;
            }

            public string? UserId { get; }
            public int? DepartmentId { get; }
            public string? UserDisplayNameSnapshot { get; }
            public IActionResult? Result { get; }

            public static EmployeeAccessResult Allowed(string userId, int departmentId, string? userDisplayNameSnapshot)
            {
                return new EmployeeAccessResult(userId, departmentId, userDisplayNameSnapshot, null);
            }

            public static EmployeeAccessResult Unauthorized()
            {
                return new EmployeeAccessResult(null, null, null, new UnauthorizedResult());
            }

            public static EmployeeAccessResult Forbidden()
            {
                return new EmployeeAccessResult(null, null, null, new ForbidResult());
            }
        }
    }
}
