using Sircip.Shared.Models;

namespace Sircip.Shared.Contracts;

public class LoginResponse
{
    public required string Token { get; set; }
    public required string NombreUsuario { get; set; }
    public required Rol Rol { get; set; }
    public required DateTime ExpiraUtc { get; set; }
}
