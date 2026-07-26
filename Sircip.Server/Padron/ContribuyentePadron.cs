namespace Sircip.Server.Padron;

/// <summary>
/// Contribuyente encontrado en el padrón de un período. Es lo que devuelve el
/// lector y lo que después consume el cálculo de percepciones.
/// </summary>
public sealed class ContribuyentePadron
{
    private readonly string _estadosPorJurisdiccion;

    public long Cuit { get; }

    /// <summary>Se conserva porque se declara en el Campo 2 de la DDJJ (Anexo A, nota 1).</summary>
    public byte Crc { get; }

    public char LetraAlicuota { get; }

    public ContribuyentePadron(long cuit, byte crc, char letraAlicuota, string estadosPorJurisdiccion)
    {
        if (estadosPorJurisdiccion.Length != Jurisdicciones.Cantidad)
        {
            throw new ArgumentException(
                $"Se esperaban {Jurisdicciones.Cantidad} estados y se recibieron {estadosPorJurisdiccion.Length}.",
                nameof(estadosPorJurisdiccion));
        }

        Cuit = cuit;
        Crc = crc;
        LetraAlicuota = letraAlicuota;
        _estadosPorJurisdiccion = estadosPorJurisdiccion;
    }

    /// <summary>
    /// Estado del contribuyente en la jurisdicción indicada (código 901 a 924):
    /// '1' inscripto, '2' no inscripto con sobretasa, '3' no inscripto sin
    /// sobretasa, '4' y '5' jurisdicción no adherida.
    /// </summary>
    public char EstadoDeJurisdiccion(int codigoJurisdiccion) =>
        _estadosPorJurisdiccion[Jurisdicciones.APosicion(codigoJurisdiccion)];

    /// <summary>Los 24 estados ya normalizados, de la jurisdicción 901 a la 924.</summary>
    public string EstadosPorJurisdiccion => _estadosPorJurisdiccion;
}
