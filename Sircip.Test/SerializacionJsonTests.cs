using System.Net;
using System.Net.Http.Json;
using Sircip.Shared.Contracts;
using Sircip.Shared.Serialization;

namespace Sircip.Test;

/// <summary>
/// Los enums viajan como texto en el JSON de la API.
///
/// Estos tests miran la respuesta cruda a propósito: los demás deserializan con
/// <see cref="JsonSircip.Opciones"/>, así que pasarían igual si el servidor
/// mandara el número de la posición del enum.
/// </summary>
public class SerializacionJsonTests : IClassFixture<SircipWebApplicationFactory>
{
    private readonly SircipWebApplicationFactory _factory;

    public SerializacionJsonTests(SircipWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpResponseMessage> ImportarAsync(HttpClient cliente, int periodo, string linea)
    {
        Directory.CreateDirectory(_factory.DirectorioImportacion);
        var nombre = $"serializacion-{periodo}.txt";
        File.WriteAllLines(Path.Combine(_factory.DirectorioImportacion, nombre), [linea]);

        return await cliente.PostAsJsonAsync("/api/padron/importaciones", new ImportarPadronRequest
        {
            Anio = periodo / 100,
            Mes = periodo % 100,
            RutaArchivo = nombre
        });
    }

    [Fact]
    public async Task ElEstadoDeUnaImportacionExitosa_ViajaComoTextoYNoComoNumero()
    {
        const int periodo = 203001;
        var cliente = await _factory.CrearClienteAdminAsync();

        var respuesta = await ImportarAsync(
            cliente, periodo, PadronDePrueba.Linea(periodo, PadronDePrueba.Cuit(0)));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        var json = await respuesta.Content.ReadAsStringAsync();
        Assert.Contains("\"estado\":\"Exitosa\"", json);
        Assert.DoesNotContain("\"estado\":0", json);
    }

    [Fact]
    public async Task ElEstadoDeUnaImportacionFallida_ViajaComoTextoYNoComoNumero()
    {
        const int periodo = 203002;
        var cliente = await _factory.CrearClienteAdminAsync();

        var respuesta = await ImportarAsync(
            cliente, periodo, $"{periodo},99999999999,Empresa,904,34,B,{PadronDePrueba.Campo7}");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);

        var json = await respuesta.Content.ReadAsStringAsync();
        Assert.Contains("\"estado\":\"ConError\"", json);
        Assert.DoesNotContain("\"estado\":1", json);
    }

    [Fact]
    public async Task ElRolDelLogin_ViajaComoTextoYNoComoNumero()
    {
        var cliente = _factory.CreateClient();

        var respuesta = await cliente.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            NombreUsuario = SircipWebApplicationFactory.NombreUsuarioAdmin,
            Password = SircipWebApplicationFactory.PasswordAdmin
        });

        var json = await respuesta.Content.ReadAsStringAsync();

        Assert.Contains("\"rol\":\"Administrador\"", json);
        Assert.DoesNotContain("\"rol\":0", json);
    }
}
