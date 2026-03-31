using System.Net.Http.Json;
using VIGOR.Shared.Interfaces.Services;
using VIGOR.Shared.Models;

namespace VIGOR.Shared.Services
{
    // Minimal client-side ICitizenService used by the WebAssembly/MAUI client to call the existing Web API.
    // Keeps logic simple for iteration 2: only implements GetCitizensByDepartmentAsync.
    public class CitizenClientService : ICitizenService
    {
        private readonly HttpClient _http;

        public CitizenClientService(HttpClient http)
        {
            _http = http;
        }

        public async Task<IEnumerable<Citizen>> GetCitizensByDepartmentAsync(int departmentId, CancellationToken cancellationToken = default)
        {
            var url = $"api/citizens/by-department/{departmentId}";
            var result = await _http.GetFromJsonAsync<IEnumerable<Citizen>>(url, cancellationToken);
            return result ?? Enumerable.Empty<Citizen>();
        }
    }
}
