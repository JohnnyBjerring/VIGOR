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
    [Route("api/citizens/{citizenId:int}/staff-assignments")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Leder,Vagtansvarlig,Personale")]
    public class StaffAssignmentsApiController : ControllerBase
    {
        private readonly IStaffAssignmentService _staffAssignmentService;
        private readonly AppDbContext _context;
        private readonly ILogger<StaffAssignmentsApiController> _logger;

        public StaffAssignmentsApiController(
            IStaffAssignmentService staffAssignmentService,
            AppDbContext context,
            ILogger<StaffAssignmentsApiController> logger)
        {
            _staffAssignmentService = staffAssignmentService;
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAssignments(int citizenId, CancellationToken cancellationToken)
        {
            try
            {
                var access = await ResolveEmployeeAccessAsync(cancellationToken);
                if (access.Result != null)
                {
                    return access.Result;
                }

                var assignments = await _staffAssignmentService.GetForCitizenAsync(citizenId, access.DepartmentId!.Value, cancellationToken);
                if (assignments == null)
                {
                    return NotFound();
                }

                return Ok(assignments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while getting staff assignments");
                return StatusCode(500);
            }
        }

        [HttpGet("available-staff")]
        public async Task<IActionResult> GetAssignableStaff(int citizenId, CancellationToken cancellationToken)
        {
            try
            {
                var access = await ResolveEmployeeAccessAsync(cancellationToken);
                if (access.Result != null)
                {
                    return access.Result;
                }

                var staff = await _staffAssignmentService.GetAssignableStaffAsync(citizenId, access.DepartmentId!.Value, cancellationToken);
                if (staff == null)
                {
                    return NotFound();
                }

                return Ok(staff);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while getting assignable staff");
                return StatusCode(500);
            }
        }

        [HttpPost]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Leder,Vagtansvarlig")]
        public async Task<IActionResult> AssignStaff(
            int citizenId,
            [FromBody] AssignStaffToCitizenRequest request,
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
                    var assigned = await _staffAssignmentService.AssignAsync(
                        citizenId,
                        access.DepartmentId!.Value,
                        request.EmployeeId,
                        access.UserId!,
                        cancellationToken,
                        access.UserDisplayNameSnapshot);

                    if (assigned == null)
                    {
                        return NotFound();
                    }

                    return CreatedAtAction(nameof(GetAssignments), new { citizenId }, assigned);
                }
                catch (ArgumentException ex)
                {
                    return BadRequest(ex.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while assigning staff to citizen");
                return StatusCode(500);
            }
        }

        [HttpPost("{citizenStaffAssignmentId:int}/unassign")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Leder,Vagtansvarlig")]
        public async Task<IActionResult> UnassignStaff(
            int citizenId,
            int citizenStaffAssignmentId,
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
                    var unassigned = await _staffAssignmentService.UnassignAsync(
                        citizenId,
                        citizenStaffAssignmentId,
                        access.DepartmentId!.Value,
                        access.UserId!,
                        cancellationToken,
                        access.UserDisplayNameSnapshot);

                    if (unassigned == null)
                    {
                        return NotFound();
                    }

                    return Ok(unassigned);
                }
                catch (ArgumentException ex)
                {
                    return BadRequest(ex.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while removing staff assignment");
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
