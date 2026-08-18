using Elcop.TI.Application.Common;

namespace Elcop.TI.Application.Tests;

public sealed class FakeUsuarioAtual : IUsuarioAtual
{
    public string? NomeUsuario => "teste@elcop.com.br";

    public string? NomeExibicao => "Usuário de Teste";

    public string? EnderecoIp => "127.0.0.1";

    public bool EstaAutenticado => true;

    public bool PossuiPerfil(string perfil) => true;
}
