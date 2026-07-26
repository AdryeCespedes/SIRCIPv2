using System.Net;
using System.Net.Http.Json;
using Sircip.Server.Padron.Services;
using Sircip.Shared.Contracts;
using Sircip.Shared.Models;

namespace Sircip.Test;

/// <summary>
/// Importación del padrón por la API (RF-03, RF-04, RF-08, RF-12).
///
/// Cada test usa un período distinto: un período con padrón vigente no se puede
/// reimportar, así que compartirlos los haría chocar entre sí.
/// </summary>
public class ImportacionPadronTests : IClassFixture<SircipWebApplicationFactory>
{
    private readonly SircipWebApplicationFactory _factory;

    public ImportacionPadronTests(SircipWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static ImportarPadronRequest Pedido(int periodo, string rutaArchivo) => new()
    {
        Anio = periodo / 100,
        Mes = periodo % 100,
        RutaArchivo = rutaArchivo
    };

    /// <summary>Deja un archivo de padrón en la carpeta de importación y devuelve su nombre.</summary>
    private string DejarArchivo(string nombre, params string[] lineas)
    {
        Directory.CreateDirectory(_factory.DirectorioImportacion);
        File.WriteAllLines(Path.Combine(_factory.DirectorioImportacion, nombre), lineas);
        return nombre;
    }

    private string DejarPadronValido(string nombre, int periodo, int cantidadRegistros = 3) =>
        DejarArchivo(
            nombre,
            Enumerable.Range(0, cantidadRegistros)
                .Select(i => PadronDePrueba.Linea(periodo, PadronDePrueba.Cuit(i)))
                .ToArray());

    private string RutaBinaria(int periodo) =>
        Path.Combine(_factory.DirectorioDatos, $"padron-{periodo}.bin");

    /// <summary>AC-01: sin autenticar, importar responde 401.</summary>
    [Fact]
    public async Task Importar_SinToken_DevuelveUnauthorized()
    {
        var cliente = _factory.CreateClient();

        var respuesta = await cliente.PostAsJsonAsync("/api/padron/importaciones", Pedido(202601, "cualquiera.txt"));

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    /// <summary>AC-05: un usuario con rol Usuario no puede importar.</summary>
    [Fact]
    public async Task Importar_ConRolUsuario_DevuelveForbidden()
    {
        var cliente = await _factory.CrearClienteUsuarioAsync();

        var respuesta = await cliente.PostAsJsonAsync("/api/padron/importaciones", Pedido(202601, "cualquiera.txt"));

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    /// <summary>AC-17: la consulta de importaciones también es solo de Administrador.</summary>
    [Fact]
    public async Task ObtenerImportacion_ConRolUsuario_DevuelveForbidden()
    {
        var cliente = await _factory.CrearClienteUsuarioAsync();

        var respuesta = await cliente.GetAsync("/api/padron/importaciones/1");

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    /// <summary>AC-06 y AC-19: un padrón válido se importa entero y devuelve 200.</summary>
    [Fact]
    public async Task Importar_ConArchivoValido_DevuelveOkYPersisteTodosLosRegistros()
    {
        const int periodo = 202602;
        var archivo = DejarPadronValido("padron-valido.txt", periodo, cantidadRegistros: 25);
        var cliente = await _factory.CrearClienteAdminAsync();

        var respuesta = await cliente.PostAsJsonAsync("/api/padron/importaciones", Pedido(periodo, archivo));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        var importacion = await respuesta.Content.ReadFromJsonAsync<ImportacionResponse>();
        Assert.NotNull(importacion);
        Assert.Equal(periodo, importacion.Periodo);
        Assert.Equal(25, importacion.CantidadRegistros);
        Assert.Equal(EstadoImportacion.Exitosa, importacion.Estado);
        Assert.Null(importacion.Error);
    }

    /// <summary>AC-07: la constancia guarda fecha, período, usuario y cantidad.</summary>
    [Fact]
    public async Task Importar_YLuegoConsultarLaConstancia_DevuelveLosCuatroDatosDeRF04()
    {
        const int periodo = 202603;
        var archivo = DejarPadronValido("padron-constancia.txt", periodo, cantidadRegistros: 7);
        var cliente = await _factory.CrearClienteAdminAsync();
        var antes = DateTime.UtcNow.AddSeconds(-5);

        var importada = await (await cliente.PostAsJsonAsync("/api/padron/importaciones", Pedido(periodo, archivo)))
            .Content.ReadFromJsonAsync<ImportacionResponse>();

        var respuesta = await cliente.GetAsync($"/api/padron/importaciones/{importada!.Id}");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        var consultada = await respuesta.Content.ReadFromJsonAsync<ImportacionResponse>();
        Assert.NotNull(consultada);
        Assert.Equal(periodo, consultada.Periodo);
        Assert.Equal(SircipWebApplicationFactory.NombreUsuarioAdmin, consultada.Usuario);
        Assert.Equal(7, consultada.CantidadRegistros);
        Assert.InRange(consultada.FechaImportacionUtc, antes, DateTime.UtcNow.AddSeconds(5));
    }

    /// <summary>El padrón importado queda buscable por CUIT.</summary>
    [Fact]
    public async Task Importar_ConArchivoValido_DejaElPadronBuscablePorCuit()
    {
        const int periodo = 202604;
        var archivo = DejarPadronValido("padron-buscable.txt", periodo, cantidadRegistros: 50);
        var cliente = await _factory.CrearClienteAdminAsync();

        await cliente.PostAsJsonAsync("/api/padron/importaciones", Pedido(periodo, archivo));

        using var lector = new LectorPadronBinario(RutaBinaria(periodo));
        Assert.Equal(periodo, lector.Periodo);
        Assert.Equal(50, lector.CantidadRegistros);
        Assert.NotNull(lector.Buscar(PadronDePrueba.Cuit(0)));
        Assert.NotNull(lector.Buscar(PadronDePrueba.Cuit(49)));
        Assert.Null(lector.Buscar(PadronDePrueba.Cuit(50)));
    }

    /// <summary>AC-18 y RF-12: una sola línea mala rechaza el archivo entero.</summary>
    [Fact]
    public async Task Importar_ConUnaLineaInvalida_RechazaTodoYNoPersisteNada()
    {
        const int periodo = 202605;
        var archivo = DejarArchivo(
            "padron-con-error.txt",
            PadronDePrueba.Linea(periodo, PadronDePrueba.Cuit(0)),
            PadronDePrueba.Linea(periodo, PadronDePrueba.Cuit(1)),
            $"{periodo},30100100106,Empresa,904,34,Z,{PadronDePrueba.Campo7}",
            PadronDePrueba.Linea(periodo, PadronDePrueba.Cuit(2)));
        var cliente = await _factory.CrearClienteAdminAsync();

        var respuesta = await cliente.PostAsJsonAsync("/api/padron/importaciones", Pedido(periodo, archivo));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);
        Assert.False(File.Exists(RutaBinaria(periodo)), "No debería haber quedado ningún padrón del período.");
    }

    /// <summary>AC-11 y RF-08: la importación fallida queda registrada y se puede consultar.</summary>
    [Fact]
    public async Task Importar_ConUnaLineaInvalida_RegistraElErrorYQuedaConsultable()
    {
        const int periodo = 202606;
        var archivo = DejarArchivo(
            "padron-error-registrado.txt",
            PadronDePrueba.Linea(periodo, PadronDePrueba.Cuit(0)),
            $"{periodo},99999999999,Empresa,904,34,B,{PadronDePrueba.Campo7}");
        var cliente = await _factory.CrearClienteAdminAsync();

        var respuesta = await cliente.PostAsJsonAsync("/api/padron/importaciones", Pedido(periodo, archivo));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);

        var fallida = await respuesta.Content.ReadFromJsonAsync<ImportacionResponse>();
        Assert.NotNull(fallida);
        Assert.Equal(EstadoImportacion.ConError, fallida.Estado);
        Assert.Equal(periodo, fallida.Periodo);
        Assert.Equal(SircipWebApplicationFactory.NombreUsuarioAdmin, fallida.Usuario);
        Assert.Contains("Línea 2", fallida.Error);

        var consultada = await (await cliente.GetAsync($"/api/padron/importaciones/{fallida.Id}"))
            .Content.ReadFromJsonAsync<ImportacionResponse>();
        Assert.Equal(EstadoImportacion.ConError, consultada!.Estado);
        Assert.Equal(fallida.Error, consultada.Error);
    }

    /// <summary>
    /// AGENTS.md: un período ya importado no se puede reimportar sin eliminarlo antes.
    /// </summary>
    [Fact]
    public async Task Importar_UnPeriodoQueYaTienePadron_DevuelveConflict()
    {
        const int periodo = 202607;
        var archivo = DejarPadronValido("padron-repetido.txt", periodo);
        var cliente = await _factory.CrearClienteAdminAsync();

        var primera = await cliente.PostAsJsonAsync("/api/padron/importaciones", Pedido(periodo, archivo));
        Assert.Equal(HttpStatusCode.OK, primera.StatusCode);

        var segunda = await cliente.PostAsJsonAsync("/api/padron/importaciones", Pedido(periodo, archivo));

        Assert.Equal(HttpStatusCode.Conflict, segunda.StatusCode);
    }

    /// <summary>
    /// La ruta la elige quien llama: el servidor solo acepta archivos de su
    /// directorio de importación.
    /// </summary>
    [Theory]
    [InlineData("../secreto.txt")]
    [InlineData("../../etc/passwd")]
    [InlineData("/etc/passwd")]
    [InlineData("subcarpeta/../../afuera.txt")]
    public async Task Importar_ConUnaRutaFueraDelDirectorioDeImportacion_DevuelveBadRequest(string ruta)
    {
        var cliente = await _factory.CrearClienteAdminAsync();

        var respuesta = await cliente.PostAsJsonAsync("/api/padron/importaciones", Pedido(202608, ruta));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Theory]
    [InlineData(2026, 0)]
    [InlineData(2026, 13)]
    [InlineData(1999, 3)]
    public async Task Importar_ConMesOAnioInvalido_DevuelveBadRequest(int anio, int mes)
    {
        var cliente = await _factory.CrearClienteAdminAsync();

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/padron/importaciones",
            new ImportarPadronRequest { Anio = anio, Mes = mes, RutaArchivo = "padron.txt" });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task Importar_SinIndicarLaRuta_DevuelveBadRequest()
    {
        var cliente = await _factory.CrearClienteAdminAsync();

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/padron/importaciones",
            new ImportarPadronRequest { Anio = 2026, Mes = 9, RutaArchivo = "   " });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    /// <summary>AC-26: el archivo que no existe queda registrado como importación fallida.</summary>
    [Fact]
    public async Task Importar_ConUnArchivoQueNoExiste_RegistraLaFallaYQuedaConsultable()
    {
        var cliente = await _factory.CrearClienteAdminAsync();

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/padron/importaciones", Pedido(202609, "no-existe.txt"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);

        var fallida = await respuesta.Content.ReadFromJsonAsync<ImportacionResponse>();
        Assert.NotNull(fallida);
        Assert.Equal(EstadoImportacion.ConError, fallida.Estado);
        Assert.Contains("No se encontró el archivo", fallida.Error);

        var consultada = await (await cliente.GetAsync($"/api/padron/importaciones/{fallida.Id}"))
            .Content.ReadFromJsonAsync<ImportacionResponse>();
        Assert.NotNull(consultada);
        Assert.Equal(EstadoImportacion.ConError, consultada.Estado);
        Assert.Equal(202609, consultada.Periodo);
        Assert.Equal(SircipWebApplicationFactory.NombreUsuarioAdmin, consultada.Usuario);
    }

    /// <summary>El período del archivo tiene que coincidir con el que se declara al importar.</summary>
    [Fact]
    public async Task Importar_ConRegistrosDeOtroPeriodo_DevuelveUnprocessableEntity()
    {
        const int periodoDeclarado = 202610;
        var archivo = DejarPadronValido("padron-otro-periodo.txt", periodo: 202512);
        var cliente = await _factory.CrearClienteAdminAsync();

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/padron/importaciones", Pedido(periodoDeclarado, archivo));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);

        var fallida = await respuesta.Content.ReadFromJsonAsync<ImportacionResponse>();
        Assert.Contains("período 202512", fallida!.Error);
        Assert.False(File.Exists(RutaBinaria(periodoDeclarado)));
    }

    [Fact]
    public async Task Importar_ConElMismoCuitDosVeces_DevuelveUnprocessableEntity()
    {
        const int periodo = 202611;
        var archivo = DejarArchivo(
            "padron-cuit-repetido.txt",
            PadronDePrueba.Linea(periodo, PadronDePrueba.Cuit(0)),
            PadronDePrueba.Linea(periodo, PadronDePrueba.Cuit(1)),
            PadronDePrueba.Linea(periodo, PadronDePrueba.Cuit(0)));
        var cliente = await _factory.CrearClienteAdminAsync();

        var respuesta = await cliente.PostAsJsonAsync("/api/padron/importaciones", Pedido(periodo, archivo));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);
        Assert.False(File.Exists(RutaBinaria(periodo)));
    }

    [Fact]
    public async Task ObtenerPorPeriodo_ConElPadronImportado_DevuelveLaConstanciaVigente()
    {
        const int periodo = 202701;
        var archivo = DejarPadronValido("padron-por-periodo.txt", periodo, cantidadRegistros: 9);
        var cliente = await _factory.CrearClienteAdminAsync();

        await cliente.PostAsJsonAsync("/api/padron/importaciones", Pedido(periodo, archivo));

        var respuesta = await cliente.GetAsync($"/api/padron/importaciones/{periodo / 100}/{periodo % 100}");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        var importacion = await respuesta.Content.ReadFromJsonAsync<ImportacionResponse>();
        Assert.NotNull(importacion);
        Assert.Equal(periodo, importacion.Periodo);
        Assert.Equal(9, importacion.CantidadRegistros);
        Assert.Equal(EstadoImportacion.Exitosa, importacion.Estado);
        Assert.Equal(SircipWebApplicationFactory.NombreUsuarioAdmin, importacion.Usuario);
    }

    /// <summary>RF-07: un período sin padrón importado responde 404.</summary>
    [Fact]
    public async Task ObtenerPorPeriodo_DeUnPeriodoSinImportar_DevuelveNotFound()
    {
        var cliente = await _factory.CrearClienteAdminAsync();

        var respuesta = await cliente.GetAsync("/api/padron/importaciones/2027/2");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    /// <summary>
    /// Un intento fallido queda en el historial pero no deja el período
    /// importado: la consulta por período tiene que seguir dando 404.
    /// </summary>
    [Fact]
    public async Task ObtenerPorPeriodo_ConSoloUnaImportacionFallida_DevuelveNotFound()
    {
        const int periodo = 202703;
        var archivo = DejarArchivo(
            "padron-fallido-por-periodo.txt",
            $"{periodo},99999999999,Empresa,904,34,B,{PadronDePrueba.Campo7}");
        var cliente = await _factory.CrearClienteAdminAsync();

        var importacion = await cliente.PostAsJsonAsync("/api/padron/importaciones", Pedido(periodo, archivo));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, importacion.StatusCode);

        var respuesta = await cliente.GetAsync($"/api/padron/importaciones/{periodo / 100}/{periodo % 100}");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    [Fact]
    public async Task ObtenerPorPeriodo_ConRolUsuario_DevuelveForbidden()
    {
        var cliente = await _factory.CrearClienteUsuarioAsync();

        var respuesta = await cliente.GetAsync("/api/padron/importaciones/2026/3");

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Theory]
    [InlineData(2026, 0)]
    [InlineData(2026, 13)]
    [InlineData(1999, 3)]
    public async Task ObtenerPorPeriodo_ConMesOAnioInvalido_DevuelveBadRequest(int anio, int mes)
    {
        var cliente = await _factory.CrearClienteAdminAsync();

        var respuesta = await cliente.GetAsync($"/api/padron/importaciones/{anio}/{mes}");

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    /// <summary>Un salto de línea de más no puede tirar abajo una importación válida.</summary>
    [Fact]
    public async Task Importar_ConLineasEnBlanco_LasSalteaEImportaElResto()
    {
        const int periodo = 202612;
        var archivo = DejarArchivo(
            "padron-con-blancos.txt",
            PadronDePrueba.Linea(periodo, PadronDePrueba.Cuit(0)),
            string.Empty,
            PadronDePrueba.Linea(periodo, PadronDePrueba.Cuit(1)),
            string.Empty);
        var cliente = await _factory.CrearClienteAdminAsync();

        var respuesta = await cliente.PostAsJsonAsync("/api/padron/importaciones", Pedido(periodo, archivo));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        var importacion = await respuesta.Content.ReadFromJsonAsync<ImportacionResponse>();
        Assert.Equal(2, importacion!.CantidadRegistros);
    }
}
