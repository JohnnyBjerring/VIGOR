using System.Net.Http.Json;
using VIGOR.Shared.DTOs;
using VIGOR.Shared.Enums;
using VIGOR.Shared.Interfaces.Services;
using VIGOR.Shared.Models;

namespace VIGOR.Shared.Services
{
    // Minimal client-side ICitizenService used by the WebAssembly/MAUI client to call the existing Web API.
    // Keeps logic simple for iteration 2: only implements GetCitizensByDepartmentAsync.
    public class CitizenClientService : ICitizenService
    {
        private readonly HttpClient _http;
        private readonly System.Text.Json.JsonSerializerOptions _jsonOptions;

        public CitizenClientService(HttpClient http)
        {
            _http = http;
            _jsonOptions = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            };
        }

        public async Task<IEnumerable<Citizen>> GetCitizensByDepartmentAsync(int departmentId, CancellationToken cancellationToken = default)
        {
            var url = "api/citizens";
            var result = await _http.GetFromJsonAsync<IEnumerable<Citizen>>(url, _jsonOptions, cancellationToken);
            return result ?? Enumerable.Empty<Citizen>();
        }

        public async Task<Citizen?> UpdateCitizenStatusAsync(int citizenId, int departmentId, CitizenStatus status, CancellationToken cancellationToken = default)
        {
            var request = new UpdateCitizenStatusRequest { Status = status };
            var response = await _http.PostAsJsonAsync($"api/citizens/{citizenId}/status", request, _jsonOptions, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<Citizen>(_jsonOptions, cancellationToken: cancellationToken);
        }
    }
}
