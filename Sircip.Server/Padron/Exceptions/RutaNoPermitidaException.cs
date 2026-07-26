namespace Sircip.Server.Padron.Exceptions;

/// <summary>
/// Se pidió importar un archivo que está fuera del directorio de importación
/// configurado. Como la ruta la elige quien llama al endpoint, sin este límite
/// un administrador podría hacer que el servidor abriera cualquier archivo del
/// disco.
/// </summary>
public class RutaNoPermitidaException : Exception
{
    public string RutaPedida { get; }

    public RutaNoPermitidaException(string rutaPedida)
        : base($"La ruta '{rutaPedida}' está fuera del directorio de importación configurado.")
    {
        RutaPedida = rutaPedida;
    }
}
