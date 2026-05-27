using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using VIGOR.Shared.DTOs;
using VIGOR.Shared.Interfaces.Services;

namespace VIGOR.Shared.Services
{
    /// <summary>
    /// Client-side implementation til UC11 personale-tildelingsendpoints.
    /// </summary>
    public class StaffAssignmentClientService : IStaffAssignmentApi
    {
        private readonly HttpClient _http;
        private readonly JsonSerializerOptions _jsonOptions;

        public StaffAssignmentClientService(HttpClient http)
        {
            _http = http;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };
        }

        public async Task<IReadOnlyList<CitizenStaffAssignmentDto>?> GetAssignmentsForCitizenAsync(
            int citizenId,
            CancellationToken cancellationToken = default)
        {
            var response = await _http.GetAsync($"api/citizens/{citizenId}/staff-assignments", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var serverMessage = await ReadServerMessageAsync(response, cancellationToken);

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new InvalidOperationException("Der blev ikke fundet personaletildelinger for den valgte borger eller afdeling.");
                }

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    throw new InvalidOperationException("Du har ikke adgang til personaletildelinger for den valgte borger.");
                }

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new InvalidOperationException("Du er ikke logget ind, eller din session er udløbet.");
                }

                throw new InvalidOperationException($"Personaletildelinger kunne ikke hentes. Serveren svarede: {serverMessage}");
            }

            return await response.Content.ReadFromJsonAsync<List<CitizenStaffAssignmentDto>>(_jsonOptions, cancellationToken)
                   ?? new List<CitizenStaffAssignmentDto>();
        }

        public async Task<IReadOnlyList<AssignableStaffDto>?> GetAssignableStaffAsync(
            int citizenId,
            CancellationToken cancellationToken = default)
        {
            var response = await _http.GetAsync($"api/citizens/{citizenId}/staff-assignments/available-staff", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var serverMessage = await ReadServerMessageAsync(response, cancellationToken);

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new InvalidOperationException("Borgeren blev ikke fundet i din afdeling.");
                }

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    throw new InvalidOperationException("Du har ikke adgang til personaleoversigten for den valgte borger.");
                }

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new InvalidOperationException("Du er ikke logget ind, eller din session er udløbet.");
                }

                throw new InvalidOperationException($"Personale kunne ikke hentes. Serveren svarede: {serverMessage}");
            }

            return await response.Content.ReadFromJsonAsync<List<AssignableStaffDto>>(_jsonOptions, cancellationToken)
                   ?? new List<AssignableStaffDto>();
        }

        public async Task<CitizenStaffAssignmentDto?> AssignStaffAsync(
            int citizenId,
            AssignStaffToCitizenRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var response = await _http.PostAsJsonAsync(
                $"api/citizens/{citizenId}/staff-assignments",
                request,
                _jsonOptions,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var serverMessage = await ReadServerMessageAsync(response, cancellationToken);

                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    throw new InvalidOperationException($"Personale kunne ikke tildeles: {serverMessage}");
                }

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new InvalidOperationException("Borgeren eller medarbejderen blev ikke fundet i din afdeling.");
                }

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    throw new InvalidOperationException("Du har ikke adgang til at tildele personale til borgeren.");
                }

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new InvalidOperationException("Du er ikke logget ind, eller din session er udløbet.");
                }

                throw new InvalidOperationException($"Personale kunne ikke tildeles. Serveren svarede: {serverMessage}");
            }

            return await response.Content.ReadFromJsonAsync<CitizenStaffAssignmentDto>(_jsonOptions, cancellationToken: cancellationToken);
        }

        public async Task<CitizenStaffAssignmentDto?> UnassignStaffAsync(
            int citizenId,
            int citizenStaffAssignmentId,
            CancellationToken cancellationToken = default)
        {
            var response = await _http.PostAsJsonAsync(
                $"api/citizens/{citizenId}/staff-assignments/{citizenStaffAssignmentId}/unassign",
                new { },
                _jsonOptions,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var serverMessage = await ReadServerMessageAsync(response, cancellationToken);

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new InvalidOperationException("Tildelingen blev ikke fundet for den valgte borger og afdeling.");
                }

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    throw new InvalidOperationException("Du har ikke adgang til at fjerne personaletildelingen.");
                }

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new InvalidOperationException("Du er ikke logget ind, eller din session er udløbet.");
                }

                throw new InvalidOperationException($"Personale kunne ikke fjernes fra borgeren. Serveren svarede: {serverMessage}");
            }

            return await response.Content.ReadFromJsonAsync<CitizenStaffAssignmentDto>(_jsonOptions, cancellationToken: cancellationToken);
        }

        private static async Task<string> ReadServerMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(content))
            {
                return response.ReasonPhrase ?? "Ukendt serverfejl.";
            }

            content = content.Trim();

            if (TryReadProblemDetailsMessage(content, out var problemDetailsMessage))
            {
                return problemDetailsMessage;
            }

            return content.Trim('"');
        }

        private static bool TryReadProblemDetailsMessage(string content, out string message)
        {
            message = string.Empty;

            if (!content.StartsWith('{') && !content.StartsWith('"'))
            {
                return false;
            }

            try
            {
                using var document = JsonDocument.Parse(content);
                var root = document.RootElement;

                if (root.ValueKind == JsonValueKind.String)
                {
                    message = root.GetString() ?? string.Empty;
                    return !string.IsNullOrWhiteSpace(message);
                }

                if (root.TryGetProperty("detail", out var detail) && detail.ValueKind == JsonValueKind.String)
                {
                    message = detail.GetString() ?? string.Empty;
                    return !string.IsNullOrWhiteSpace(message);
                }

                if (root.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String)
                {
                    message = title.GetString() ?? string.Empty;
                    return !string.IsNullOrWhiteSpace(message);
                }

                if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                {
                    foreach (var property in errors.EnumerateObject())
                    {
                        if (property.Value.ValueKind != JsonValueKind.Array)
                        {
                            continue;
                        }

                        foreach (var error in property.Value.EnumerateArray())
                        {
                            if (error.ValueKind != JsonValueKind.String)
                            {
                                continue;
                            }

                            message = error.GetString() ?? string.Empty;
                            return !string.IsNullOrWhiteSpace(message);
                        }
                    }
                }
            }
            catch (JsonException)
            {
                return false;
            }

            return false;
        }
    }
}
