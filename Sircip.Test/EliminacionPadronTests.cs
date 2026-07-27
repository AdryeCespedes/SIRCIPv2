using System.Net;
using System.Net.Http.Json;
using Sircip.Shared.Contracts;
using Sircip.Shared.Models;

namespace Sircip.Test;

/// <summary>
/// Borrado lógico del padrón de un período (RF-09).
///
/// Cada test usa un período propio, por la misma razón que en
/// <see cref="ImportacionPadronTests"/>: los períodos con padrón vigente no se
/// pueden reimportar y compartirlos haría chocar los tests entre sí.
/// </summary>
public class EliminacionPadronTests : IClassFixture<SircipWebApplicationFactory>
{
    private readonly SircipWebApplicationFactory _factory;

    public EliminacionPadronTests(SircipWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static ImportarPadronRequest Pedido(int periodo, string rutaArchivo) => new()
    {
        Anio = periodo / 100,
        Mes = periodo % 100,
        RutaArchivo = rutaArchivo
    };

    private static string Url(int periodo) => $"/api/padron/importaciones/{periodo / 100}/{periodo % 100}";

    private string RutaBinaria(int periodo) => Path.Combine(_factory.DirectorioDatos, $"padron-{periodo}.bin");

    /// <summary>Importa un padrón de tres registros y devuelve su constancia.</summary>
    private async Task<ImportacionResponse> ImportarAsync(HttpClient cliente, int periodo)
    {
        Directory.CreateDirectory(_factory.DirectorioImportacion);
        var nombre = $"padron-{periodo}.txt";
        File.WriteAllLines(
            Path.Combine(_factory.DirectorioImportacion, nombre),
            Enumerable.Range(0, 3).Select(i => PadronDePrueba.Linea(periodo, PadronDePrueba.Cuit(i))));

        var respuesta = await cliente.PostAsJsonAsync("/api/padron/importaciones", Pedido(periodo, nombre));
        respuesta.EnsureSuccessStatusCode();

        return (await respuesta.Content.ReadFromJsonAsync<ImportacionResponse>())!;
    }

    [Fact]
    public async Task Eliminar_SinToken_DevuelveUnauthorized()
    {
        var cliente = _factory.CreateClient();

        var respuesta = await cliente.DeleteAsync(Url(202801));

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    /// <summary>AC-14: un usuario con rol Usuario no puede eliminar el padrón de un período.</summary>
    [Fact]
    public async Task Eliminar_ConRolUsuario_DevuelveForbidden()
    {
        var cliente = await _factory.CrearClienteUsuarioAsync();

        var respuesta = await cliente.DeleteAsync(Url(202801));

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact]
    public async Task Eliminar_UnPeriodoImportado_DevuelveLaConstanciaMarcadaComoBorrada()
    {
        const int periodo = 202802;
        var cliente = await _factory.CrearClienteAdminAsync();
        await ImportarAsync(cliente, periodo);

        var respuesta = await cliente.DeleteAsync(Url(periodo));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        var borrada = await respuesta.Content.ReadFromJsonAsync<ImportacionResponse>();
        Assert.NotNull(borrada);
        Assert.Equal(EstadoImportacion.Borrada, borrada.Estado);
        Assert.Equal(periodo, borrada.Periodo);
    }

    /// <summary>
    /// AC-13: la constancia no desaparece del historial, sigue estando y se
    /// muestra marcada como borrada.
    /// </summary>
    [Fact]
    public async Task Eliminar_ConservaLaConstanciaEnElHistorial()
    {
        const int periodo = 202803;
        var cliente = await _factory.CrearClienteAdminAsync();
        var importada = await ImportarAsync(cliente, periodo);

        await cliente.DeleteAsync(Url(periodo));

        var consultada = await (await cliente.GetAsync($"/api/padron/importaciones/{importada.Id}"))
            .Content.ReadFromJsonAsync<ImportacionResponse>();

        Assert.NotNull(consultada);
        Assert.Equal(importada.Id, consultada.Id);
        Assert.Equal(EstadoImportacion.Borrada, consultada.Estado);
        Assert.Equal(SircipWebApplicationFactory.NombreUsuarioAdmin, consultada.Usuario);
        Assert.Equal(3, consultada.CantidadRegistros);
        Assert.Equal(importada.FechaImportacionUtc, consultada.FechaImportacionUtc);
    }

    /// <summary>
    /// AC-12 y RF-09: después de borrarlo, el período pasa a contar como no
    /// importado. El 404 al consultar un CUIT (AC-12) sale de esto mismo.
    /// </summary>
    [Fact]
    public async Task Eliminar_DejaElPeriodoComoNoImportado()
    {
        const int periodo = 202804;
        var cliente = await _factory.CrearClienteAdminAsync();
        await ImportarAsync(cliente, periodo);

        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync(Url(periodo))).StatusCode);

        await cliente.DeleteAsync(Url(periodo));

        Assert.Equal(HttpStatusCode.NotFound, (await cliente.GetAsync(Url(periodo))).StatusCode);
    }

