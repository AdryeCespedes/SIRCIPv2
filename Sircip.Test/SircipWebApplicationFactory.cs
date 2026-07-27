using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Sircip.Server.Data;
using Sircip.Server.Models;
using Sircip.Shared.Contracts;
using Sircip.Shared.Models;
using Sircip.Shared.Serialization;

namespace Sircip.Test;

/// <summary>
/// Levanta la API en memoria, reemplazando SQL Server por una base InMemory
/// sembrada con un usuario de cada rol.
/// </summary>
public class SircipWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string NombreUsuarioAdmin = "admin-test";
    public const string PasswordAdmin = "admin-password";

    /// <summary>Segundo administrador, para verificar que cada uno ve las importaciones del otro (AC-16).</summary>
    public const string NombreUsuarioOtroAdmin = "otro-admin-test";
    public const string PasswordOtroAdmin = "otro-admin-password";

    public const string NombreUsuarioComun = "usuario-test";
    public const string PasswordUsuario = "usuario-password";

    private readonly string _directorioRaiz = Directory.CreateTempSubdirectory("sircip-test-").FullName;

    /// <summary>Única carpeta de la que la API acepta archivos .txt para importar.</summary>
    public string DirectorioImportacion => Path.Combine(_directorioRaiz, "importacion");

    /// <summary>Carpeta donde la API deja los .bin de cada período.</summary>
    public string DirectorioDatos => Path.Combine(_directorioRaiz, "datos");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Production);

        builder.UseSetting("ConnectionStrings:Default", "no-usada-en-tests");
        builder.UseSetting("Jwt:Issuer", "Sircip");
        builder.UseSetting("Jwt:Audience", "SircipClient");
        builder.UseSetting("Jwt:Key", "clave-solo-para-tests-de-al-menos-32-caracteres");
        builder.UseSetting("Jwt:ExpirationHours", "24");
        builder.UseSetting("Padron:DirectorioImportacion", DirectorioImportacion);
        builder.UseSetting("Padron:DirectorioDatos", DirectorioDatos);

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
                NombreUsuario = NombreUsuarioOtroAdmin,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(PasswordOtroAdmin),
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

    public Task<HttpClient> CrearClienteAdminAsync() =>
        CrearClienteAutenticadoAsync(NombreUsuarioAdmin, PasswordAdmin);

    public Task<HttpClient> CrearClienteOtroAdminAsync() =>
        CrearClienteAutenticadoAsync(NombreUsuarioOtroAdmin, PasswordOtroAdmin);

    public Task<HttpClient> CrearClienteUsuarioAsync() =>
        CrearClienteAutenticadoAsync(NombreUsuarioComun, PasswordUsuario);

    private async Task<HttpClient> CrearClienteAutenticadoAsync(string nombreUsuario, string password)
    {
        var cliente = CreateClient();

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/auth/login", new LoginRequest { NombreUsuario = nombreUsuario, Password = password });
        respuesta.EnsureSuccessStatusCode();

        var login = await respuesta.Content.ReadFromJsonAsync<LoginResponse>(JsonSircip.Opciones);
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.Token);

        return cliente;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing && Directory.Exists(_directorioRaiz))
        {
            Directory.Delete(_directorioRaiz, recursive: true);
        }
    }
}
