using Google.Apis.Auth.OAuth2;

namespace Elcop.TI.Infrastructure.Identity;

/// <summary>
/// Resolve a credencial usada pelos serviços do Google (Firebase Auth e Cloud Storage),
/// para os dois terem exatamente o mesmo comportamento.
/// </summary>
internal static class CredencialGoogle
{
    /// <summary>
    /// Sempre resolve pelas Application Default Credentials — a forma recomendada pelo
    /// Google e a única que não passa por APIs depreciadas.
    ///
    /// No Cloud Run / App Engine as ADC já vêm da identidade do ambiente e não há
    /// arquivo de chave para guardar. Fora do Google Cloud, <c>Elcop:Firebase:CaminhoCredencial</c>
    /// aponta o JSON da conta de serviço e nós o publicamos em
    /// <c>GOOGLE_APPLICATION_CREDENTIALS</c>, que é onde as ADC procuram.
    /// </summary>
    public static GoogleCredential Resolver(string? caminhoCredencial)
    {
        if (!string.IsNullOrWhiteSpace(caminhoCredencial))
        {
            if (!File.Exists(caminhoCredencial))
            {
                throw new FileNotFoundException(
                    $"Elcop:Firebase:CaminhoCredencial aponta para um arquivo inexistente: {caminhoCredencial}",
                    caminhoCredencial);
            }

            Environment.SetEnvironmentVariable(
                "GOOGLE_APPLICATION_CREDENTIALS", Path.GetFullPath(caminhoCredencial));
        }

        return GoogleCredential.GetApplicationDefault();
    }
}
