using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading;
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

        // GET api/citizens/by-department/{departmentId}
        [HttpGet("by-department/{departmentId}")]
        public async Task<IActionResult> GetCitizensByDepartment(int departmentId, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogWarning("CitizensApiController.GetCitizensByDepartment REACHED! Claims = {Claims}", string.Join(", ", User.Claims.Select(c => c.Type)));
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

                // Filtrering sker server-side: vi ignorerer det indsendte departmentId 
                // for at fastholde sikkerheden omkring medarbejderens egen afdeling.
                var safeDepartmentId = employee.DepartmentId.Value;

                var citizens = await _citizenService.GetCitizensByDepartmentAsync(safeDepartmentId, cancellationToken);
                return Ok(citizens);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while getting citizens by department");
                return StatusCode(500);
            }
        }
    }
}

