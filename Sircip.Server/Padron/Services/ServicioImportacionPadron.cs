using Sircip.Server.Padron.Exceptions;
using Microsoft.EntityFrameworkCore;
using Sircip.Server.Data;
using Sircip.Server.Models;
using Sircip.Shared.Models;

namespace Sircip.Server.Padron.Services;

/// <summary>
/// Importa el padrón de un período: valida el archivo entero, escribe el
/// binario y deja la constancia en la base.
///
/// Es todo o nada (RF-12): primero se parsean y validan todas las líneas en
/// memoria, y recién cuando no quedó ninguna afuera se escribe el archivo del
/// período. Si algo falla no se persiste ningún registro, pero sí queda la
/// constancia del intento fallido (RF-08).
/// </summary>
public class ServicioImportacionPadron
{
    private readonly SircipDbContext _db;
    private readonly AlmacenPadron _almacen;
    private readonly ILogger<ServicioImportacionPadron> _logger;

    public ServicioImportacionPadron(
        SircipDbContext db,
        AlmacenPadron almacen,
        ILogger<ServicioImportacionPadron> logger)
    {
        _db = db;
        _almacen = almacen;
        _logger = logger;
    }

    /// <summary>
    /// Devuelve la constancia de la importación, exitosa o con error. Lanza
    /// <see cref="RutaNoPermitidaException"/> o
    /// <see cref="PeriodoYaImportadoException"/> cuando ni siquiera se llega a
    /// intentar la importación, y en esos casos no deja constancia.
    /// </summary>
    public async Task<Importacion> ImportarAsync(
        int periodo,
        string rutaPedida,
        int usuarioId,
        CancellationToken cancelacion = default)
    {
        var rutaArchivo = _almacen.ResolverArchivoAImportar(rutaPedida);

        var yaImportado = await _db.Importaciones
            .AnyAsync(i => i.Periodo == periodo && i.Estado == EstadoImportacion.Exitosa, cancelacion);
        if (yaImportado)
        {
            throw new PeriodoYaImportadoException(periodo);
        }

        int cantidadRegistros;
        try
        {
            cantidadRegistros = EscribirPadron(periodo, rutaArchivo);
        }
        catch (Exception excepcion) when (
            excepcion is ImportacionInvalidaException or CuitDuplicadoException
                or IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(
                excepcion, "Falló la importación del padrón del período {Periodo}.", periodo);

            return await RegistrarAsync(
                periodo, usuarioId, 0, EstadoImportacion.ConError, excepcion.Message, cancelacion);
        }

        try
        {
            return await RegistrarAsync(
                periodo, usuarioId, cantidadRegistros, EstadoImportacion.Exitosa, null, cancelacion);
        }
        catch
        {
            // Sin la constancia en la base el padrón quedaría huérfano: el
            // período no figuraría como importado pero el archivo existiría.
            File.Delete(_almacen.RutaDelPeriodo(periodo));
            throw;
        }
    }

    private int EscribirPadron(int periodo, string rutaArchivo)
    {
        if (!File.Exists(rutaArchivo))
        {
            throw new ImportacionInvalidaException($"No se encontró el archivo '{rutaArchivo}'.");
        }

        var escritor = new EscritorPadronBinario(periodo);
        var numeroLinea = 0;

        foreach (var linea in File.ReadLines(rutaArchivo))
        {
            numeroLinea++;

            // Una línea en blanco no aporta ningún registro, así que se saltea en
            // vez de tirar abajo el archivo entero por un salto de línea de más.
            if (string.IsNullOrWhiteSpace(linea))
            {
                continue;
            }

            if (!ParserPadron.TryParsear(linea, out var registro, out var error))
            {
                throw new ImportacionInvalidaException($"Línea {numeroLinea}: {error}");
            }

            if (registro.Periodo != periodo)
            {
                throw new ImportacionInvalidaException(
                    $"Línea {numeroLinea}: el registro es del período {registro.Periodo} y se está importando el {periodo}.");
            }

            escritor.Agregar(registro);
        }

        _almacen.AsegurarDirectorios();

        // Se escribe en un temporal y se mueve al final, para que una caída a
        // mitad de la escritura no deje un padrón incompleto en uso.
        var rutaFinal = _almacen.RutaDelPeriodo(periodo);
        var rutaTemporal = rutaFinal + ".tmp";
        try
        {
            using (var archivo = File.Create(rutaTemporal))
            {
                escritor.Escribir(archivo);
            }

            File.Move(rutaTemporal, rutaFinal, overwrite: true);
        }
        catch
        {
            File.Delete(rutaTemporal);
            throw;
        }

        _logger.LogInformation(
            "Padrón del período {Periodo} importado con {Cantidad} registros.", periodo, escritor.Cantidad);

        return escritor.Cantidad;
    }

    private async Task<Importacion> RegistrarAsync(
        int periodo,
        int usuarioId,
        int cantidadRegistros,
        EstadoImportacion estado,
        string? error,
        CancellationToken cancelacion)
    {
        var importacion = new Importacion
        {
            Periodo = periodo,
            FechaImportacionUtc = DateTime.UtcNow,
            UsuarioId = usuarioId,
            CantidadRegistros = cantidadRegistros,
            Estado = estado,
            Error = error
        };

        _db.Importaciones.Add(importacion);
        await _db.SaveChangesAsync(cancelacion);

        return importacion;
    }
}
