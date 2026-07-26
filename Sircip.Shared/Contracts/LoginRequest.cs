namespace Sircip.Shared.Contracts;

public class LoginRequest
{
    public required string NombreUsuario { get; set; }
    public required string Password { get; set; }
}
