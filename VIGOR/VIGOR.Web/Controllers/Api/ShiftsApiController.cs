using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VIGOR.Shared.DTOs;
using VIGOR.Shared.Enums;
using VIGOR.Web.Data;
using VIGOR.Web.Services;

namespace VIGOR.Web.Controllers.Api
{
    [ApiController]
    [Route("api/shifts")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Leder,Vagtansvarlig,Personale")]
    public class ShiftsApiController : ControllerBase
    {
        private readonly IShiftSelectionService _shiftSelectionService;
        private readonly AppDbContext _context;
        private readonly ILogger<ShiftsApiController> _logger;

        public ShiftsApiController(
            IShiftSelectionService shiftSelectionService,
            AppDbContext context,
            ILogger<ShiftsApiController> logger)
        {
            _shiftSelectionService = shiftSelectionService;
            _context = context;
            _logger = logger;
        }

        [HttpPost("select")]
        public async Task<IActionResult> SelectShift(
            [FromBody] SelectShiftRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                if (request == null || !ModelState.IsValid)
                {
                    return BadRequest();
                }

                if (!Enum.IsDefined(typeof(ShiftType), request.ShiftType))
                {
                    return BadRequest("Den valgte vagttype er ugyldig.");
                }

                var access = await ResolveEmployeeAccessAsync(cancellationToken);
                if (access.Result != null)
                {
                    return access.Result;
                }

                var shiftContext = await _shiftSelectionService.SelectShiftAsync(
                    request.ShiftType,
                    access.UserId!,
                    access.DepartmentId!.Value,
                    cancellationToken);

                return Ok(shiftContext);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while selecting active shift context");
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

            return EmployeeAccessResult.Allowed(userId, employee.DepartmentId.Value);
        }

        private sealed class EmployeeAccessResult
        {
            private EmployeeAccessResult(string? userId, int? departmentId, IActionResult? result)
            {
                UserId = userId;
                DepartmentId = departmentId;
                Result = result;
            }

            public string? UserId { get; }
            public int? DepartmentId { get; }
            public IActionResult? Result { get; }

            public static EmployeeAccessResult Allowed(string userId, int departmentId)
            {
                return new EmployeeAccessResult(userId, departmentId, null);
            }

            public static EmployeeAccessResult Unauthorized()
            {
                return new EmployeeAccessResult(null, null, new UnauthorizedResult());
            }

            public static EmployeeAccessResult Forbidden()
            {
                return new EmployeeAccessResult(null, null, new ForbidResult());
            }
        }
    }
}
