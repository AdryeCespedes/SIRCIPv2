namespace Sircip.Server.Padron.Exceptions;

/// <summary>
/// El archivo no se puede importar: falta, tiene una línea que no cumple el
/// Anexo A, o sus registros son de otro período. Dispara el rechazo completo
/// del archivo (RF-12) y queda registrada como importación con error (RF-08).
/// </summary>
public class ImportacionInvalidaException : Exception
{
    public ImportacionInvalidaException(string mensaje) : base(mensaje)
    {
    }
}
