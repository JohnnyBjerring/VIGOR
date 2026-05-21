using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VIGOR.Shared.DTOs;
using VIGOR.Shared.Enums;
using VIGOR.Shared.Interfaces.Services;
using VIGOR.Web.Data;
using VIGOR.Web.Services;

namespace VIGOR.Web.Controllers.Api
{
    [ApiController]
    [Route("api/citizens")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Leder,Vagtansvarlig,Personale")]
    public class CitizensApiController : ControllerBase
    {
        private readonly ICitizenService _citizenService;
        private readonly IFixedMedicationService _fixedMedicationService;
        private readonly AppDbContext _context;
        private readonly ILogger<CitizensApiController> _logger;

        public CitizensApiController(ICitizenService citizenService, IFixedMedicationService fixedMedicationService, AppDbContext context, ILogger<CitizensApiController> logger)
        {
            _citizenService = citizenService;
            _fixedMedicationService = fixedMedicationService;
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetCitizens(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogWarning("CitizensApiController.GetCitizens REACHED! Claims = {Claims}", string.Join(", ", User.Claims.Select(c => c.Type)));
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                {
                    _logger.LogWarning("UserId is null! Claims: {Claims}", string.Join(", ", User.Claims.Select(c => c.Type)));
                    return Unauthorized();
                }

                var employee = await _context.Employees
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.IdentityUserId == userId, cancellationToken);

                if (employee == null || employee.DepartmentId == null)
                {
                    _logger.LogWarning("Employee not found or DepartmentId is null for userId: {UserId}", userId);
                    return Forbid();
                }

                // Department og sikkerhed fastsættes udelukkende ud fra den autentificerede medarbejders Employee/Department relation.
                var safeDepartmentId = employee.DepartmentId.Value;

                var citizens = await _citizenService.GetCitizensByDepartmentAsync(safeDepartmentId, cancellationToken);
                return Ok(citizens);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while getting citizens");
                return StatusCode(500);
            }
        }

        [HttpPost("{citizenId:int}/status")]
        public async Task<IActionResult> UpdateStatus(int citizenId, [FromBody] UpdateCitizenStatusRequest request, CancellationToken cancellationToken)
        {
            try
            {
                if (request == null || !ModelState.IsValid)
                {
                    return BadRequest();
                }

                if (!Enum.IsDefined(typeof(CitizenStatus), request.Status))
                {
                    return BadRequest("Invalid status value.");
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                {
                    return Unauthorized();
                }

                var employee = await _context.Employees
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.IdentityUserId == userId, cancellationToken);

                if (employee == null || employee.DepartmentId == null)
                {
                    return Forbid();
                }

                var updatedCitizen = await _citizenService.UpdateCitizenStatusAsync(
                    citizenId,
                    employee.DepartmentId.Value,
                    request.Status,
                    userId,
                    employee.Name,
                    shiftType: null,
                    cancellationToken: cancellationToken);

                if (updatedCitizen == null)
                {
                    return NotFound();
                }

                return Ok(updatedCitizen);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating citizen status");
                return StatusCode(500);
            }
        }

        [HttpGet("{citizenId:int}/fixed-medications")]
        public async Task<IActionResult> GetFixedMedications(int citizenId, CancellationToken cancellationToken)
        {
            try
            {
                var access = await ResolveEmployeeAccessAsync(cancellationToken);
                if (access.Result != null)
                {
                    return access.Result;
                }

                var meds = await _fixedMedicationService.GetForCitizenAsync(citizenId, access.DepartmentId!.Value, cancellationToken);
                if (meds == null)
                {
                    return NotFound();
                }

                return Ok(meds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while getting fixed medications");
                return StatusCode(500);
            }
        }

        [HttpPost("{citizenId:int}/fixed-medications/{fixedMedicationId:int}/give")]
        public async Task<IActionResult> GiveFixedMedication(
            int citizenId,
            int fixedMedicationId,
            [FromBody] RegisterFixedMedicationGivenRequest? request,
            CancellationToken cancellationToken)
        {
            try
            {
                if (request != null && !ModelState.IsValid)
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
                    var updated = await _fixedMedicationService.GiveAsync(
                        citizenId,
                        fixedMedicationId,
                        access.DepartmentId!.Value,
                        access.UserId!,
                        request?.GivenAt,
                        cancellationToken,
                        access.UserDisplayNameSnapshot);

                    if (updated == null)
                    {
                        return NotFound();
                    }

                    return Ok(updated);
                }
                catch (ArgumentException ex)
                {
                    return BadRequest(ex.Message);
                }
                catch (InvalidOperationException ex)
                {
                    return Conflict(ex.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while registering fixed medication as given");
                return StatusCode(500);
            }
        }

        [HttpPost("{citizenId:int}/fixed-medications/{fixedMedicationId:int}/cancel")]
        public async Task<IActionResult> CancelFixedMedication(
            int citizenId,
            int fixedMedicationId,
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
                    var updated = await _fixedMedicationService.CancelGivenAsync(
                        citizenId,
                        fixedMedicationId,
                        access.DepartmentId!.Value,
                        access.UserId!,
                        cancellationToken,
                        access.UserDisplayNameSnapshot);

                    if (updated == null)
                    {
                        return NotFound();
                    }

                    return Ok(updated);
                }
                catch (ArgumentException ex)
                {
                    return BadRequest(ex.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while cancelling fixed medication registration");
                return StatusCode(500);
            }
        }

        [HttpPut("{citizenId:int}/fixed-medications/{fixedMedicationId:int}/plan")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Leder,Vagtansvarlig")]
        public async Task<IActionResult> UpdateFixedMedicationPlan(
            int citizenId,
            int fixedMedicationId,
            [FromBody] UpdateFixedMedicationPlanRequest request,
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

                if (!User.IsInRole("Leder") && !User.IsInRole("Vagtansvarlig"))
                {
                    return Forbid();
                }

                try
                {
                    var updated = await _fixedMedicationService.UpdatePlanAsync(
                        citizenId,
                        fixedMedicationId,
                        access.DepartmentId!.Value,
                        request,
                        cancellationToken);

                    if (updated == null)
                    {
                        return NotFound();
                    }

                    return Ok(updated);
                }
                catch (ArgumentException ex)
                {
                    return BadRequest(ex.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating fixed medication plan");
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
