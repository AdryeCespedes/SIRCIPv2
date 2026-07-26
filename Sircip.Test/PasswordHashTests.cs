namespace Sircip.Test;

/// <summary>RNF-02: las contraseñas se almacenan con hash seguro, nunca en texto plano.</summary>
public class PasswordHashTests
{
    private const string Password = "una-password-cualquiera";

    [Fact]
    public void HashPassword_NoDevuelveLaPasswordEnTextoPlano()
    {
        var hash = BCrypt.Net.BCrypt.HashPassword(Password);

        Assert.NotEqual(Password, hash);
        Assert.DoesNotContain(Password, hash);
    }

    [Fact]
    public void HashPassword_GeneraHashConFormatoBCrypt()
    {
        var hash = BCrypt.Net.BCrypt.HashPassword(Password);

        Assert.StartsWith("$2", hash);
    }

    [Fact]
    public void HashPassword_ConLaMismaPassword_GeneraHashesDistintos()
    {
        var primerHash = BCrypt.Net.BCrypt.HashPassword(Password);
        var segundoHash = BCrypt.Net.BCrypt.HashPassword(Password);

        Assert.NotEqual(primerHash, segundoHash);
    }

    [Fact]
    public void Verify_ConLaPasswordCorrecta_DevuelveTrue()
    {
        var hash = BCrypt.Net.BCrypt.HashPassword(Password);

        Assert.True(BCrypt.Net.BCrypt.Verify(Password, hash));
    }

    [Fact]
    public void Verify_ConOtraPassword_DevuelveFalse()
    {
        var hash = BCrypt.Net.BCrypt.HashPassword(Password);

        Assert.False(BCrypt.Net.BCrypt.Verify("otra-password", hash));
    }
}
