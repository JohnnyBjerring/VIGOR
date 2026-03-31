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

        public CitizensApiController(ICitizenService citizenService, AppDbContext context)
        {
            _citizenService = citizenService;
            _context = context;
        }

        // GET api/citizens/by-department/{departmentId}
        [HttpGet("by-department/{departmentId}")]
        public async Task<IActionResult> GetCitizensByDepartment(int departmentId, CancellationToken cancellationToken)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null) return Unauthorized();

                var employee = await _context.Employees
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.IdentityUserId == userId, cancellationToken);

                if (employee == null || employee.DepartmentId == null)
                    return Forbid();

                // Filtrering sker server-side: vi ignorerer det indsendte departmentId 
                // for at fastholde sikkerheden omkring medarbejderens egen afdeling.
                var safeDepartmentId = employee.DepartmentId.Value;

                var citizens = await _citizenService.GetCitizensByDepartmentAsync(safeDepartmentId, cancellationToken);
                return Ok(citizens);
            }
            catch
            {
                return StatusCode(500);
            }
        }
    }
}
