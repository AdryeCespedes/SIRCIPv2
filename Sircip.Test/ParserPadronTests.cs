using Sircip.Server.Padron.Models;
using Sircip.Server.Padron.Services;

namespace Sircip.Test;

/// <summary>
/// RF-11: cada línea del padrón se valida contra el diseño de registro del Anexo A.
/// </summary>
public class ParserPadronTests
{
    /// <summary>Registro de ejemplo del Anexo A.</summary>
    private const string LineaValida = "202603,30100100106,Empresa de prueba,904,34,B,5225355222512555552512420";

    /// <summary>Reemplaza un campo de la línea de ejemplo para probar su validación por separado.</summary>
    private static string ConCampo(int numeroCampo, string valor)
    {
        var campos = LineaValida.Split(',');
        campos[numeroCampo - 1] = valor;
        return string.Join(',', campos);
    }

    [Fact]
    public void TryParsear_ConElRegistroDeEjemploDelAnexoA_DevuelveTodosLosCampos()
    {
        var esValido = ParserPadron.TryParsear(LineaValida, out var registro, out var error);

        Assert.True(esValido, error);
        Assert.Null(error);
        Assert.Equal(202603, registro!.Periodo);
        Assert.Equal(30100100106L, registro.Cuit);
        Assert.Equal("Empresa de prueba", registro.RazonSocial);
        Assert.Equal((short)904, registro.JurisdiccionSede);
        Assert.Equal((byte)34, registro.Crc);
        Assert.Equal('B', registro.LetraAlicuota);
        Assert.Equal("5225355222512555552512420", registro.Campo7);
    }

    [Theory]
    [InlineData("")]
    [InlineData("202603")]
    [InlineData("202603,30100100106,Empresa de prueba,904,34,B")]
    public void TryParsear_ConMenosCamposDeLosEsperados_Rechaza(string linea)
    {
        var esValido = ParserPadron.TryParsear(linea, out var registro, out var error);

        Assert.False(esValido);
        Assert.Null(registro);
        Assert.Contains("campos separados por coma", error);
    }

    /// <summary>
    /// El archivo es un CSV sin comillas, así que una coma dentro de la razón
    /// social genera un campo de más y la línea se rechaza.
    /// </summary>
    [Fact]
    public void TryParsear_ConUnaComaDentroDeLaRazonSocial_Rechaza()
    {
        var linea = ConCampo(3, "Empresa de prueba, S.A.");

        var esValido = ParserPadron.TryParsear(linea, out var registro, out var error);

        Assert.False(esValido);
        Assert.Null(registro);
        Assert.Contains("campos separados por coma", error);
    }

    [Theory]
    [InlineData("20260")]
    [InlineData("2026030")]
    [InlineData("2026o3")]
    [InlineData("")]
    public void TryParsear_ConPeriodoQueNoTieneSeisDigitos_Rechaza(string periodo)
    {
        var esValido = ParserPadron.TryParsear(ConCampo(1, periodo), out _, out var error);

        Assert.False(esValido);
        Assert.Contains("Campo 1", error);
    }

    [Theory]
    [InlineData("202600")]
    [InlineData("202613")]
    [InlineData("202699")]
    public void TryParsear_ConMesFueraDeRango_Rechaza(string periodo)
    {
        var esValido = ParserPadron.TryParsear(ConCampo(1, periodo), out _, out var error);

        Assert.False(esValido);
        Assert.Contains("Campo 1", error);
    }

    [Theory]
    [InlineData("202601")]
    [InlineData("202612")]
    public void TryParsear_ConMesEnLosBordesDelRango_Acepta(string periodo)
    {
        var esValido = ParserPadron.TryParsear(ConCampo(1, periodo), out var registro, out var error);

        Assert.True(esValido, error);
        Assert.Equal(int.Parse(periodo), registro!.Periodo);
    }

