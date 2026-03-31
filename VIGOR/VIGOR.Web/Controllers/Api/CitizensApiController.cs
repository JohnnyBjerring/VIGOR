using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using VIGOR.Shared.Interfaces.Services;

namespace VIGOR.Web.Controllers.Api
{
    [ApiController]
    [Route("api/citizens")]
    [Authorize(Roles = "Leder,Vagtansvarlig,Personale")]
    public class CitizensApiController : ControllerBase
    {
        private readonly ICitizenService _citizenService;

        public CitizensApiController(ICitizenService citizenService)
        {
            _citizenService = citizenService;
        }

        // GET api/citizens/by-department/{departmentId}
        [HttpGet("by-department/{departmentId}")]
        public async Task<IActionResult> GetCitizensByDepartment(int departmentId, CancellationToken cancellationToken)
        {
            try
            {
                var citizens = await _citizenService.GetCitizensByDepartmentAsync(departmentId, cancellationToken);
                return Ok(citizens);
            }
            catch
            {
                return StatusCode(500);
            }
        }
    }
}
