using System.Buffers.Binary;

namespace Sircip.Server.Padron;

/// <summary>
/// Arma el archivo binario del padrón de un período: se le van agregando los
/// registros ya validados y al escribir los ordena por CUIT.
///
/// Acumula los registros en un buffer de bytes en vez de guardar los
/// <see cref="RegistroPadron"/>, así el que importa puede ir descartando cada
/// línea parseada. Con un millón de registros el buffer queda en unos 34 MB, y
/// no en los cientos que ocuparían un millón de objetos con sus strings.
///
/// El ordenamiento se hace sobre un vector de índices y no moviendo los bytes:
/// se ordenan los CUIT llevando de la mano las posiciones, y recién al escribir
/// se recorren los registros en ese orden.
/// </summary>
public sealed class EscritorPadronBinario
{
    private const int CapacidadInicialPorDefecto = 1024;

    private readonly int _periodo;

    private byte[] _registros;
    private long[] _cuits;
    private int _cantidad;

    public EscritorPadronBinario(int periodo, int capacidadInicial = CapacidadInicialPorDefecto)
    {
        if (capacidadInicial <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(capacidadInicial), capacidadInicial, "La capacidad inicial debe ser mayor a cero.");
        }

        _periodo = periodo;
        _registros = new byte[(long)capacidadInicial * FormatoPadronBinario.LargoRegistro];
        _cuits = new long[capacidadInicial];
    }

    public int Cantidad => _cantidad;

    public void Agregar(RegistroPadron registro)
    {
        ArgumentNullException.ThrowIfNull(registro);

        if (registro.Campo7.Length != ParserPadron.LargoCampo7)
        {
            throw new ArgumentException(
                $"El Campo 7 debe tener {ParserPadron.LargoCampo7} dígitos.", nameof(registro));
        }

        AsegurarCapacidad(_cantidad + 1);

        var destino = _registros.AsSpan(
            _cantidad * FormatoPadronBinario.LargoRegistro, FormatoPadronBinario.LargoRegistro);

        BinaryPrimitives.WriteInt64LittleEndian(destino[FormatoPadronBinario.OffsetCuit..], registro.Cuit);
        destino[FormatoPadronBinario.OffsetCrc] = registro.Crc;
        destino[FormatoPadronBinario.OffsetLetraAlicuota] = (byte)registro.LetraAlicuota;

        // El Campo 7 se lee de derecha a izquierda descartando el último dígito:
        // la jurisdicción 901 es la anteúltima posición y la 924 la primera.
        for (var posicion = 0; posicion < Jurisdicciones.Cantidad; posicion++)
        {
            var enElArchivo = registro.Campo7[ParserPadron.LargoCampo7 - 2 - posicion];
            destino[FormatoPadronBinario.OffsetEstados + posicion] = (byte)enElArchivo;
        }

        _cuits[_cantidad] = registro.Cuit;
        _cantidad++;
    }

    /// <summary>
    /// Ordena los registros por CUIT y los escribe con su encabezado. Devuelve
    /// la cantidad de registros escritos.
    /// </summary>
    public int Escribir(Stream destino)
    {
        ArgumentNullException.ThrowIfNull(destino);

        var cuitsOrdenados = _cuits[.._cantidad];
        var orden = new int[_cantidad];
        for (var i = 0; i < _cantidad; i++)
        {
            orden[i] = i;
        }

        Array.Sort(cuitsOrdenados, orden);
        VerificarQueNoHayaCuitsRepetidos(cuitsOrdenados);

        EscribirEncabezado(destino);

        foreach (var indice in orden)
        {
            destino.Write(_registros.AsSpan(
                indice * FormatoPadronBinario.LargoRegistro, FormatoPadronBinario.LargoRegistro));
        }

        destino.Flush();
        return _cantidad;
    }

    private static void VerificarQueNoHayaCuitsRepetidos(ReadOnlySpan<long> cuitsOrdenados)
    {
        for (var i = 1; i < cuitsOrdenados.Length; i++)
        {
            if (cuitsOrdenados[i] == cuitsOrdenados[i - 1])
            {
                throw new CuitDuplicadoException(cuitsOrdenados[i]);
            }
        }
    }

    private void EscribirEncabezado(Stream destino)
    {
        Span<byte> encabezado = stackalloc byte[FormatoPadronBinario.LargoEncabezado];
        encabezado.Clear();

        FormatoPadronBinario.Magic.CopyTo(encabezado[FormatoPadronBinario.OffsetMagic..]);
        BinaryPrimitives.WriteInt32LittleEndian(
            encabezado[FormatoPadronBinario.OffsetVersion..], FormatoPadronBinario.Version);
        BinaryPrimitives.WriteInt32LittleEndian(
            encabezado[FormatoPadronBinario.OffsetPeriodo..], _periodo);
        BinaryPrimitives.WriteInt32LittleEndian(
            encabezado[FormatoPadronBinario.OffsetCantidadRegistros..], _cantidad);
        BinaryPrimitives.WriteInt32LittleEndian(
            encabezado[FormatoPadronBinario.OffsetLargoRegistro..], FormatoPadronBinario.LargoRegistro);

        destino.Write(encabezado);
    }

    private void AsegurarCapacidad(int cantidadNecesaria)
    {
        if (cantidadNecesaria <= _cuits.Length)
        {
            return;
        }

        var nuevaCapacidad = _cuits.Length * 2;
        while (nuevaCapacidad < cantidadNecesaria)
        {
            nuevaCapacidad *= 2;
        }

        Array.Resize(ref _registros, nuevaCapacidad * FormatoPadronBinario.LargoRegistro);
        Array.Resize(ref _cuits, nuevaCapacidad);
    }
}