    /// <summary>Los datos del padrón dejan de estar disponibles, no solo marcados.</summary>
    [Fact]
    public async Task Eliminar_BorraElArchivoDelPeriodo()
    {
        const int periodo = 202805;
        var cliente = await _factory.CrearClienteAdminAsync();
        await ImportarAsync(cliente, periodo);

        Assert.True(File.Exists(RutaBinaria(periodo)));

        await cliente.DeleteAsync(Url(periodo));

        Assert.False(File.Exists(RutaBinaria(periodo)));
    }

    /// <summary>
    /// RF-09 y RF-03: eliminar es lo que habilita volver a importar el período.
    /// </summary>
    [Fact]
    public async Task Eliminar_HabilitaVolverAImportarElPeriodo()
    {
        const int periodo = 202806;
        var cliente = await _factory.CrearClienteAdminAsync();
        await ImportarAsync(cliente, periodo);

        var reimportarAntes = await cliente.PostAsJsonAsync(
            "/api/padron/importaciones", Pedido(periodo, $"padron-{periodo}.txt"));
        Assert.Equal(HttpStatusCode.Conflict, reimportarAntes.StatusCode);

        await cliente.DeleteAsync(Url(periodo));

        var reimportarDespues = await cliente.PostAsJsonAsync(
            "/api/padron/importaciones", Pedido(periodo, $"padron-{periodo}.txt"));

        Assert.Equal(HttpStatusCode.OK, reimportarDespues.StatusCode);
        Assert.True(File.Exists(RutaBinaria(periodo)));
    }

    /// <summary>Reimportar deja dos constancias del período: la borrada y la nueva.</summary>
    [Fact]
    public async Task Eliminar_YReimportar_DejaLasDosConstanciasEnElHistorial()
    {
        const int periodo = 202807;
        var cliente = await _factory.CrearClienteAdminAsync();
        var primera = await ImportarAsync(cliente, periodo);

        await cliente.DeleteAsync(Url(periodo));
        var segunda = await ImportarAsync(cliente, periodo);

        Assert.NotEqual(primera.Id, segunda.Id);

        var borrada = await (await cliente.GetAsync($"/api/padron/importaciones/{primera.Id}"))
            .Content.ReadFromJsonAsync<ImportacionResponse>();
        var vigente = await (await cliente.GetAsync($"/api/padron/importaciones/{segunda.Id}"))
            .Content.ReadFromJsonAsync<ImportacionResponse>();

        Assert.Equal(EstadoImportacion.Borrada, borrada!.Estado);
        Assert.Equal(EstadoImportacion.Exitosa, vigente!.Estado);
    }

    [Fact]
    public async Task Eliminar_UnPeriodoSinPadronImportado_DevuelveNotFound()
    {
        var cliente = await _factory.CrearClienteAdminAsync();

        var respuesta = await cliente.DeleteAsync(Url(202808));

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    [Fact]
    public async Task Eliminar_DosVeces_LaSegundaDevuelveNotFound()
    {
        const int periodo = 202809;
        var cliente = await _factory.CrearClienteAdminAsync();
        await ImportarAsync(cliente, periodo);

        Assert.Equal(HttpStatusCode.OK, (await cliente.DeleteAsync(Url(periodo))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.DeleteAsync(Url(periodo))).StatusCode);
    }

    /// <summary>Una importación fallida no es un padrón vigente: no hay nada que eliminar.</summary>
    [Fact]
    public async Task Eliminar_UnPeriodoConSoloUnaImportacionFallida_DevuelveNotFound()
    {
        const int periodo = 202810;
        var cliente = await _factory.CrearClienteAdminAsync();

        Directory.CreateDirectory(_factory.DirectorioImportacion);
        var nombre = $"padron-fallido-{periodo}.txt";
        File.WriteAllLines(
            Path.Combine(_factory.DirectorioImportacion, nombre),
            [$"{periodo},99999999999,Empresa,904,34,B,{PadronDePrueba.Campo7}"]);

        var importacion = await cliente.PostAsJsonAsync("/api/padron/importaciones", Pedido(periodo, nombre));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, importacion.StatusCode);

        var respuesta = await cliente.DeleteAsync(Url(periodo));

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    [Theory]
    [InlineData(2026, 0)]
    [InlineData(2026, 13)]
    [InlineData(1999, 3)]
    public async Task Eliminar_ConMesOAnioInvalido_DevuelveBadRequest(int anio, int mes)
    {
        var cliente = await _factory.CrearClienteAdminAsync();

        var respuesta = await cliente.DeleteAsync($"/api/padron/importaciones/{anio}/{mes}");

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }
}
