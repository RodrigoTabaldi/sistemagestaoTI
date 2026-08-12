using Elcop.TI.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace Elcop.TI.Web.Infra;

/// <summary>
/// Recebe a foto enviada no formulário de ativo ou de colaborador. A validação acontece
/// antes de qualquer escrita, para um formulário rejeitado não deixar arquivo órfão no
/// disco nem no bucket.
/// </summary>
public static class UploadDeImagem
{
    /// <summary>Nome do campo no formulário — usado também na chave do ModelState.</summary>
    public const string Campo = "Foto";

    /// <summary>
    /// Valida extensão e tamanho, registrando o erro no ModelState quando o arquivo é
    /// inaceitável. Devolve <c>true</c> quando não há nada que impeça o envio.
    /// </summary>
    public static bool ValidarFoto(this Controller controller, IFormFile? arquivo)
    {
        if (arquivo is null || arquivo.Length == 0) return true;

        var erro = RegrasDeUpload.Validar(arquivo.FileName, arquivo.Length);
        if (erro is null) return true;

        controller.ModelState.AddModelError(Campo, erro);
        return false;
    }

    /// <summary>
    /// Grava o arquivo e devolve a nova URL, ou <c>null</c> quando nada foi enviado
    /// (nesse caso a URL já existente no registro deve ser preservada).
    /// Chame apenas depois de <see cref="ValidarFoto"/> e do <c>ModelState.IsValid</c>.
    /// </summary>
    public static async Task<string?> EnviarFotoAsync(
        IArmazenamentoArquivos armazenamento, IFormFile? arquivo, string pasta, CancellationToken ct)
    {
        if (arquivo is null || arquivo.Length == 0) return null;

        await using var conteudo = arquivo.OpenReadStream();

        return await armazenamento.EnviarAsync(
            conteudo, arquivo.FileName, arquivo.ContentType, pasta, ct);
    }

    /// <summary>
    /// Remove a imagem anterior depois que a substituta já foi gravada com sucesso.
    /// Falha aqui é registrada pelo próprio armazenamento e nunca derruba o cadastro.
    /// </summary>
    public static async Task RemoverAnteriorAsync(
        IArmazenamentoArquivos armazenamento, string? urlAnterior, string? urlNova, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(urlAnterior)) return;
        if (string.IsNullOrWhiteSpace(urlNova)) return;
        if (urlAnterior == urlNova) return;

        await armazenamento.RemoverAsync(urlAnterior, ct);
    }
}
