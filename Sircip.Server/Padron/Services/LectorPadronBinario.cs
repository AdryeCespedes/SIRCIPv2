using Sircip.Server.Padron.Exceptions;
using Sircip.Server.Padron.Models;
using System.IO.MemoryMappedFiles;

namespace Sircip.Server.Padron.Services;

/// <summary>
/// Busca contribuyentes por CUIT en el archivo binario de un período.
///
/// Mapea el archivo en memoria y hace búsqueda binaria sobre él: como los
/// registros son de ancho fijo y están ordenados por CUIT, la posición de cada
/// uno se calcula con una multiplicación y el sistema operativo trae del disco
/// solo las páginas que se tocan. Para un padrón de un millón de registros son
/// unas 20 comparaciones y ninguna lectura del archivo completo.
///
/// La instancia es de solo lectura y se puede compartir entre pedidos.
/// </summary>
public sealed class LectorPadronBinario : IDisposable
{
    private readonly MemoryMappedFile _archivo;
    private readonly MemoryMappedViewAccessor _vista;

    public string Ruta { get; }
    public int Periodo { get; }
    public int CantidadRegistros { get; }

    public LectorPadronBinario(string ruta)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ruta);

        Ruta = ruta;

        var tamañoArchivo = new FileInfo(ruta).Length;
        if (tamañoArchivo < FormatoPadronBinario.LargoEncabezado)
        {
            throw new ArchivoPadronCorruptoException(ruta, "es más chico que el encabezado.");
        }

        _archivo = MemoryMappedFile.CreateFromFile(
            new FileStream(ruta, FileMode.Open, FileAccess.Read, FileShare.Read),
            mapName: null,
            capacity: 0,
            MemoryMappedFileAccess.Read,
            HandleInheritability.None,
            leaveOpen: false);

        try
        {
            _vista = _archivo.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
            (Periodo, CantidadRegistros) = LeerEncabezado(ruta, tamañoArchivo);
        }
        catch
        {
            _vista?.Dispose();
            _archivo.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Devuelve el contribuyente con ese CUIT, o <c>null</c> si no está en el
    /// padrón del período (que es un caso normal: dispara RF-06 o RF-13).
    /// </summary>
    public ContribuyentePadron? Buscar(long cuit)
    {
        var izquierda = 0;
        var derecha = CantidadRegistros - 1;

        while (izquierda <= derecha)
        {
            var medio = izquierda + (derecha - izquierda) / 2;
            var cuitDelMedio = _vista.ReadInt64(
                FormatoPadronBinario.OffsetDeRegistro(medio) + FormatoPadronBinario.OffsetCuit);

            if (cuitDelMedio == cuit)
            {
                return LeerRegistro(medio, cuit);
            }

            if (cuitDelMedio < cuit)
            {
                izquierda = medio + 1;
            }
            else
            {
                derecha = medio - 1;
            }
        }

        return null;
    }

    public bool Contiene(long cuit) => Buscar(cuit) is not null;

    private ContribuyentePadron LeerRegistro(int indice, long cuit)
    {
        var offset = FormatoPadronBinario.OffsetDeRegistro(indice);

        var crc = _vista.ReadByte(offset + FormatoPadronBinario.OffsetCrc);
        var letraAlicuota = (char)_vista.ReadByte(offset + FormatoPadronBinario.OffsetLetraAlicuota);

        var estados = new char[Jurisdicciones.Cantidad];
        for (var posicion = 0; posicion < estados.Length; posicion++)
        {
            estados[posicion] = (char)_vista.ReadByte(
                offset + FormatoPadronBinario.OffsetEstados + posicion);
        }

        return new ContribuyentePadron(cuit, crc, letraAlicuota, new string(estados));
    }

    private (int Periodo, int CantidadRegistros) LeerEncabezado(string ruta, long tamañoArchivo)
    {
        Span<byte> magic = stackalloc byte[FormatoPadronBinario.Magic.Length];
        for (var i = 0; i < magic.Length; i++)
        {
            magic[i] = _vista.ReadByte(FormatoPadronBinario.OffsetMagic + i);
        }

        if (!magic.SequenceEqual(FormatoPadronBinario.Magic))
        {
            throw new ArchivoPadronCorruptoException(ruta, "no es un archivo de padrón de SIRCIP.");
        }

        var version = _vista.ReadInt32(FormatoPadronBinario.OffsetVersion);
        if (version != FormatoPadronBinario.Version)
        {
            throw new ArchivoPadronCorruptoException(
                ruta, $"es de la versión de formato {version} y se esperaba la {FormatoPadronBinario.Version}.");
        }

        var largoRegistro = _vista.ReadInt32(FormatoPadronBinario.OffsetLargoRegistro);
        if (largoRegistro != FormatoPadronBinario.LargoRegistro)
        {
            throw new ArchivoPadronCorruptoException(
                ruta, $"declara registros de {largoRegistro} bytes y se esperaban de {FormatoPadronBinario.LargoRegistro}.");
        }

        var cantidadRegistros = _vista.ReadInt32(FormatoPadronBinario.OffsetCantidadRegistros);
        if (cantidadRegistros < 0)
        {
            throw new ArchivoPadronCorruptoException(ruta, $"declara {cantidadRegistros} registros.");
        }

        var tamañoEsperado = FormatoPadronBinario.OffsetDeRegistro(cantidadRegistros);
        if (tamañoArchivo != tamañoEsperado)
        {
            throw new ArchivoPadronCorruptoException(
                ruta,
                $"declara {cantidadRegistros} registros, que son {tamañoEsperado} bytes, y el archivo tiene {tamañoArchivo}.");
        }

        return (_vista.ReadInt32(FormatoPadronBinario.OffsetPeriodo), cantidadRegistros);
    }

    public void Dispose()
    {
        _vista.Dispose();
        _archivo.Dispose();
    }
}
