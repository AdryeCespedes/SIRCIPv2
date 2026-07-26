namespace Sircip.Shared.Models;

public enum EstadoImportacion
{
    /// <summary>El padrón del período se importó y está vigente.</summary>
    Exitosa,

    /// <summary>La importación falló y no se persistió ningún registro del período (RF-08, RF-12).</summary>
    ConError,

    /// <summary>El padrón se eliminó con borrado lógico y el registro queda en el historial (RF-09).</summary>
    Borrada
}
