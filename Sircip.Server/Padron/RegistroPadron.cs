namespace Sircip.Server.Padron;

/// <summary>
/// Registro de contribuyente del padrón, tal como viene en cada línea del
/// archivo .txt de importación (Anexo A del PRD).
/// </summary>
public sealed class RegistroPadron
{
    /// <summary>Campo 1: período del padrón en formato aaaamm.</summary>
    public required int Periodo { get; init; }

    /// <summary>Campo 2: CUIT del contribuyente, 11 dígitos.</summary>
    public required long Cuit { get; init; }

    /// <summary>Campo 3: razón social del contribuyente.</summary>
    public required string RazonSocial { get; init; }

    /// <summary>Campo 4: jurisdicción sede del contribuyente, 3 dígitos.</summary>
    public required short JurisdiccionSede { get; init; }

    /// <summary>
    /// Campo 5: código de redundancia cíclica del contribuyente para el período.
    /// Se conserva porque después se declara en el Campo 2 de la DDJJ.
    /// </summary>
    public required byte Crc { get; init; }

    /// <summary>Campo 6: letra del set de alícuotas (ver <see cref="SetAlicuotas"/>).</summary>
    public required char LetraAlicuota { get; init; }

    /// <summary>
    /// Campo 7: estado del contribuyente por jurisdicción, 25 dígitos que se
    /// leen de derecha a izquierda descartando el último.
    /// </summary>
    public required string Campo7 { get; init; }
}
