using Elcop.TI.Application.Common;
using Elcop.TI.Infrastructure.Identity;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elcop.TI.Infrastructure.Storage;

/// <summary>
/// Grava no bucket do Firebase Cloud Storage. Os objetos ficam públicos para leitura
/// (é o mesmo que a pasta <c>wwwroot/uploads</c> já era) e são endereçados pela URL
/// <c>storage.googleapis.com/&lt;bucket&gt;/&lt;objeto&gt;</c>.
///
/// Se o bucket for privado, troque <see cref="EnviarAsync"/> para devolver uma URL
/// assinada — mas aí a URL passa a expirar e não pode ser guardada no banco.
/// </summary>
public sealed class ArmazenamentoFirebase : IArmazenamentoArquivos
{
    private const string BaseUrl = "https://storage.googleapis.com";

    private readonly ElcopFirebaseOptions _opcoes;
    private readonly ILogger<ArmazenamentoFirebase> _logger;
    private readonly Lazy<StorageClient> _cliente;

    public ArmazenamentoFirebase(
        IOptions<ElcopFirebaseOptions> opcoes, ILogger<ArmazenamentoFirebase> logger)
    {
        _opcoes = opcoes.Value;
        _logger = logger;

        _cliente = new Lazy<StorageClient>(
            () => StorageClient.Create(CredencialGoogle.Resolver(_opcoes.CaminhoCredencial)));
    }

    public async Task<string> EnviarAsync(
        Stream conteudo, string nomeArquivo, string contentType, string pasta, CancellationToken ct = default)
    {
        var bucket = ExigirBucket();
        var objeto = $"{ArmazenamentoLocal.NomeDePastaSeguro(pasta)}/{ArmazenamentoLocal.GerarNomeUnico(nomeArquivo)}";

        await _cliente.Value.UploadObjectAsync(
            bucket, objeto, contentType, conteudo,
            new UploadObjectOptions { PredefinedAcl = PredefinedObjectAcl.PublicRead },
            cancellationToken: ct);

        return $"{BaseUrl}/{bucket}/{objeto}";
    }

    public async Task RemoverAsync(string url, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(url)) return;

        var bucket = ExigirBucket();
        var prefixo = $"{BaseUrl}/{bucket}/";

        // URL de outro destino (ex.: registro antigo, gravado ainda no disco local).
        if (!url.StartsWith(prefixo, StringComparison.Ordinal)) return;

        var objeto = url[prefixo.Length..];

        try
        {
            await _cliente.Value.DeleteObjectAsync(bucket, objeto, cancellationToken: ct);
        }
        catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // Já não existe: o efeito desejado é o mesmo.
        }
        catch (Google.GoogleApiException ex)
        {
            _logger.LogWarning(ex, "Não foi possível remover {Objeto} do bucket {Bucket}.", objeto, bucket);
        }
    }

    private string ExigirBucket() =>
        string.IsNullOrWhiteSpace(_opcoes.Bucket)
            ? throw new InvalidOperationException(
                "Elcop:Armazenamento está como \"Firebase\", mas Elcop:Firebase:Bucket não foi configurado.")
            : _opcoes.Bucket;
}
