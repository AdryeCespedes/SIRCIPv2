using System.Net;
using System.Net.Http.Json;
using Sircip.Shared.Contracts;
using Sircip.Shared.Models;
using Sircip.Shared.Serialization;

namespace Sircip.Test;

/// <summary>
/// Historial de importaciones (RF-10). Es lo que consume la página de
/// importaciones del cliente.
/// </summary>
public class HistorialImportacionesTests : IClassFixture<SircipWebApplicationFactory>
{
    private const string Url = "/api/padron/importaciones";

    private readonly SircipWebApplicationFactory _factory;

    public HistorialImportacionesTests(SircipWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static ImportarPadronRequest Pedido(int periodo, string rutaArchivo) => new()
    {
        Anio = periodo / 100,
        Mes = periodo % 100,
        RutaArchivo = rutaArchivo
    };

    private string DejarArchivo(string nombre, params string[] lineas)
    {
        Directory.CreateDirectory(_factory.DirectorioImportacion);
        File.WriteAllLines(Path.Combine(_factory.DirectorioImportacion, nombre), lineas);
        return nombre;
    }

    private async Task<ImportacionResponse> ImportarBienAsync(HttpClient cliente, int periodo)
    {
        var archivo = DejarArchivo(
            $"historial-{periodo}.txt",
            PadronDePrueba.Linea(periodo, PadronDePrueba.Cuit(0)),
            PadronDePrueba.Linea(periodo, PadronDePrueba.Cuit(1)));

        var respuesta = await cliente.PostAsJsonAsync("/api/padron/importaciones", Pedido(periodo, archivo));
        respuesta.EnsureSuccessStatusCode();

        return (await respuesta.Content.ReadFromJsonAsync<ImportacionResponse>(JsonSircip.Opciones))!;
    }

    private async Task<ImportacionResponse> ImportarMalAsync(HttpClient cliente, int periodo)
    {
        var archivo = DejarArchivo(
            $"historial-fallido-{periodo}.txt",
            $"{periodo},99999999999,Empresa,904,34,B,{PadronDePrueba.Campo7}");

        var respuesta = await cliente.PostAsJsonAsync("/api/padron/importaciones", Pedido(periodo, archivo));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);

        return (await respuesta.Content.ReadFromJsonAsync<ImportacionResponse>(JsonSircip.Opciones))!;
    }

    private static async Task<List<ImportacionResponse>> ListarAsync(HttpClient cliente)
    {
        var respuesta = await cliente.GetAsync(Url);
        respuesta.EnsureSuccessStatusCode();

        return (await respuesta.Content.ReadFromJsonAsync<List<ImportacionResponse>>(JsonSircip.Opciones))!;
    }

    [Fact]
    public async Task Listar_SinToken_DevuelveUnauthorized()
    {
        var cliente = _factory.CreateClient();

        var respuesta = await cliente.GetAsync(Url);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    /// <summary>AC-17: un usuario con rol Usuario no puede consultar el historial.</summary>
    [Fact]
    public async Task Listar_ConRolUsuario_DevuelveForbidden()
    {
        var cliente = await _factory.CrearClienteUsuarioAsync();

        var respuesta = await cliente.GetAsync(Url);

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    /// <summary>AC-15: el listado incluye tanto las exitosas como las que tuvieron error.</summary>
    [Fact]
    public async Task Listar_DevuelveLasExitosasYLasQueTuvieronError()
    {
        var cliente = await _factory.CrearClienteAdminAsync();
        var exitosa = await ImportarBienAsync(cliente, 202901);
        var fallida = await ImportarMalAsync(cliente, 202902);

        var historial = await ListarAsync(cliente);

        var enElHistorial = historial.Where(i => i.Id == exitosa.Id || i.Id == fallida.Id).ToList();
        Assert.Equal(2, enElHistorial.Count);
        Assert.Contains(enElHistorial, i => i.Id == exitosa.Id && i.Estado == EstadoImportacion.Exitosa);
        Assert.Contains(enElHistorial, i => i.Id == fallida.Id && i.Estado == EstadoImportacion.ConError);
    }

    /// <summary>
    /// AC-16: cada Administrador ve todas las importaciones, propias y de otros
    /// administradores. No hay aislamiento de datos entre usuarios.
    /// </summary>
    [Fact]
    public async Task Listar_MuestraLasImportacionesDeOtrosAdministradores()
    {
        var admin = await _factory.CrearClienteAdminAsync();
        var otroAdmin = await _factory.CrearClienteOtroAdminAsync();

        var propia = await ImportarBienAsync(admin, 202903);
        var ajena = await ImportarBienAsync(otroAdmin, 202904);

        var historialDelPrimero = await ListarAsync(admin);

        Assert.Contains(historialDelPrimero, i => i.Id == propia.Id);
        Assert.Contains(historialDelPrimero, i => i.Id == ajena.Id);
        Assert.Contains(
            historialDelPrimero,
            i => i.Id == ajena.Id && i.Usuario == SircipWebApplicationFactory.NombreUsuarioOtroAdmin);

        // Y al revés, para que quede claro que no depende de quién importó primero.
        var historialDelSegundo = await ListarAsync(otroAdmin);
        Assert.Contains(historialDelSegundo, i => i.Id == propia.Id);
        Assert.Contains(historialDelSegundo, i => i.Id == ajena.Id);
    }

    /// <summary>Un padrón eliminado no desaparece del historial: sigue, marcado como borrado.</summary>
    [Fact]
    public async Task Listar_IncluyeLasImportacionesBorradas()
    {
        const int periodo = 202905;
        var cliente = await _factory.CrearClienteAdminAsync();
        var importada = await ImportarBienAsync(cliente, periodo);

        await cliente.DeleteAsync($"/api/padron/importaciones/{periodo / 100}/{periodo % 100}");

        var historial = await ListarAsync(cliente);

        var borrada = Assert.Single(historial.Where(i => i.Id == importada.Id));
        Assert.Equal(EstadoImportacion.Borrada, borrada.Estado);
    }

    /// <summary>El historial se lee de lo más reciente a lo más antiguo.</summary>
    [Fact]
    public async Task Listar_DevuelveLasMasRecientesPrimero()
    {
        var cliente = await _factory.CrearClienteAdminAsync();
        await ImportarBienAsync(cliente, 202906);
        await ImportarBienAsync(cliente, 202907);

        var historial = await ListarAsync(cliente);

        var fechas = historial.Select(i => i.FechaImportacionUtc).ToList();
        Assert.Equal(fechas.OrderByDescending(f => f).ToList(), fechas);
    }

    /// <summary>Cada constancia del listado trae los cuatro datos de RF-04.</summary>
    [Fact]
    public async Task Listar_TraeLosDatosDeCadaConstancia()
    {
        const int periodo = 202908;
        var cliente = await _factory.CrearClienteAdminAsync();
        var importada = await ImportarBienAsync(cliente, periodo);

        var historial = await ListarAsync(cliente);

        var constancia = Assert.Single(historial.Where(i => i.Id == importada.Id));
        Assert.Equal(periodo, constancia.Periodo);
        Assert.Equal(SircipWebApplicationFactory.NombreUsuarioAdmin, constancia.Usuario);
        Assert.Equal(2, constancia.CantidadRegistros);
        Assert.Equal(importada.FechaImportacionUtc, constancia.FechaImportacionUtc);
    }
}
