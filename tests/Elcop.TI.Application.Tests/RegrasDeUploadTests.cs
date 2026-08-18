using Elcop.TI.Application.Common;

namespace Elcop.TI.Application.Tests;

public sealed class RegrasDeUploadTests
{
    [Theory]
    [InlineData("foto.jpg")]
    [InlineData("foto.JPEG")]
    [InlineData("foto.png")]
    [InlineData("foto.webp")]
    public void Validar_ComExtensaoPermitida_NaoRetornaErro(string nomeArquivo)
    {
        var erro = RegrasDeUpload.Validar(nomeArquivo, 1024);
        Assert.Null(erro);
    }

    [Theory]
    [InlineData("script.exe")]
    [InlineData("pagina.html")]
    [InlineData("imagem.svg")]
    [InlineData("arquivo.php")]
    [InlineData("sem-extensao")]
    public void Validar_ComExtensaoNaoPermitida_RetornaErro(string nomeArquivo)
    {
        var erro = RegrasDeUpload.Validar(nomeArquivo, 1024);
        Assert.NotNull(erro);
    }

    [Fact]
    public void Validar_AcimaDoTamanhoMaximo_RetornaErro()
    {
        var erro = RegrasDeUpload.Validar("foto.jpg", RegrasDeUpload.TamanhoMaximoBytes + 1);
        Assert.NotNull(erro);
    }

    [Fact]
    public void Validar_NoLimiteDoTamanhoMaximo_NaoRetornaErro()
    {
        var erro = RegrasDeUpload.Validar("foto.jpg", RegrasDeUpload.TamanhoMaximoBytes);
        Assert.Null(erro);
    }

    [Fact]
    public void AssinaturaCorresponde_JpegValido_RetornaTrue()
    {
        byte[] cabecalho = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10];
        Assert.True(RegrasDeUpload.AssinaturaCorresponde(cabecalho, ".jpg"));
    }

    [Fact]
    public void AssinaturaCorresponde_PngValido_RetornaTrue()
    {
        byte[] cabecalho = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        Assert.True(RegrasDeUpload.AssinaturaCorresponde(cabecalho, ".png"));
    }

    [Fact]
    public void AssinaturaCorresponde_WebpValido_RetornaTrue()
    {
        byte[] cabecalho = "RIFF????WEBP"u8.ToArray();
        Assert.True(RegrasDeUpload.AssinaturaCorresponde(cabecalho, ".webp"));
    }

    [Fact]
    public void AssinaturaCorresponde_ExecutavelDisfarcadoDePng_RetornaFalse()
    {
        // Cabeçalho de um executável Windows (MZ) com extensão .png — o ataque que a
        // checagem existe para barrar: extensão mentindo sobre o conteúdo real.
        byte[] cabecalho = [0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00];
        Assert.False(RegrasDeUpload.AssinaturaCorresponde(cabecalho, ".png"));
    }

    [Fact]
    public void AssinaturaCorresponde_ConteudoVazio_RetornaFalse()
    {
        Assert.False(RegrasDeUpload.AssinaturaCorresponde(ReadOnlySpan<byte>.Empty, ".jpg"));
    }
}
