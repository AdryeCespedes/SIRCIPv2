using Sircip.Shared.Validations;

namespace Sircip.Test;

/// <summary>Dígito verificador del CUIT (módulo 11, ponderadores 5-4-3-2-7-6-5-4-3-2).</summary>
public class ValidadorCuitTests
{
    [Theory]
    [InlineData("30100100106")] // Registro de ejemplo del Anexo A.
    [InlineData("33693450239")]
    [InlineData("20123456786")]
    [InlineData("27123456780")] // Resto 0: le corresponde el dígito 0.
    [InlineData("23200000039")] // Resto 1: le corresponde el dígito 9, no el 10.
    public void EsValido_ConDigitoVerificadorCorrecto_DevuelveTrue(string cuit)
    {
        Assert.True(ValidadorCuit.EsValido(cuit));
    }

    [Theory]
    [InlineData("30100100105")]
    [InlineData("33693450230")]
    [InlineData("20123456789")]
    [InlineData("27123456781")]
    [InlineData("23200000030")]
    public void EsValido_ConDigitoVerificadorIncorrecto_DevuelveFalse(string cuit)
    {
        Assert.False(ValidadorCuit.EsValido(cuit));
    }

    /// <summary>Cambiar un solo dígito del CUIT siempre altera el verificador.</summary>
    [Fact]
    public void EsValido_AlAlterarUnDigitoDeUnCuitValido_DevuelveFalse()
    {
        const string cuitValido = "30100100106";

        for (var posicion = 0; posicion < cuitValido.Length; posicion++)
        {
            var alterado = cuitValido.ToCharArray();
            alterado[posicion] = alterado[posicion] == '9' ? '8' : (char)(alterado[posicion] + 1);

            Assert.False(ValidadorCuit.EsValido(alterado), $"No detectó el cambio en la posición {posicion}.");
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("3010010010")]
    [InlineData("301001001060")]
    [InlineData("3010010010A")]
    [InlineData("A0100100106")]
    public void EsValido_ConUnValorQueNoSonOnceDigitos_DevuelveFalse(string cuit)
    {
        Assert.False(ValidadorCuit.EsValido(cuit));
    }
}
