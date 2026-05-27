using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VIGOR.Shared.DTOs;
using VIGOR.Web.Services;

namespace VIGOR.Web.Controllers.Api
{
    [ApiController]
    [Route("api/admin/work-phones")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Leder,Vagtansvarlig,Superbruger")]
    public class WorkPhonesApiController : ControllerBase
    {
        private readonly IWorkPhoneService _workPhoneService;
        private readonly ILogger<WorkPhonesApiController> _logger;

        public WorkPhonesApiController(
            IWorkPhoneService workPhoneService,
            ILogger<WorkPhonesApiController> logger)
        {
            _workPhoneService = workPhoneService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetPhones(CancellationToken cancellationToken)
        {
            try
            {
                var phones = await _workPhoneService.GetPhonesAsync(cancellationToken);
                return Ok(phones);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while getting work phones");
                return StatusCode(500);
            }
        }

        [HttpGet("assignments")]
        [Authorize(Roles = "Leder,Vagtansvarlig")]
        public async Task<IActionResult> GetActiveAssignments(CancellationToken cancellationToken)
        {
            try
            {
                var assignments = await _workPhoneService.GetActiveAssignmentsAsync(cancellationToken);
                return Ok(assignments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while getting active phone assignments");
                return StatusCode(500);
            }
        }

        [HttpGet("employees")]
        [Authorize(Roles = "Leder,Vagtansvarlig")]
        public async Task<IActionResult> GetAssignableEmployees(CancellationToken cancellationToken)
        {
            try
            {
                var employees = await _workPhoneService.GetAssignableEmployeesAsync(cancellationToken);
                return Ok(employees);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while getting employees for phone assignment");
                return StatusCode(500);
            }
        }

        [HttpPost]
        [Authorize(Roles = "Leder,Superbruger")]
        public async Task<IActionResult> CreatePhone(
            [FromBody] CreateWorkPhoneRequest request,
            CancellationToken cancellationToken)
        {
            if (request == null || !ModelState.IsValid)
            {
                return BadRequest();
            }

            try
            {
                var created = await _workPhoneService.CreatePhoneAsync(request, cancellationToken);
                return CreatedAtAction(nameof(GetPhones), new { workPhoneId = created.WorkPhoneId }, created);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating work phone");
                return StatusCode(500);
            }
        }

        [HttpPost("assignments")]
        [Authorize(Roles = "Leder,Vagtansvarlig")]
        public async Task<IActionResult> AssignPhone(
            [FromBody] AssignWorkPhoneRequest request,
            CancellationToken cancellationToken)
        {
            if (request == null || !ModelState.IsValid)
            {
                return BadRequest();
            }

            var userId = GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            try
            {
                var assigned = await _workPhoneService.AssignPhoneAsync(request, userId, cancellationToken);
                return assigned == null ? NotFound() : CreatedAtAction(nameof(GetActiveAssignments), new { phoneAssignmentId = assigned.PhoneAssignmentId }, assigned);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while assigning work phone");
                return StatusCode(500);
            }
        }

        [HttpPost("assignments/{phoneAssignmentId:int}/unassign")]
        [Authorize(Roles = "Leder,Vagtansvarlig")]
        public async Task<IActionResult> UnassignPhone(
            int phoneAssignmentId,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            try
            {
                var unassigned = await _workPhoneService.UnassignPhoneAsync(phoneAssignmentId, userId, cancellationToken);
                return unassigned == null ? NotFound() : Ok(unassigned);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while unassigning work phone");
                return StatusCode(500);
            }
        }

        private string? GetCurrentUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }
}
