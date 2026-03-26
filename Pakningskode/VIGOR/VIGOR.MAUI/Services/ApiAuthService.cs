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

        public ApiAuthService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<AuthResult> SignInAsync(string email, string password)
        {
            try
            {
                var request = new LoginRequest { Email = email, Password = password };
                var response = await _httpClient.PostAsJsonAsync("api/auth/login", request);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
                    if (result != null && !string.IsNullOrEmpty(result.Token))
                    {
                        // Safely store the JWT token locally
                        await SecureStorage.Default.SetAsync("jwt_token", result.Token);
                        return result.Result;
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
                    if (result != null && result.Result != null)
                    {
                        return result.Result;
                    }
                }

                return new AuthResult { Status = AuthStatus.Rejected, Message = "Forkert email eller adgangskode." };
            }
            catch (Exception)
            {
                // In a real app we would log the 'ex'
                return new AuthResult { Status = AuthStatus.Rejected, Message = "Kunne ikke forbinde til serveren." };
            }
        }
    }
}
