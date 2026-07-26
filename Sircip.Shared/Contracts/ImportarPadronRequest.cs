namespace Sircip.Shared.Contracts;

/// <summary>
/// Pedido de importación del padrón (RF-03): el mes y el año del período y la
/// ubicación del archivo .txt en el disco del servidor.
/// </summary>
public class ImportarPadronRequest
{
    public required int Anio { get; set; }
    public required int Mes { get; set; }

    /// <summary>
    /// Ruta del archivo. Puede ser relativa al directorio de importación
    /// configurado o absoluta, pero siempre tiene que quedar dentro de él.
    /// </summary>
    public required string RutaArchivo { get; set; }
}
