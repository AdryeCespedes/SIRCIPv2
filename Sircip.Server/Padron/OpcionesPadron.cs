namespace Sircip.Server.Padron;

public class OpcionesPadron
{
    public const string SectionName = "Padron";

    /// <summary>
    /// Única carpeta del disco del servidor de la que se aceptan archivos .txt
    /// para importar. Todo lo que se pida importar tiene que estar acá adentro.
    /// </summary>
    public required string DirectorioImportacion { get; set; }

    /// <summary>Carpeta donde el sistema guarda los archivos binarios por período.</summary>
    public required string DirectorioDatos { get; set; }
}