    [Theory]
    [InlineData("3010010010")]
    [InlineData("301001001060")]
    [InlineData("3010010010A")]
    [InlineData("")]
    public void TryParsear_ConCuitQueNoTieneOnceDigitos_Rechaza(string cuit)
    {
        var esValido = ParserPadron.TryParsear(ConCampo(2, cuit), out _, out var error);

        Assert.False(esValido);
        Assert.Contains("Campo 2", error);
    }

    [Theory]
    [InlineData("33693450239")]
    [InlineData("20123456786")]
    [InlineData("27123456780")]
    [InlineData("23200000039")]
    public void TryParsear_ConCuitDeDigitoVerificadorCorrecto_Acepta(string cuit)
    {
        var esValido = ParserPadron.TryParsear(ConCampo(2, cuit), out var registro, out var error);

        Assert.True(esValido, error);
        Assert.Equal(long.Parse(cuit), registro!.Cuit);
    }

    [Theory]
    [InlineData("30100100105")]
    [InlineData("33693450230")]
    [InlineData("20123456789")]
    // Le corresponde el dígito 9 por el caso especial de resto 1, no el 0.
    [InlineData("23200000030")]
    public void TryParsear_ConCuitDeDigitoVerificadorIncorrecto_Rechaza(string cuit)
    {
        var esValido = ParserPadron.TryParsear(ConCampo(2, cuit), out _, out var error);

        Assert.False(esValido);
        Assert.Contains("dígito verificador", error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void TryParsear_ConRazonSocialVacia_Rechaza(string razonSocial)
    {
        var esValido = ParserPadron.TryParsear(ConCampo(3, razonSocial), out _, out var error);

        Assert.False(esValido);
        Assert.Contains("Campo 3", error);
    }

    [Fact]
    public void TryParsear_ConRazonSocialDeSetentaCaracteres_Acepta()
    {
        var razonSocial = new string('A', ParserPadron.LargoMaximoRazonSocial);

        var esValido = ParserPadron.TryParsear(ConCampo(3, razonSocial), out var registro, out var error);

        Assert.True(esValido, error);
        Assert.Equal(razonSocial, registro!.RazonSocial);
    }

    [Fact]
    public void TryParsear_ConRazonSocialDeMasDeSetentaCaracteres_Rechaza()
    {
        var razonSocial = new string('A', ParserPadron.LargoMaximoRazonSocial + 1);

        var esValido = ParserPadron.TryParsear(ConCampo(3, razonSocial), out _, out var error);

        Assert.False(esValido);
        Assert.Contains("Campo 3", error);
    }

    [Fact]
    public void TryParsear_ConRazonSocialConEspaciosAlrededor_LosDescarta()
    {
        var esValido = ParserPadron.TryParsear(ConCampo(3, "  Empresa de prueba  "), out var registro, out var error);

        Assert.True(esValido, error);
        Assert.Equal("Empresa de prueba", registro!.RazonSocial);
    }

    [Theory]
    [InlineData("90")]
    [InlineData("9040")]
    [InlineData("90A")]
    [InlineData("")]
    public void TryParsear_ConJurisdiccionSedeQueNoTieneTresDigitos_Rechaza(string jurisdiccion)
    {
        var esValido = ParserPadron.TryParsear(ConCampo(4, jurisdiccion), out _, out var error);

        Assert.False(esValido);
        Assert.Contains("Campo 4", error);
    }

    [Theory]
    [InlineData("3")]
    [InlineData("340")]
    [InlineData("3A")]
    [InlineData("")]
    public void TryParsear_ConCrcQueNoTieneDosDigitos_Rechaza(string crc)
    {
        var esValido = ParserPadron.TryParsear(ConCampo(5, crc), out _, out var error);

        Assert.False(esValido);
        Assert.Contains("Campo 5", error);
    }

    /// <summary>El Anexo A define el CRC como un valor de 10 a 99.</summary>
    [Theory]
    [InlineData("00")]
    [InlineData("09")]
    public void TryParsear_ConCrcMenorADiez_Rechaza(string crc)
    {
        var esValido = ParserPadron.TryParsear(ConCampo(5, crc), out _, out var error);

        Assert.False(esValido);
        Assert.Contains("Campo 5", error);
    }

    [Theory]
    [InlineData("10")]
    [InlineData("99")]
    public void TryParsear_ConCrcEnLosBordesDelRango_Acepta(string crc)
    {
        var esValido = ParserPadron.TryParsear(ConCampo(5, crc), out var registro, out var error);

        Assert.True(esValido, error);
        Assert.Equal(byte.Parse(crc), registro!.Crc);
    }

    [Theory]
    [InlineData("A")]
    [InlineData("X")]
    [InlineData("S")]
    public void TryParsear_ConLetraDeAlicuotaDelSet_Acepta(string letra)
    {
        var esValido = ParserPadron.TryParsear(ConCampo(6, letra), out var registro, out var error);

        Assert.True(esValido, error);
        Assert.Equal(letra[0], registro!.LetraAlicuota);
    }

    [Theory]
    [InlineData("Y")]
    [InlineData("Z")]
    [InlineData("b")]
    [InlineData("1")]
    [InlineData("BB")]
    [InlineData("")]
    public void TryParsear_ConLetraDeAlicuotaFueraDelSet_Rechaza(string letra)
    {
        var esValido = ParserPadron.TryParsear(ConCampo(6, letra), out _, out var error);

        Assert.False(esValido);
        Assert.Contains("Campo 6", error);
    }

    [Theory]
    [InlineData("522535522251255555251242")]
    [InlineData("52253552225125555525124200")]
    [InlineData("522535522251255555251242O")]
    [InlineData("")]
    public void TryParsear_ConCampo7QueNoTieneVeinticincoDigitos_Rechaza(string campo7)
    {
        var esValido = ParserPadron.TryParsear(ConCampo(7, campo7), out _, out var error);

        Assert.False(esValido);
        Assert.Contains("Campo 7", error);
    }

    [Fact]
    public void TryParsear_ConCampo7QueNoTerminaEnCero_Rechaza()
    {
        var esValido = ParserPadron.TryParsear(ConCampo(7, "5225355222512555552512421"), out _, out var error);

        Assert.False(esValido);
        Assert.Contains("última posición", error);
    }

    /// <summary>Los estados válidos por jurisdicción son 1, 2, 3, 4 y 5.</summary>
    [Theory]
    [InlineData('0')]
    [InlineData('6')]
    [InlineData('9')]
    public void TryParsear_ConUnEstadoDeJurisdiccionFueraDeRango_Rechaza(char estado)
    {
        var campo7 = (new string('5', ParserPadron.LargoCampo7 - 1) + '0').ToCharArray();
        campo7[0] = estado;

        var esValido = ParserPadron.TryParsear(ConCampo(7, new string(campo7)), out _, out var error);

        Assert.False(esValido);
        Assert.Contains("Campo 7", error);
    }

    /// <summary>
    /// El Campo 7 se lee de derecha a izquierda: la anteúltima posición es la
    /// jurisdicción 1 y la primera es la jurisdicción 24.
    /// </summary>
    [Theory]
    [InlineData(0, 24)]
    [InlineData(23, 1)]
    public void TryParsear_ConUnEstadoInvalido_IdentificaLaJurisdiccion(int posicion, int jurisdiccionEsperada)
    {
        var campo7 = (new string('5', ParserPadron.LargoCampo7 - 1) + '0').ToCharArray();
        campo7[posicion] = '7';

        var esValido = ParserPadron.TryParsear(ConCampo(7, new string(campo7)), out _, out var error);

        Assert.False(esValido);
        Assert.Contains($"jurisdicción {jurisdiccionEsperada}", error);
    }
}
