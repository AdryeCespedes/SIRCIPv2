using Microsoft.EntityFrameworkCore;
using Sircip.Server.Data;
using Sircip.Server.Models;
using Sircip.Shared.Models;

namespace Sircip.Server.Padron.Services;

/// <summary>
/// Elimina el padrón de un período con borrado lógico (RF-09): la constancia de
/// la importación no se borra ni desaparece del historial, se marca con estado
/// Borrada, y el período pasa a considerarse no importado.
/// </summary>
public class ServicioEliminacionPadron
{
    private readonly SircipDbContext _db;
    private readonly AlmacenPadron _almacen;
    private readonly ILogger<ServicioEliminacionPadron> _logger;

    public ServicioEliminacionPadron(
        SircipDbContext db,
        AlmacenPadron almacen,
        ILogger<ServicioEliminacionPadron> logger)
    {
        _db = db;
        _almacen = almacen;
        _logger = logger;
    }

    /// <summary>
    /// Devuelve la constancia ya marcada como borrada, o <c>null</c> si el
    /// período no tenía ningún padrón vigente que eliminar.
    /// </summary>
    public async Task<Importacion?> EliminarAsync(int periodo, CancellationToken cancelacion = default)
    {
        var importacion = await _db.Importaciones
            .Include(i => i.Usuario)
            .SingleOrDefaultAsync(
                i => i.Periodo == periodo && i.Estado == EstadoImportacion.Exitosa, cancelacion);

        if (importacion is null)
        {
            return null;
        }

        importacion.Estado = EstadoImportacion.Borrada;
        await _db.SaveChangesAsync(cancelacion);

        // El archivo se borra después de la constancia y no antes. Si se cayera
        // en el medio, el período ya figura como no importado y el archivo que
        // sobra no lo lee nadie. Al revés quedaría un período que dice estar
        // importado pero sin datos, y encima no se podría reimportar.
        try
        {
            _almacen.EliminarPeriodo(periodo);
        }
        catch (Exception excepcion) when (excepcion is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(
                excepcion,
                "El padrón del período {Periodo} quedó borrado, pero no se pudo eliminar su archivo.",
                periodo);
        }

        _logger.LogInformation("Padrón del período {Periodo} eliminado.", periodo);

        return importacion;
    }
}
