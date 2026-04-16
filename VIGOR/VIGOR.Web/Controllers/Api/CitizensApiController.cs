using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading;
using VIGOR.Shared.DTOs;
using VIGOR.Shared.Enums;
using VIGOR.Shared.Interfaces.Services;
using VIGOR.Web.Data;

namespace VIGOR.Web.Controllers.Api
{
    [ApiController]
    [Route("api/citizens")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Leder,Vagtansvarlig,Personale")]
    public class CitizensApiController : ControllerBase
    {
        private readonly ICitizenService _citizenService;
        private readonly AppDbContext _context;
        private readonly ILogger<CitizensApiController> _logger;

        public CitizensApiController(ICitizenService citizenService, AppDbContext context, ILogger<CitizensApiController> logger)
        {
            _citizenService = citizenService;
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

                var updatedCitizen = await _citizenService.UpdateCitizenStatusAsync(citizenId, employee.DepartmentId.Value, request.Status, cancellationToken);

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
    }
}
