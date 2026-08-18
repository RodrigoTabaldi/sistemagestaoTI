namespace Elcop.TI.Application.Common;

/// <summary>
/// Guarda arquivos enviados pelo usuário (fotos de ativos e de colaboradores) e devolve
/// a URL que fica gravada no banco. Existem duas implementações — disco local e
/// Firebase Cloud Storage — escolhidas por <c>Elcop:Armazenamento</c>, para que trocar
/// de destino não exija tocar em controller nenhum.
/// </summary>
public interface IArmazenamentoArquivos
{
    /// <summary>
    /// Grava o arquivo e devolve a URL a ser persistida em <c>FotoUrl</c>.
    /// </summary>
    /// <param name="conteudo">Fluxo do arquivo, posicionado no início.</param>
    /// <param name="nomeArquivo">Nome original — usado só para extrair a extensão.</param>
    /// <param name="contentType">MIME informado pelo navegador.</param>
    /// <param name="pasta">Agrupador lógico, ex.: <c>ativos</c> ou <c>colaboradores</c>.</param>
    Task<string> EnviarAsync(
        Stream conteudo,
        string nomeArquivo,
        string contentType,
        string pasta,
        CancellationToken ct = default);

    /// <summary>
    /// Remove um arquivo gravado anteriormente. Recebe a mesma URL devolvida por
    /// <see cref="EnviarAsync"/>. Não lança se o arquivo já não existir.
    /// </summary>
    Task RemoverAsync(string url, CancellationToken ct = default);
}

/// <summary>Regras de validação de upload compartilhadas por todos os destinos.</summary>
public static class RegrasDeUpload
{
    public const long TamanhoMaximoBytes = 4 * 1024 * 1024;

    public static readonly string[] ExtensoesPermitidas = [".jpg", ".jpeg", ".png", ".webp"];

    /// <summary>
    /// Valida extensão e tamanho. Devolve <c>null</c> quando o arquivo é aceitável
    /// ou a mensagem de erro pronta para exibir ao usuário.
    /// </summary>
    public static string? Validar(string nomeArquivo, long tamanhoBytes)
    {
        var extensao = Path.GetExtension(nomeArquivo).ToLowerInvariant();

        if (!ExtensoesPermitidas.Contains(extensao))
            return $"Formato não aceito. Envie uma imagem {string.Join(", ", ExtensoesPermitidas)}.";

        if (tamanhoBytes > TamanhoMaximoBytes)
            return $"A imagem deve ter no máximo {TamanhoMaximoBytes / 1024 / 1024} MB.";

        if (tamanhoBytes == 0)
            return "O arquivo enviado está vazio.";

        return null;
    }

    /// <summary>
    /// Confere a assinatura binária (magic bytes) do conteúdo contra a extensão declarada —
    /// a extensão do nome do arquivo é só o que o navegador informou, nunca dado confiável.
    /// </summary>
    public static bool AssinaturaCorresponde(ReadOnlySpan<byte> cabecalho, string extensao) =>
        extensao.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" =>
                cabecalho.Length >= 3
                && cabecalho[0] == 0xFF && cabecalho[1] == 0xD8 && cabecalho[2] == 0xFF,

            ".png" =>
                cabecalho.Length >= 8
                && cabecalho[0] == 0x89 && cabecalho[1] == 0x50 && cabecalho[2] == 0x4E && cabecalho[3] == 0x47
                && cabecalho[4] == 0x0D && cabecalho[5] == 0x0A && cabecalho[6] == 0x1A && cabecalho[7] == 0x0A,

            ".webp" =>
                cabecalho.Length >= 12
                && cabecalho[0] == (byte)'R' && cabecalho[1] == (byte)'I'
                && cabecalho[2] == (byte)'F' && cabecalho[3] == (byte)'F'
                && cabecalho[8] == (byte)'W' && cabecalho[9] == (byte)'E'
                && cabecalho[10] == (byte)'B' && cabecalho[11] == (byte)'P',

            _ => false
        };
}
