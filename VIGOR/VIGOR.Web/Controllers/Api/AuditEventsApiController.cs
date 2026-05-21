using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VIGOR.Web.Data;
using VIGOR.Web.Services;

namespace VIGOR.Web.Controllers.Api
{
    /// <summary>
    /// UC06: Simpelt læse-endpoint til audit-events for en borger.
    /// Selve audit-events oprettes server-side af de relevante services og ikke direkte af klienten.
    /// </summary>
    [ApiController]
    [Route("api/citizens/{citizenId:int}/audit-events")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Leder,Vagtansvarlig,Personale")]
    public class AuditEventsApiController : ControllerBase
    {
        private readonly IAuditService _auditService;
        private readonly AppDbContext _context;
        private readonly ILogger<AuditEventsApiController> _logger;

        public AuditEventsApiController(
            IAuditService auditService,
            AppDbContext context,
            ILogger<AuditEventsApiController> logger)
        {
            _auditService = auditService;
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAuditEvents(int citizenId, CancellationToken cancellationToken)
        {
            try
            {
                var access = await ResolveEmployeeAccessAsync(cancellationToken);
                if (access.Result != null)
                {
                    return access.Result;
                }

                var events = await _auditService.GetForCitizenAsync(citizenId, access.DepartmentId!.Value, cancellationToken);
                if (events == null)
                {
                    return NotFound();
                }

                return Ok(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while getting audit events");
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

            return EmployeeAccessResult.Allowed(employee.DepartmentId.Value);
        }

        private sealed class EmployeeAccessResult
        {
            private EmployeeAccessResult(int? departmentId, IActionResult? result)
            {
                DepartmentId = departmentId;
                Result = result;
            }

            public int? DepartmentId { get; }
            public IActionResult? Result { get; }

            public static EmployeeAccessResult Allowed(int departmentId)
            {
                return new EmployeeAccessResult(departmentId, null);
            }

            public static EmployeeAccessResult Unauthorized()
            {
                return new EmployeeAccessResult(null, new UnauthorizedResult());
            }

            public static EmployeeAccessResult Forbidden()
            {
                return new EmployeeAccessResult(null, new ForbidResult());
            }
        }
    }
}
