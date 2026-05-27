using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VIGOR.Shared.Enums;
using VIGOR.Web.Data;
using VIGOR.Web.Services;

namespace VIGOR.Web.Controllers.Api
{
    [ApiController]
    [Route("api/overlap")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Leder,Vagtansvarlig,Personale")]
    public class OverlapApiController : ControllerBase
    {
        private readonly IOverlapService _overlapService;
        private readonly AppDbContext _context;
        private readonly ILogger<OverlapApiController> _logger;

        public OverlapApiController(
            IOverlapService overlapService,
            AppDbContext context,
            ILogger<OverlapApiController> logger)
        {
            _overlapService = overlapService;
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetOverlap(
            [FromQuery] ShiftType? shiftType,
            CancellationToken cancellationToken)
        {
            try
            {
                if (shiftType.HasValue && !Enum.IsDefined(typeof(ShiftType), shiftType.Value))
                {
                    return BadRequest("Vagttype er ugyldig. Vælg dagvagt, aftenvagt eller nattevagt.");
                }

                var access = await ResolveEmployeeAccessAsync(cancellationToken);
                if (access.Result != null)
                {
                    return access.Result;
                }

                var overlap = await _overlapService.GetOverlapAsync(
                    access.DepartmentId!.Value,
                    shiftType,
                    cancellationToken);

                if (overlap == null)
                {
                    return NotFound();
                }

                return Ok(overlap);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while getting overlap");
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
