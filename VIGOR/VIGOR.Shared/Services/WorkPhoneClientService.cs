using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using VIGOR.Shared.DTOs;
using VIGOR.Shared.Interfaces.Services;

namespace VIGOR.Shared.Services
{
    /// <summary>
    /// Client-side implementation til UC12 arbejdstelefon-endpoints.
    /// </summary>
    public class WorkPhoneClientService : IWorkPhoneApi
    {
        private readonly HttpClient _http;
        private readonly JsonSerializerOptions _jsonOptions;

        public WorkPhoneClientService(HttpClient http)
        {
            _http = http;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };
        }

        public async Task<IReadOnlyList<WorkPhoneDto>> GetPhonesAsync(CancellationToken cancellationToken = default)
        {
            var response = await _http.GetAsync("api/admin/work-phones", cancellationToken);
            await EnsureSuccessAsync(response, "Arbejdstelefoner kunne ikke hentes.", cancellationToken);

            return await response.Content.ReadFromJsonAsync<List<WorkPhoneDto>>(_jsonOptions, cancellationToken)
                   ?? new List<WorkPhoneDto>();
        }

        public async Task<IReadOnlyList<PhoneAssignmentDto>> GetActiveAssignmentsAsync(CancellationToken cancellationToken = default)
        {
            var response = await _http.GetAsync("api/admin/work-phones/assignments", cancellationToken);
            await EnsureSuccessAsync(response, "Telefontildelinger kunne ikke hentes.", cancellationToken);

            return await response.Content.ReadFromJsonAsync<List<PhoneAssignmentDto>>(_jsonOptions, cancellationToken)
                   ?? new List<PhoneAssignmentDto>();
        }

        public async Task<IReadOnlyList<PhoneAssignableEmployeeDto>> GetAssignableEmployeesAsync(CancellationToken cancellationToken = default)
        {
            var response = await _http.GetAsync("api/admin/work-phones/employees", cancellationToken);
            await EnsureSuccessAsync(response, "Medarbejdere til telefontildeling kunne ikke hentes.", cancellationToken);

            return await response.Content.ReadFromJsonAsync<List<PhoneAssignableEmployeeDto>>(_jsonOptions, cancellationToken)
                   ?? new List<PhoneAssignableEmployeeDto>();
        }

        public async Task<WorkPhoneDto?> CreatePhoneAsync(
            CreateWorkPhoneRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var response = await _http.PostAsJsonAsync("api/admin/work-phones", request, _jsonOptions, cancellationToken);
            await EnsureSuccessAsync(response, "Arbejdstelefonen kunne ikke oprettes.", cancellationToken);

            return await response.Content.ReadFromJsonAsync<WorkPhoneDto>(_jsonOptions, cancellationToken);
        }

        public async Task<PhoneAssignmentDto?> AssignPhoneAsync(
            AssignWorkPhoneRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var response = await _http.PostAsJsonAsync("api/admin/work-phones/assignments", request, _jsonOptions, cancellationToken);
            await EnsureSuccessAsync(response, "Arbejdstelefonen kunne ikke tildeles.", cancellationToken);

            return await response.Content.ReadFromJsonAsync<PhoneAssignmentDto>(_jsonOptions, cancellationToken);
        }

        public async Task<PhoneAssignmentDto?> UnassignPhoneAsync(
            int phoneAssignmentId,
            CancellationToken cancellationToken = default)
        {
            var response = await _http.PostAsJsonAsync(
                $"api/admin/work-phones/assignments/{phoneAssignmentId}/unassign",
                new { },
                _jsonOptions,
                cancellationToken);

            await EnsureSuccessAsync(response, "Arbejdstelefonen kunne ikke fjernes fra medarbejderen.", cancellationToken);

            return await response.Content.ReadFromJsonAsync<PhoneAssignmentDto>(_jsonOptions, cancellationToken);
        }

        private static async Task EnsureSuccessAsync(HttpResponseMessage response, string fallbackMessage, CancellationToken cancellationToken)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            var serverMessage = await ReadServerMessageAsync(response, cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new InvalidOperationException("Du er ikke logget ind, eller din session er udløbet.");
            }

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new InvalidOperationException("Du har ikke adgang til arbejdstelefoner.");
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new InvalidOperationException("Arbejdstelefonen, medarbejderen eller tildelingen blev ikke fundet.");
            }

            throw new InvalidOperationException($"{fallbackMessage} Serveren svarede: {serverMessage}");
        }

        private static async Task<string> ReadServerMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(content))
            {
                return response.ReasonPhrase ?? "Ukendt serverfejl.";
            }

            content = content.Trim();

            try
            {
                using var document = JsonDocument.Parse(content);
                var root = document.RootElement;

                if (root.ValueKind == JsonValueKind.String)
                {
                    return root.GetString() ?? string.Empty;
                }

                if (root.TryGetProperty("detail", out var detail) && detail.ValueKind == JsonValueKind.String)
                {
                    return detail.GetString() ?? string.Empty;
                }

                if (root.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String)
                {
                    return title.GetString() ?? string.Empty;
                }
            }
            catch (JsonException)
            {
                // Fall back to raw content.
            }

            return content.Trim('"');
        }
    }
}
