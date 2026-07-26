namespace Sircip.Server.Padron.Exceptions;

/// <summary>
/// El período ya tiene un padrón importado vigente. No se puede reimportar ni
/// modificar parcialmente: primero hay que eliminarlo con el borrado lógico
/// (RF-09) y recién después volver a importarlo.
/// </summary>
public class PeriodoYaImportadoException : Exception
{
    public int Periodo { get; }

    public PeriodoYaImportadoException(int periodo)
        : base($"El período {periodo} ya tiene un padrón importado. Hay que eliminarlo antes de volver a importarlo.")
    {
        Periodo = periodo;
    }
}
