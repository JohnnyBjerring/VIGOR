using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using VIGOR.Shared.DTOs;
using VIGOR.Shared.Interfaces.Services;

namespace VIGOR.Shared.Services
{
    public class ShiftSelectionClientService : IShiftSelectionApi
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public ShiftSelectionClientService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };
        }

        public async Task<ActiveShiftContextDto?> SelectShiftAsync(
            SelectShiftRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var response = await _httpClient.PostAsJsonAsync(
                "api/shifts/select",
                request,
                _jsonOptions,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ActiveShiftContextDto>(
                _jsonOptions,
                cancellationToken);
        }
    }
}
