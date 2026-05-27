using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VIGOR.Web.Services;

namespace VIGOR.Web.Controllers.Api
{
    [ApiController]
    [Route("api/public/overview")]
    [AllowAnonymous]
    public class PublicOverviewApiController : ControllerBase
    {
        private readonly IPublicOverviewService _publicOverviewService;
        private readonly ILogger<PublicOverviewApiController> _logger;

        public PublicOverviewApiController(
            IPublicOverviewService publicOverviewService,
            ILogger<PublicOverviewApiController> logger)
        {
            _publicOverviewService = publicOverviewService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetPublicOverview(CancellationToken cancellationToken)
        {
            try
            {
                var overview = await _publicOverviewService.GetPublicOverviewAsync(cancellationToken);
                return Ok(overview);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while getting public overview");
                return StatusCode(500);
            }
        }
    }
}
