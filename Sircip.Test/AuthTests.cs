using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Sircip.Shared.Contracts;
using Sircip.Shared.Models;
using Sircip.Shared.Serialization;

namespace Sircip.Test;

public class AuthTests : IClassFixture<SircipWebApplicationFactory>
{
    private readonly SircipWebApplicationFactory _factory;

    public AuthTests(SircipWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static LoginRequest Credenciales(string nombreUsuario, string password) =>
        new() { NombreUsuario = nombreUsuario, Password = password };

    /// <summary>AC-03: credenciales válidas devuelven 200 y un token de sesión.</summary>
    [Fact]
    public async Task Login_ConCredencialesValidas_DevuelveOkYToken()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            Credenciales(SircipWebApplicationFactory.NombreUsuarioAdmin, SircipWebApplicationFactory.PasswordAdmin));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var login = await response.Content.ReadFromJsonAsync<LoginResponse>(JsonSircip.Opciones);
        Assert.NotNull(login);
        Assert.False(string.IsNullOrWhiteSpace(login.Token));
        Assert.Equal(SircipWebApplicationFactory.NombreUsuarioAdmin, login.NombreUsuario);
        Assert.Equal(Rol.Administrador, login.Rol);
        Assert.True(login.ExpiraUtc > DateTime.UtcNow);
    }

    /// <summary>El rol del usuario viaja en la respuesta del login (base de RF-02).</summary>
    [Fact]
    public async Task Login_ConUsuarioRolUsuario_DevuelveRolUsuario()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            Credenciales(SircipWebApplicationFactory.NombreUsuarioComun, SircipWebApplicationFactory.PasswordUsuario));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var login = await response.Content.ReadFromJsonAsync<LoginResponse>(JsonSircip.Opciones);
        Assert.NotNull(login);
        Assert.Equal(Rol.Usuario, login.Rol);
    }

    [Fact]
    public async Task Login_ConPasswordIncorrecta_DevuelveUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            Credenciales(SircipWebApplicationFactory.NombreUsuarioAdmin, "password-incorrecta"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_ConUsuarioInexistente_DevuelveUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            Credenciales("no-existe", "cualquier-password"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("", "una-password")]
    [InlineData("un-usuario", "")]
    [InlineData("   ", "   ")]
    public async Task Login_SinUsuarioOPassword_DevuelveBadRequest(string nombreUsuario, string password)
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", Credenciales(nombreUsuario, password));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>AC-01/AC-02: sin autenticar, un endpoint protegido responde 401.</summary>
    [Fact]
    public async Task EndpointProtegido_SinToken_DevuelveUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task EndpointProtegido_ConTokenValido_DevuelveUsuarioYRol()
    {
        var client = _factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login",
            Credenciales(SircipWebApplicationFactory.NombreUsuarioAdmin, SircipWebApplicationFactory.PasswordAdmin));
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>(JsonSircip.Opciones);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.Token);
        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var contenido = await response.Content.ReadAsStringAsync();
        Assert.Contains(SircipWebApplicationFactory.NombreUsuarioAdmin, contenido);
        Assert.Contains(nameof(Rol.Administrador), contenido);
    }

    [Fact]
    public async Task EndpointProtegido_ConTokenInvalido_DevuelveUnauthorized()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "token-invalido");

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
