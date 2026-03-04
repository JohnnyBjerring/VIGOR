namespace VIGOR.Shared.DTOs
{
    public class AuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public AuthResult Result { get; set; } = default!;
    }
}
