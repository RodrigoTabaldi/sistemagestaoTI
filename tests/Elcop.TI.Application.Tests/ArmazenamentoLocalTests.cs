using Elcop.TI.Infrastructure.Storage;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elcop.TI.Application.Tests;

public sealed class ArmazenamentoLocalTests : IDisposable
{
    private readonly string _raizWeb;
    private readonly ArmazenamentoLocal _armazenamento;

    public ArmazenamentoLocalTests()
    {
        _raizWeb = Path.Combine(Path.GetTempPath(), "elcop-testes-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_raizWeb);

        var ambiente = new FakeWebHostEnvironment { WebRootPath = _raizWeb };
        _armazenamento = new ArmazenamentoLocal(ambiente, NullLogger<ArmazenamentoLocal>.Instance);
    }

    public void Dispose()
    {
        if (Directory.Exists(_raizWeb)) Directory.Delete(_raizWeb, recursive: true);
    }

    [Theory]
    [InlineData("../../etc", "etc")]
    [InlineData("..\\..\\windows", "windows")]
    [InlineData("ativos", "ativos")]
    [InlineData("Ativos-2026!", "ativos-2026")]
    public void NomeDePastaSeguro_RemoveQualquerCoisaAlemDeLetrasNumerosEHifen(string entrada, string esperado)
    {
        Assert.Equal(esperado, ArmazenamentoLocal.NomeDePastaSeguro(entrada));
    }

    [Fact]
    public void GerarNomeUnico_ComExtensaoForaDaListaPermitida_CaiParaJpg()
    {
        var nome = ArmazenamentoLocal.GerarNomeUnico("malicioso.exe");
        Assert.EndsWith(".jpg", nome);
    }

    [Fact]
    public void GerarNomeUnico_NaoPreservaONomeOriginal()
    {
        // O nome enviado pelo usuário nunca deve sobreviver no disco — só a extensão.
        var nome = ArmazenamentoLocal.GerarNomeUnico("../../../etc/passwd.png");
        Assert.DoesNotContain("passwd", nome);
        Assert.DoesNotContain("..", nome);
        Assert.EndsWith(".png", nome);
    }

    [Fact]
    public async Task EnviarAsync_GravaDentroDePastaUploads()
    {
        await using var conteudo = new MemoryStream([1, 2, 3]);
        var url = await _armazenamento.EnviarAsync(conteudo, "foto.png", "image/png", "ativos", CancellationToken.None);

        Assert.StartsWith("/uploads/ativos/", url);

        var caminhoFisico = Path.Combine(_raizWeb, url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(caminhoFisico));
    }

    [Fact]
    public async Task RemoverAsync_ComUrlTentandoEscaparDaPastaUploads_NaoApagaArquivoFora()
    {
        // Arquivo "sensível" fora de wwwroot/uploads, simulando algo que um path
        // traversal poderia tentar alcançar a partir de uma URL manipulada no banco.
        var arquivoSensivel = Path.Combine(_raizWeb, "segredo.txt");
        await File.WriteAllTextAsync(arquivoSensivel, "não pode ser apagado por uma URL de upload");

        var urlMaliciosa = "/uploads/../segredo.txt";

        await _armazenamento.RemoverAsync(urlMaliciosa, CancellationToken.None);

        Assert.True(File.Exists(arquivoSensivel));
    }

    [Fact]
    public async Task RemoverAsync_ComUrlLegitima_ApagaOArquivo()
    {
        await using var conteudo = new MemoryStream([1, 2, 3]);
        var url = await _armazenamento.EnviarAsync(conteudo, "foto.png", "image/png", "ativos", CancellationToken.None);

        await _armazenamento.RemoverAsync(url, CancellationToken.None);

        var caminhoFisico = Path.Combine(_raizWeb, url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        Assert.False(File.Exists(caminhoFisico));
    }
}
