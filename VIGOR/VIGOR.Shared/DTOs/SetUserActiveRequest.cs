namespace VIGOR.Shared.DTOs
{
    /// <summary>
    /// UC13: Aktiverer eller deaktiverer en bruger.
    /// Deaktivering implementeres via Identity lockout.
    /// </summary>
    public class SetUserActiveRequest
    {
        public bool IsActive { get; set; }
    }
}
