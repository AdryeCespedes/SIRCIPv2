using Sircip.Shared.Models;

namespace Sircip.Server.Models;

public class Usuario
{
    public int Id { get; set; }
    public required string NombreUsuario { get; set; }
    public required string PasswordHash { get; set; }
    public Rol Rol { get; set; }
}
