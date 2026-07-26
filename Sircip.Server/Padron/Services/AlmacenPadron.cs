using Sircip.Server.Padron.Exceptions;
using Sircip.Server.Padron.Models;
using Microsoft.Extensions.Options;

namespace Sircip.Server.Padron.Services;

/// <summary>
/// Sabe dónde vive cada archivo del padrón: los .bin que genera el sistema,
/// uno por período, y de qué carpeta se aceptan los .txt a importar.
/// </summary>
public class AlmacenPadron
{
    private readonly OpcionesPadron _opciones;

    public AlmacenPadron(IOptions<OpcionesPadron> opciones)
    {
        _opciones = opciones.Value;
    }

    public string DirectorioImportacion => Path.GetFullPath(_opciones.DirectorioImportacion);

    public string RutaDelPeriodo(int periodo) =>
        Path.Combine(Path.GetFullPath(_opciones.DirectorioDatos), $"padron-{periodo}.bin");

    public bool ExistePeriodo(int periodo) => File.Exists(RutaDelPeriodo(periodo));

    public LectorPadronBinario Abrir(int periodo) => new(RutaDelPeriodo(periodo));

    public void AsegurarDirectorios()
    {
        Directory.CreateDirectory(DirectorioImportacion);
        Directory.CreateDirectory(Path.GetFullPath(_opciones.DirectorioDatos));
    }

    /// <summary>
    /// Convierte la ruta pedida en una ruta absoluta y verifica que caiga dentro
    /// del directorio de importación. Acepta rutas relativas a ese directorio y
    /// rutas absolutas, pero rechaza cualquier cosa que se escape de él,
    /// incluidos los saltos con "..".
    /// </summary>
    public string ResolverArchivoAImportar(string rutaPedida)
    {
        if (string.IsNullOrWhiteSpace(rutaPedida))
        {
            throw new RutaNoPermitidaException(rutaPedida ?? string.Empty);
        }

        var directorioBase = DirectorioImportacion;
        var rutaCompleta = Path.GetFullPath(Path.Combine(directorioBase, rutaPedida));

        // La comparación tiene que respetar el criterio de mayúsculas del sistema
        // de archivos: en Windows 'C:\Padrones' y 'c:\padrones' son la misma
        // carpeta, y en Linux no.
        var comparacion = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        var prefijo = directorioBase.EndsWith(Path.DirectorySeparatorChar)
            ? directorioBase
            : directorioBase + Path.DirectorySeparatorChar;

        if (!rutaCompleta.StartsWith(prefijo, comparacion))
        {
            throw new RutaNoPermitidaException(rutaPedida);
        }

        return rutaCompleta;
    }
}
