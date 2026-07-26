namespace Sircip.Server.Padron.Models;

/// <summary>
/// Disposición del archivo binario del padrón de un período: un encabezado
/// seguido de registros de ancho fijo ordenados por CUIT ascendente, para poder
/// buscar por CUIT con búsqueda binaria sobre el archivo mapeado en memoria.
///
/// Encabezado (24 bytes)
///   0  magic "SIRCIPPD"        8 bytes
///   8  versión del formato     int32
///   12 período (aaaamm)        int32
///   16 cantidad de registros   int32
///   20 largo de cada registro  int32
///
/// Registro (34 bytes)
///   0  CUIT                    int64
///   8  CRC                     byte
///   9  letra de alícuota       byte (ASCII)
///   10 estados por jurisdicción 24 bytes (ASCII '1'..'5')
///
/// Los estados se guardan ya normalizados: la posición 0 es la jurisdicción 901
/// y la 23 la 924. El Campo 7 del archivo .txt se lee al revés y con un dígito
/// de relleno, así que se da vuelta una sola vez al importar y no una vez por
/// cada cálculo.
///
/// La razón social no se guarda: ningún requerimiento la necesita para calcular
/// una percepción, y ocupaba el doble que todo el resto del registro junto.
/// </summary>
public static class FormatoPadronBinario
{
    public static ReadOnlySpan<byte> Magic => "SIRCIPPD"u8;

    public const int Version = 1;

    public const int LargoEncabezado = 24;
    public const int OffsetMagic = 0;
    public const int OffsetVersion = 8;
    public const int OffsetPeriodo = 12;
    public const int OffsetCantidadRegistros = 16;
    public const int OffsetLargoRegistro = 20;

    public const int LargoRegistro = 34;
    public const int OffsetCuit = 0;
    public const int OffsetCrc = 8;
    public const int OffsetLetraAlicuota = 9;
    public const int OffsetEstados = 10;

    public static long OffsetDeRegistro(int indice) =>
        LargoEncabezado + (long)indice * LargoRegistro;
}
