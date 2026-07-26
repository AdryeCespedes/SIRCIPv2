using System.Text;
using Microsoft.EntityFrameworkCore;
using Sircip.Server.Data;
using Sircip.Server.Models;
using Sircip.Shared.Models;

namespace Sircip.Server.Auth;

public class SeedUsuario
{
    private readonly SircipDbContext _db;

    public SeedUsuario(SircipDbContext db)
    {
        _db = db;
    }

    public async Task EjecutarAsync()
    {
        Console.WriteLine("=== Alta manual de usuario ===");

        Console.Write("Nombre de usuario: ");
        var nombreUsuario = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(nombreUsuario))
        {
            Console.WriteLine("Nombre de usuario requerido.");
            return;
        }

        if (await _db.Usuarios.AnyAsync(u => u.NombreUsuario == nombreUsuario))
        {
            Console.WriteLine($"Ya existe un usuario '{nombreUsuario}'.");
            return;
        }

        Console.Write("Rol (Administrador/Usuario) [Administrador]: ");
        var rolInput = Console.ReadLine()?.Trim();
        var rol = string.IsNullOrWhiteSpace(rolInput)
            ? Rol.Administrador
            : Enum.Parse<Rol>(rolInput, ignoreCase: true);

        Console.Write("Contraseña: ");
        var password = LeerPasswordOculto();

        if (string.IsNullOrWhiteSpace(password))
        {
            Console.WriteLine("Contraseña requerida.");
            return;
        }

        var usuario = new Usuario
        {
            NombreUsuario = nombreUsuario,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Rol = rol
        };

        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync();

        Console.WriteLine($"Usuario '{nombreUsuario}' ({rol}) creado.");
    }

    private static string LeerPasswordOculto()
    {
        if (Console.IsInputRedirected)
        {
            return Console.ReadLine() ?? string.Empty;
        }

        var password = new StringBuilder();
        ConsoleKeyInfo key;
        while ((key = Console.ReadKey(intercept: true)).Key != ConsoleKey.Enter)
        {
            if (key.Key == ConsoleKey.Backspace && password.Length > 0)
            {
                password.Remove(password.Length - 1, 1);
                Console.Write("\b \b");
            }
            else if (!char.IsControl(key.KeyChar))
            {
                password.Append(key.KeyChar);
                Console.Write('*');
            }
        }

        Console.WriteLine();
        return password.ToString();
    }
}
