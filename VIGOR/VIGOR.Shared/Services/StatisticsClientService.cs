using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using VIGOR.Shared.DTOs;
using VIGOR.Shared.Interfaces.Services;

namespace VIGOR.Shared.Services
{
    /// <summary>
    /// Client-side implementation til UC15 statistik-endpoint.
    /// </summary>
    public class StatisticsClientService : IStatisticsApi
    {
        private readonly HttpClient _http;
        private readonly JsonSerializerOptions _jsonOptions;

        public StatisticsClientService(HttpClient http)
        {
            _http = http;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };
        }

        public async Task<StatisticsOverviewDto?> GetStatisticsAsync(
            DateTime? fromUtc = null,
            DateTime? toUtc = null,
            CancellationToken cancellationToken = default)
        {
            var query = new List<string>();
            if (fromUtc.HasValue)
            {
                query.Add($"fromUtc={Uri.EscapeDataString(fromUtc.Value.ToUniversalTime().ToString("O"))}");
            }

            if (toUtc.HasValue)
            {
                query.Add($"toUtc={Uri.EscapeDataString(toUtc.Value.ToUniversalTime().ToString("O"))}");
            }

            var url = query.Count == 0
                ? "api/statistics"
                : $"api/statistics?{string.Join("&", query)}";

            var response = await _http.GetAsync(url, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);

            return await response.Content.ReadFromJsonAsync<StatisticsOverviewDto>(_jsonOptions, cancellationToken: cancellationToken);
        }

        private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
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
                throw new InvalidOperationException("Du har ikke adgang til statistik og rapporter.");
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                throw new InvalidOperationException(serverMessage);
            }

            throw new InvalidOperationException($"Statistik kunne ikke hentes. Serveren svarede: {serverMessage}");
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
