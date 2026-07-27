using Sircip.Shared.Contracts;

namespace Sircip.Client.Services;

/// <summary>Cómo le fue al pedido de eliminación del padrón de un período.</summary>
public class ResultadoEliminacion
{
    public bool Exitosa { get; }

    /// <summary>La constancia ya marcada como borrada.</summary>
    public ImportacionResponse? Importacion { get; }

    public string? Error { get; }

    private ResultadoEliminacion(bool exitosa, ImportacionResponse? importacion, string? error)
    {
        Exitosa = exitosa;
        Importacion = importacion;
        Error = error;
    }

    public static ResultadoEliminacion Ok(ImportacionResponse importacion) => new(true, importacion, null);

    public static ResultadoEliminacion Fallo(string error) => new(false, null, error);
}
