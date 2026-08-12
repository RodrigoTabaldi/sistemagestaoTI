using Elcop.TI.Application.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace Elcop.TI.Infrastructure.Storage;

/// <summary>
/// Destino padrão: grava em <c>wwwroot/uploads/&lt;pasta&gt;</c> e devolve o caminho
/// relativo servido pelo próprio site. Suficiente para instalação em servidor único.
/// </summary>
public sealed class ArmazenamentoLocal : IArmazenamentoArquivos
{
    private const string PastaRaiz = "uploads";

    private readonly IWebHostEnvironment _ambiente;
    private readonly ILogger<ArmazenamentoLocal> _logger;

    public ArmazenamentoLocal(IWebHostEnvironment ambiente, ILogger<ArmazenamentoLocal> logger)
    {
        _ambiente = ambiente;
        _logger = logger;
    }

    public async Task<string> EnviarAsync(
        Stream conteudo, string nomeArquivo, string contentType, string pasta, CancellationToken ct = default)
    {
        var subpasta = NomeDePastaSeguro(pasta);
        var nomeFinal = GerarNomeUnico(nomeArquivo);

        var diretorio = Path.Combine(RaizWeb(), PastaRaiz, subpasta);
        Directory.CreateDirectory(diretorio);

        var caminho = Path.Combine(diretorio, nomeFinal);

        await using (var destino = File.Create(caminho))
        {
            await conteudo.CopyToAsync(destino, ct);
        }

        return $"/{PastaRaiz}/{subpasta}/{nomeFinal}";
    }

    public Task RemoverAsync(string url, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(url)) return Task.CompletedTask;

        var caminho = ResolverCaminhoFisico(url);

        if (caminho is not null && File.Exists(caminho))
        {
            try
            {
                File.Delete(caminho);
            }
            catch (IOException ex)
            {
                // Falhar ao apagar um arquivo antigo não pode derrubar o cadastro.
                _logger.LogWarning(ex, "Não foi possível remover o arquivo {Url}.", url);
            }
        }

        return Task.CompletedTask;
    }

    private string RaizWeb() =>
        string.IsNullOrWhiteSpace(_ambiente.WebRootPath)
            ? Path.Combine(_ambiente.ContentRootPath, "wwwroot")
            : _ambiente.WebRootPath;

    /// <summary>
    /// Traduz a URL pública para o caminho no disco, recusando qualquer coisa que
    /// escape de <c>wwwroot/uploads</c> — a URL vem do banco, não é dado confiável.
    /// </summary>
    private string? ResolverCaminhoFisico(string url)
    {
        if (!url.StartsWith($"/{PastaRaiz}/", StringComparison.Ordinal)) return null;

        var raizUploads = Path.GetFullPath(Path.Combine(RaizWeb(), PastaRaiz));
        var relativo = url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var completo = Path.GetFullPath(Path.Combine(RaizWeb(), relativo));

        return completo.StartsWith(raizUploads + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            ? completo
            : null;
    }

    /// <summary>Nome novo e imprevisível: preserva só a extensão do arquivo enviado.</summary>
    internal static string GerarNomeUnico(string nomeArquivo)
    {
        var extensao = Path.GetExtension(nomeArquivo).ToLowerInvariant();

        if (!RegrasDeUpload.ExtensoesPermitidas.Contains(extensao))
            extensao = ".jpg";

        return $"{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}{extensao}";
    }

    /// <summary>Só letras, números e hífen — a pasta vem do código, mas não custa garantir.</summary>
    internal static string NomeDePastaSeguro(string pasta)
    {
        var limpo = new string(pasta.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());
        return string.IsNullOrEmpty(limpo) ? "diversos" : limpo.ToLowerInvariant();
    }
}
