using Sircip.Shared.Contracts;

namespace Sircip.Client.Services;

/// <summary>
/// Cómo le fue al pedido de importación. La API distingue varios motivos de
/// rechazo (ruta no permitida, período ya importado, archivo que no se puede
/// procesar) y cada uno se le muestra distinto al Administrador.
/// </summary>
public class ResultadoImportacion
{
    public bool Exitosa { get; }

    /// <summary>La constancia, tanto si salió bien como si quedó registrada con error.</summary>
    public ImportacionResponse? Importacion { get; }

    /// <summary>Qué salió mal, en un mensaje que se pueda mostrar tal cual.</summary>
    public string? Error { get; }

    private ResultadoImportacion(bool exitosa, ImportacionResponse? importacion, string? error)
    {
        Exitosa = exitosa;
        Importacion = importacion;
        Error = error;
    }

    public static ResultadoImportacion Ok(ImportacionResponse importacion) => new(true, importacion, null);

    public static ResultadoImportacion Fallo(string error, ImportacionResponse? importacion = null) =>
        new(false, importacion, error);
}
