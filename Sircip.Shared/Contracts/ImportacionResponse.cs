using Sircip.Shared.Models;

namespace Sircip.Shared.Contracts;

/// <summary>
/// Constancia de una importación del padrón (RF-04). Es lo que se devuelve
/// tanto al importar como al consultar el historial, y también cuando la
/// importación falla (RF-08).
/// </summary>
public class ImportacionResponse
{
    public required int Id { get; set; }

    /// <summary>Período del padrón en formato aaaamm.</summary>
    public required int Periodo { get; set; }

    public required DateTime FechaImportacionUtc { get; set; }

    public required string Usuario { get; set; }

    public required int CantidadRegistros { get; set; }

    public required EstadoImportacion Estado { get; set; }

    /// <summary>Motivo del rechazo cuando el estado es ConError.</summary>
    public string? Error { get; set; }
}
