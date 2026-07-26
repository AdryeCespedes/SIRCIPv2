namespace Sircip.Server.Padron;

/// <summary>
/// El archivo binario de un período no se puede leer: no tiene el encabezado
/// esperado, es de una versión de formato distinta o su tamaño no se condice
/// con la cantidad de registros que declara.
/// </summary>
public class ArchivoPadronCorruptoException : Exception
{
    public string Ruta { get; }

    public ArchivoPadronCorruptoException(string ruta, string motivo)
        : base($"El archivo de padrón '{ruta}' no se puede leer: {motivo}")
    {
        Ruta = ruta;
    }
}
