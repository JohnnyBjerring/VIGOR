using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using VIGOR.Shared.DTOs;
using VIGOR.Shared.Interfaces.Services;

namespace VIGOR.Shared.Services
{
    /// <summary>
    /// Client-side implementation for UC13 admin endpoints.
    /// Holder UI fri for endpointdetaljer.
    /// </summary>
    public class UserAdminClientService : IUserAdminApi
    {
        private readonly HttpClient _http;
        private readonly JsonSerializerOptions _jsonOptions;

        public UserAdminClientService(HttpClient http)
        {
            _http = http;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };
        }

        public async Task<IReadOnlyList<UserAdminUserDto>> GetUsersAsync(CancellationToken cancellationToken = default)
        {
            var response = await _http.GetAsync("api/admin/users", cancellationToken);
            await EnsureSuccessAsync(response, "Brugere kunne ikke hentes.", cancellationToken);

            return await response.Content.ReadFromJsonAsync<List<UserAdminUserDto>>(_jsonOptions, cancellationToken)
                   ?? new List<UserAdminUserDto>();
        }

        public async Task<IReadOnlyList<UserAdminRoleDto>> GetRolesAsync(CancellationToken cancellationToken = default)
        {
            var response = await _http.GetAsync("api/admin/users/roles", cancellationToken);
            await EnsureSuccessAsync(response, "Roller kunne ikke hentes.", cancellationToken);

            return await response.Content.ReadFromJsonAsync<List<UserAdminRoleDto>>(_jsonOptions, cancellationToken)
                   ?? new List<UserAdminRoleDto>();
        }

        public async Task<IReadOnlyList<UserAdminDepartmentDto>> GetDepartmentsAsync(CancellationToken cancellationToken = default)
        {
            var response = await _http.GetAsync("api/admin/users/departments", cancellationToken);
            await EnsureSuccessAsync(response, "Afdelinger kunne ikke hentes.", cancellationToken);

            return await response.Content.ReadFromJsonAsync<List<UserAdminDepartmentDto>>(_jsonOptions, cancellationToken)
                   ?? new List<UserAdminDepartmentDto>();
        }

        public async Task<UserAdminUserDto?> CreateUserAsync(
            CreateUserAdminUserRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var response = await _http.PostAsJsonAsync("api/admin/users", request, _jsonOptions, cancellationToken);
            await EnsureSuccessAsync(response, "Brugeren kunne ikke oprettes.", cancellationToken);

            return await response.Content.ReadFromJsonAsync<UserAdminUserDto>(_jsonOptions, cancellationToken);
        }

        public async Task<UserAdminUserDto?> UpdateRoleAsync(
            string userId,
            UpdateUserRoleRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var response = await _http.PutAsJsonAsync($"api/admin/users/{Uri.EscapeDataString(userId)}/role", request, _jsonOptions, cancellationToken);
            await EnsureSuccessAsync(response, "Brugerens rolle kunne ikke opdateres.", cancellationToken);

            return await response.Content.ReadFromJsonAsync<UserAdminUserDto>(_jsonOptions, cancellationToken);
        }

        public async Task<UserAdminUserDto?> SetActiveAsync(
            string userId,
            SetUserActiveRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var response = await _http.PutAsJsonAsync($"api/admin/users/{Uri.EscapeDataString(userId)}/active", request, _jsonOptions, cancellationToken);
            await EnsureSuccessAsync(response, "Brugerens aktive status kunne ikke opdateres.", cancellationToken);

            return await response.Content.ReadFromJsonAsync<UserAdminUserDto>(_jsonOptions, cancellationToken);
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
                throw new InvalidOperationException("Du har ikke adgang til brugeradministration.");
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new InvalidOperationException("Brugeren eller ressourcen blev ikke fundet.");
            }

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                throw new InvalidOperationException(serverMessage);
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                throw new InvalidOperationException(serverMessage);
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
                    var firstError = errors.EnumerateObject()
                        .SelectMany(p => p.Value.EnumerateArray())
                        .Select(v => v.GetString())
                        .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

                    if (!string.IsNullOrWhiteSpace(firstError))
                    {
                        message = firstError!;
                        return true;
                    }
                }
            }
            catch (JsonException)
            {
                // Fall back to raw content.
            }

            return false;
        }
    }
}
