namespace Sircip.Server.Padron;

/// <summary>
/// El archivo de padrón trae el mismo CUIT más de una vez en el período. La
/// importación tiene que rechazarlo entero, igual que a una línea mal formada
/// (RF-12), porque no hay forma de saber cuál de los dos registros vale.
/// </summary>
public class CuitDuplicadoException : Exception
{
    public long Cuit { get; }

    public CuitDuplicadoException(long cuit)
        : base($"El CUIT {cuit:00000000000} aparece más de una vez en el padrón del período.")
    {
        Cuit = cuit;
    }
}
