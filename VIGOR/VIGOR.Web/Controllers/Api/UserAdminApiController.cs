using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VIGOR.Shared.DTOs;
using VIGOR.Web.Services;

namespace VIGOR.Web.Controllers.Api
{
    [ApiController]
    [Route("api/admin/users")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Leder,Superbruger")]
    public class UserAdminApiController : ControllerBase
    {
        private readonly IUserAdminService _userAdminService;
        private readonly ILogger<UserAdminApiController> _logger;

        public UserAdminApiController(
            IUserAdminService userAdminService,
            ILogger<UserAdminApiController> logger)
        {
            _userAdminService = userAdminService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
        {
            var accessResult = ResolveAdminAccess();
            if (accessResult != null)
            {
                return accessResult;
            }

            try
            {
                var users = await _userAdminService.GetUsersAsync(cancellationToken);
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while getting users");
                return StatusCode(500);
            }
        }

        [HttpGet("roles")]
        public async Task<IActionResult> GetRoles(CancellationToken cancellationToken)
        {
            var accessResult = ResolveAdminAccess();
            if (accessResult != null)
            {
                return accessResult;
            }

            try
            {
                var roles = await _userAdminService.GetRolesAsync(cancellationToken);
                return Ok(roles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while getting roles");
                return StatusCode(500);
            }
        }

        [HttpGet("departments")]
        public async Task<IActionResult> GetDepartments(CancellationToken cancellationToken)
        {
            var accessResult = ResolveAdminAccess();
            if (accessResult != null)
            {
                return accessResult;
            }

            try
            {
                var departments = await _userAdminService.GetDepartmentsAsync(cancellationToken);
                return Ok(departments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while getting departments");
                return StatusCode(500);
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser(
            [FromBody] CreateUserAdminUserRequest request,
            CancellationToken cancellationToken)
        {
            var accessResult = ResolveAdminAccess();
            if (accessResult != null)
            {
                return accessResult;
            }

            if (request == null || !ModelState.IsValid)
            {
                return BadRequest();
            }

            try
            {
                var created = await _userAdminService.CreateUserAsync(request, GetCurrentRoleNames(), cancellationToken);
                return CreatedAtAction(nameof(GetUsers), new { userId = created.UserId }, created);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating user");
                return StatusCode(500);
            }
        }

        [HttpPut("{userId}/role")]
        public async Task<IActionResult> UpdateRole(
            string userId,
            [FromBody] UpdateUserRoleRequest request,
            CancellationToken cancellationToken)
        {
            var accessResult = ResolveAdminAccess();
            if (accessResult != null)
            {
                return accessResult;
            }

            if (request == null || !ModelState.IsValid)
            {
                return BadRequest();
            }

            try
            {
                var currentUserId = GetCurrentUserId();
                if (string.IsNullOrWhiteSpace(currentUserId))
                {
                    return Unauthorized();
                }

                var updated = await _userAdminService.UpdateRoleAsync(userId, request, currentUserId, GetCurrentRoleNames(), cancellationToken);
                return updated == null ? NotFound() : Ok(updated);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating user role");
                return StatusCode(500);
            }
        }

        [HttpPut("{userId}/active")]
        public async Task<IActionResult> SetActive(
            string userId,
            [FromBody] SetUserActiveRequest request,
            CancellationToken cancellationToken)
        {
            var accessResult = ResolveAdminAccess();
            if (accessResult != null)
            {
                return accessResult;
            }

            if (request == null || !ModelState.IsValid)
            {
                return BadRequest();
            }

            try
            {
                var currentUserId = GetCurrentUserId();
                if (string.IsNullOrWhiteSpace(currentUserId))
                {
                    return Unauthorized();
                }

                var updated = await _userAdminService.SetActiveAsync(userId, request, currentUserId, GetCurrentRoleNames(), cancellationToken);
                return updated == null ? NotFound() : Ok(updated);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating user active state");
                return StatusCode(500);
            }
        }

        private string? GetCurrentUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        private IReadOnlyCollection<string> GetCurrentRoleNames()
        {
            return User.FindAll(ClaimTypes.Role)
                .Select(claim => claim.Value)
                .Where(roleName => !string.IsNullOrWhiteSpace(roleName))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private IActionResult? ResolveAdminAccess()
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            if (!User.IsInRole("Leder") && !User.IsInRole("Superbruger"))
            {
                return Forbid();
            }

            return null;
        }
    }
}
