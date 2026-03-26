using Microsoft.AspNetCore.Mvc;
using VIGOR.Shared.Interfaces.Services;

namespace VIGOR.Web.Controllers.Api
{
    [ApiController]
    [Route("api/citizens")]
    public class CitizensApiController : ControllerBase
    {
        private readonly ICitizenService _citizenService;

        public CitizensApiController(ICitizenService citizenService)
        {
            _citizenService = citizenService;
        }

        // GET api/citizens/by-department/{departmentId}
        [HttpGet("by-department/{departmentId}")]
        public async Task<IActionResult> GetCitizensByDepartment(int departmentId)
        {
            var citizens = await _citizenService.GetCitizensByDepartmentAsync(departmentId);
            return Ok(citizens);
        }
    }
}
