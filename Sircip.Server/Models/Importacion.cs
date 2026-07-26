using Sircip.Shared.Models;

namespace Sircip.Server.Models;

/// <summary>
/// Constancia de un intento de importación del padrón de un período (RF-04).
/// Las fallidas también se guardan, para que queden disponibles para consulta
/// (RF-08), y las eliminadas no se borran: cambian de estado (RF-09).
/// </summary>
public class Importacion
{
    public int Id { get; set; }

    /// <summary>Período del padrón en formato aaaamm.</summary>
    public int Periodo { get; set; }

    public DateTime FechaImportacionUtc { get; set; }

    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public int CantidadRegistros { get; set; }

    public EstadoImportacion Estado { get; set; }

    public string? Error { get; set; }
}
