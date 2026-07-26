using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Sircip.Server.Data;
using Sircip.Server.Models;
using Sircip.Shared.Models;

namespace Sircip.Test;

/// <summary>
/// Levanta la API en memoria, reemplazando SQL Server por una base InMemory
/// sembrada con un usuario de cada rol.
/// </summary>
public class SircipWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string NombreUsuarioAdmin = "admin-test";
    public const string PasswordAdmin = "admin-password";

    public const string NombreUsuarioComun = "usuario-test";
    public const string PasswordUsuario = "usuario-password";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Production);

        builder.UseSetting("ConnectionStrings:Default", "no-usada-en-tests");
        builder.UseSetting("Jwt:Issuer", "Sircip");
        builder.UseSetting("Jwt:Audience", "SircipClient");
        builder.UseSetting("Jwt:Key", "clave-solo-para-tests-de-al-menos-32-caracteres");
        builder.UseSetting("Jwt:ExpirationHours", "24");

        var nombreBase = Guid.NewGuid().ToString();
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<SircipDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.AddDbContext<SircipDbContext>(options => options.UseInMemoryDatabase(nombreBase));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SircipDbContext>();
        db.Usuarios.AddRange(
            new Usuario
            {
                NombreUsuario = NombreUsuarioAdmin,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(PasswordAdmin),
                Rol = Rol.Administrador
            },
            new Usuario
            {
                NombreUsuario = NombreUsuarioComun,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(PasswordUsuario),
                Rol = Rol.Usuario
            });
        db.SaveChanges();

        return host;
    }
}
