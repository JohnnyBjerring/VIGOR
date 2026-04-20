using System.Net.Http.Json;
using VIGOR.Shared.DTOs;
using VIGOR.Shared.Enums;
using VIGOR.Shared.Interfaces.Services;
using Microsoft.Maui.Storage;

namespace VIGOR.MAUI.Services
{
    public class ApiAuthService : IAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly System.Text.Json.JsonSerializerOptions _jsonOptions;

        public ApiAuthService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _jsonOptions = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            };
        }

        public async Task<AuthResult> SignInAsync(string email, string password)
        {
            try
            {
                var request = new LoginRequest { Email = email, Password = password };
                var response = await _httpClient.PostAsJsonAsync("api/auth/login", request, _jsonOptions);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<AuthResponse>(_jsonOptions);
                    if (result != null && !string.IsNullOrEmpty(result.Token))
                    {
                        // Safely store the JWT token locally
                        await SecureStorage.Default.SetAsync("jwt_token", result.Token);
                        return result.Result;
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    var result = await response.Content.ReadFromJsonAsync<AuthResponse>(_jsonOptions);
                    if (result != null && result.Result != null)
                    {
                        return result.Result;
                    }
                }

                return new AuthResult { Status = AuthStatus.Rejected, Message = "Forkert email eller adgangskode." };
            }
            catch (Exception ex)
            {
                // In a real app we would log the 'ex'
                System.Diagnostics.Debug.WriteLine($"Login fejlede: {ex.Message}");
#if DEBUG
                return new AuthResult { Status = AuthStatus.Rejected, Message = $"Fejl under login: {ex.Message}" };
#else
                return new AuthResult { Status = AuthStatus.Rejected, Message = "Kunne ikke forbinde til serveren." };
#endif
            }
        }
    }
}
