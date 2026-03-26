using VIGOR.Shared.Enums;
using VIGOR.Shared.Models;

namespace VIGOR.Shared.DTOs
{
    /// <summary>
    /// Resultat af et login-forsøg. Returneres fra AuthService til AuthController.
    /// Indeholder ingen Identity-typer – kun domæne-data.
    /// </summary>
    public class AuthResult
    {
        public AuthStatus Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public Role? Role { get; set; }
    }
}
