using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sircip.Server.Data;
using Sircip.Server.Models;
using Sircip.Server.Padron.Exceptions;
using Sircip.Server.Padron.Services;
using Sircip.Shared.Contracts;
using Sircip.Shared.Models;

namespace Sircip.Server.Controllers;

/// <summary>
/// Importación y consulta del padrón. Todo acá adentro es exclusivo del rol
/// Administrador (RF-02, AC-04).
/// </summary>
[ApiController]
[Route("api/padron")]
[Authorize(Roles = nameof(Rol.Administrador))]
public class PadronController : ControllerBase
{
    private const int AnioMinimo = 2000;
    private const int AnioMaximo = 9999;

    private readonly SircipDbContext _db;
    private readonly ServicioImportacionPadron _servicioImportacion;
    private readonly ServicioEliminacionPadron _servicioEliminacion;

    public PadronController(
        SircipDbContext db,
        ServicioImportacionPadron servicioImportacion,
        ServicioEliminacionPadron servicioEliminacion)
    {
        _db = db;
        _servicioImportacion = servicioImportacion;
        _servicioEliminacion = servicioEliminacion;
    }

    /// <summary>Importa el padrón de un período desde un archivo .txt (RF-03).</summary>
    [HttpPost("importaciones")]
    public async Task<ActionResult<ImportacionResponse>> Importar(
        ImportarPadronRequest request,
        CancellationToken cancelacion)
    {
        if (!TryArmarPeriodo(request.Anio, request.Mes, out var periodo, out var errorPeriodo))
        {
            return BadRequest(errorPeriodo);
        }

        if (string.IsNullOrWhiteSpace(request.RutaArchivo))
        {
            return BadRequest("Debe indicar la ruta del archivo del padrón.");
        }

        Importacion importacion;
        try
        {
            importacion = await _servicioImportacion.ImportarAsync(
                periodo, request.RutaArchivo, UsuarioId(), cancelacion);
        }
        catch (RutaNoPermitidaException excepcion)
        {
            return BadRequest(excepcion.Message);
        }
        catch (PeriodoYaImportadoException excepcion)
        {
            return Conflict(excepcion.Message);
        }

        var respuesta = AResponse(importacion, NombreUsuario());

        if (importacion.Estado == EstadoImportacion.ConError)
        {
            return UnprocessableEntity(respuesta);
        }

        return Ok(respuesta);
    }

    /// <summary>Consulta la constancia de una importación (RF-04, RF-08).</summary>
    [HttpGet("importaciones/{id:int}")]
    public async Task<ActionResult<ImportacionResponse>> ObtenerImportacion(int id, CancellationToken cancelacion)
    {
        var importacion = await _db.Importaciones
            .Include(i => i.Usuario)
            .SingleOrDefaultAsync(i => i.Id == id, cancelacion);

        if (importacion is null)
        {
            return NotFound();
        }

        return Ok(AResponse(importacion, importacion.Usuario.NombreUsuario));
    }

    /// <summary>
    /// Devuelve el padrón vigente de un período, o 404 si ese período no tiene
    /// ninguno importado (RF-07).
    ///
    /// Un período puede tener muchas constancias —los intentos fallidos quedan
    /// registrados y las eliminadas también—, pero vigente hay una sola, así que
    /// es esa la que se devuelve. El historial completo es la página de RF-10.
    /// </summary>
    [HttpGet("importaciones/{anio:int}/{mes:int}")]
    public async Task<ActionResult<ImportacionResponse>> ObtenerImportacionDelPeriodo(
        int anio,
        int mes,
        CancellationToken cancelacion)
    {
        if (!TryArmarPeriodo(anio, mes, out var periodo, out var error))
        {
            return BadRequest(error);
        }

        var importacion = await _db.Importaciones
            .Include(i => i.Usuario)
            .SingleOrDefaultAsync(
                i => i.Periodo == periodo && i.Estado == EstadoImportacion.Exitosa, cancelacion);

        if (importacion is null)
        {
            return NotFound($"El período {periodo} no tiene un padrón importado.");
        }

        return Ok(AResponse(importacion, importacion.Usuario.NombreUsuario));
    }

    /// <summary>
    /// Elimina el padrón de un período con borrado lógico (RF-09). La constancia
    /// sigue en el historial marcada como borrada, y el período queda libre para
    /// volver a importarse.
    /// </summary>
    [HttpDelete("importaciones/{anio:int}/{mes:int}")]
    public async Task<ActionResult<ImportacionResponse>> EliminarPadronDelPeriodo(
        int anio,
        int mes,
        CancellationToken cancelacion)
    {
        if (!TryArmarPeriodo(anio, mes, out var periodo, out var error))
        {
            return BadRequest(error);
        }

        var importacion = await _servicioEliminacion.EliminarAsync(periodo, cancelacion);

        if (importacion is null)
        {
            return NotFound($"El período {periodo} no tiene un padrón importado que eliminar.");
        }

        return Ok(AResponse(importacion, importacion.Usuario.NombreUsuario));
    }

    private static bool TryArmarPeriodo(int anio, int mes, out int periodo, out string? error)
    {
        periodo = 0;

        if (mes is < 1 or > 12)
        {
            error = "El mes debe estar entre 1 y 12.";
            return false;
        }

        if (anio is < AnioMinimo or > AnioMaximo)
        {
            error = $"El año debe estar entre {AnioMinimo} y {AnioMaximo}.";
            return false;
        }

        periodo = anio * 100 + mes;
        error = null;
        return true;
    }

    private static ImportacionResponse AResponse(Importacion importacion, string nombreUsuario) => new()
    {
        Id = importacion.Id,
        Periodo = importacion.Periodo,
        FechaImportacionUtc = importacion.FechaImportacionUtc,
        Usuario = nombreUsuario,
        CantidadRegistros = importacion.CantidadRegistros,
        Estado = importacion.Estado,
        Error = importacion.Error
    };

    private int UsuarioId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private string NombreUsuario() => User.FindFirstValue(ClaimTypes.Name)!;
}
