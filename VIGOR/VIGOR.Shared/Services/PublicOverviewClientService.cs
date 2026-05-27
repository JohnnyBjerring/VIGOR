using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using VIGOR.Shared.DTOs;
using VIGOR.Shared.Interfaces.Services;

namespace VIGOR.Shared.Services
{
    /// <summary>
    /// Client-side implementation til UC14 public overview endpoint.
    /// Kalder kun anonymt endpoint og sender ingen bruger-/afdelingsdata.
    /// </summary>
    public class PublicOverviewClientService : IPublicOverviewApi
    {
        private readonly HttpClient _http;
        private readonly JsonSerializerOptions _jsonOptions;

        public PublicOverviewClientService(HttpClient http)
        {
            _http = http;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };
        }

        public async Task<PublicOverviewDto?> GetPublicOverviewAsync(CancellationToken cancellationToken = default)
        {
            var response = await _http.GetAsync("api/public/overview", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var serverMessage = await response.Content.ReadAsStringAsync(cancellationToken);
                if (string.IsNullOrWhiteSpace(serverMessage))
                {
                    serverMessage = response.ReasonPhrase ?? "Ukendt serverfejl.";
                }

                throw new InvalidOperationException($"Den anonyme oversigt kunne ikke hentes. Serveren svarede: {serverMessage.Trim().Trim('\"')}");
            }

            return await response.Content.ReadFromJsonAsync<PublicOverviewDto>(_jsonOptions, cancellationToken: cancellationToken);
        }
    }
}
