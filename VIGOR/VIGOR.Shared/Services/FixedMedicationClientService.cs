using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using VIGOR.Shared.DTOs;
using VIGOR.Shared.Interfaces.Services;

namespace VIGOR.Shared.Services
{
    /// <summary>
    /// Client-side implementation til UC04 endpoints.
    /// </summary>
    public class FixedMedicationClientService : IFixedMedicationApi
    {
        private readonly HttpClient _http;
        private readonly JsonSerializerOptions _jsonOptions;

        public FixedMedicationClientService(HttpClient http)
        {
            _http = http;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<IReadOnlyList<FixedMedicationDto>?> GetFixedMedicationsForCitizenAsync(
            int citizenId,
            CancellationToken cancellationToken = default)
        {
            var response = await _http.GetAsync($"api/citizens/{citizenId}/fixed-medications", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var serverMessage = await ReadServerMessageAsync(response, cancellationToken);

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new InvalidOperationException("Der blev ikke fundet en medicinplan for den valgte borger eller afdeling.");
                }

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    throw new InvalidOperationException("Du har ikke adgang til medicinplanen for den valgte borger.");
                }

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new InvalidOperationException("Du er ikke logget ind, eller din session er udløbet.");
                }

                throw new InvalidOperationException($"Fast medicin kunne ikke hentes. Serveren svarede: {serverMessage}");
            }

            return await response.Content.ReadFromJsonAsync<List<FixedMedicationDto>>(_jsonOptions, cancellationToken)
                   ?? new List<FixedMedicationDto>();
        }

        public async Task<FixedMedicationDto?> RegisterFixedMedicationGivenAsync(
            int citizenId,
            int fixedMedicationId,
            DateTime? givenAt,
            CancellationToken cancellationToken = default)
        {
            var request = new RegisterFixedMedicationGivenRequest { GivenAt = givenAt };
            var response = await _http.PostAsJsonAsync(
                $"api/citizens/{citizenId}/fixed-medications/{fixedMedicationId}/give",
                request,
                _jsonOptions,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var serverMessage = await ReadServerMessageAsync(response, cancellationToken);

                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    throw new InvalidOperationException($"Serveren afviste tidspunktet: {serverMessage}");
                }

                if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    throw new InvalidOperationException($"Medicinen kunne ikke registreres: {serverMessage}");
                }

                throw new InvalidOperationException($"Medicinen kunne ikke registreres. Serveren svarede: {serverMessage}");
            }

            return await response.Content.ReadFromJsonAsync<FixedMedicationDto>(_jsonOptions, cancellationToken: cancellationToken);
        }

        public async Task<FixedMedicationDto?> CancelFixedMedicationGivenAsync(
            int citizenId,
            int fixedMedicationId,
            CancellationToken cancellationToken = default)
        {
            var response = await _http.PostAsync(
                $"api/citizens/{citizenId}/fixed-medications/{fixedMedicationId}/cancel",
                content: null,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var serverMessage = await ReadServerMessageAsync(response, cancellationToken);

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new InvalidOperationException("Medicinen blev ikke fundet for den valgte borger eller afdeling.");
                }

                throw new InvalidOperationException($"Medicinen kunne ikke annulleres. Serveren svarede: {serverMessage}");
            }

            return await response.Content.ReadFromJsonAsync<FixedMedicationDto>(_jsonOptions, cancellationToken: cancellationToken);
        }

        public async Task<FixedMedicationDto?> UpdateFixedMedicationPlanAsync(
            int citizenId,
            int fixedMedicationId,
            UpdateFixedMedicationPlanRequest request,
            CancellationToken cancellationToken = default)
        {
            var response = await _http.PutAsJsonAsync(
                $"api/citizens/{citizenId}/fixed-medications/{fixedMedicationId}/plan",
                request,
                _jsonOptions,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var serverMessage = await ReadServerMessageAsync(response, cancellationToken);

                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    throw new InvalidOperationException($"Medicinplanen kunne ikke gemmes: {serverMessage}");
                }

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new InvalidOperationException("Medicinplanen blev ikke fundet for den valgte borger eller afdeling.");
                }

                throw new InvalidOperationException($"Medicinplanen kunne ikke gemmes. Serveren svarede: {serverMessage}");
            }

            return await response.Content.ReadFromJsonAsync<FixedMedicationDto>(_jsonOptions, cancellationToken: cancellationToken);
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
