using System.Net;
using Elcop.TI.Infrastructure.Identity;

namespace Elcop.TI.Web.Tests;

/// <summary>
/// Sobe a aplicação real (Program.cs, DI, filtros, [Authorize]) e prova, por fora,
/// que as políticas de <see cref="Elcop.TI.Web.Infra.Politicas"/> realmente bloqueiam
/// quem não tem o perfil — algo que só a leitura de código garantia até aqui.
/// </summary>
public sealed class AutorizacaoTests : IAsyncLifetime
{
    private ElcopWebApplicationFactory _factory = null!;

    public Task InitializeAsync()
    {
        _factory = new ElcopWebApplicationFactory();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task Anonimo_AcessandoRotaProtegida_NaoEhAutenticado()
    {
        var client = _factory.CriarClienteAnonimo();

        var resposta = await client.GetAsync("/Ativos");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Anonimo_VerificandoHealthCheck_RecebeSucesso()
    {
        var client = _factory.CriarClienteAnonimo();

        var resposta = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    public async Task PerfilConsulta_ListandoAtivos_TemLeituraLiberada()
    {
        var client = _factory.CriarClienteComo(Perfis.Consulta);

        var resposta = await client.GetAsync("/Ativos");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    public async Task PerfilConsulta_AbrindoTelaDeUsuarios_RecebeProibido()
    {
        // Usuarios exige a política Administrar — Consulta não tem.
        var client = _factory.CriarClienteComo(Perfis.Consulta);

        var resposta = await client.GetAsync("/Usuarios");

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task PerfilAdministrador_AbrindoTelaDeUsuarios_EhPermitido()
    {
        var client = _factory.CriarClienteComo(Perfis.Administrador);

        var resposta = await client.GetAsync("/Usuarios");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    public async Task PerfilConsulta_EditandoAtivo_RecebeProibido()
    {
        // Editar exige a política Operar (Administrador ou Tecnico) — Consulta não tem,
        // mesmo que o ativo não exista: a autorização é checada antes da ação rodar.
        var client = _factory.CriarClienteComo(Perfis.Consulta);

        var resposta = await client.GetAsync("/Ativos/Editar/1");

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task PerfilTecnico_EditandoAtivoInexistente_PassaPelaAutorizacaoERecebeNaoEncontrado()
    {
        // Tecnico tem a política Operar: a autorização deixa passar e a ação roda de
        // verdade — como a base de teste está vazia, o resultado é 404, não 403/401.
        var client = _factory.CriarClienteComo(Perfis.Tecnico);

        var resposta = await client.GetAsync("/Ativos/Editar/1");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task PerfilConsulta_AbrindoFormularioDeCadastroDeAtivo_EhPermitido()
    {
        // Cadastrar (criar ativo/colaborador) é liberado a qualquer perfil autenticado.
        var client = _factory.CriarClienteComo(Perfis.Consulta);

        var resposta = await client.GetAsync("/Ativos/Criar");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    public async Task PerfilConsulta_AbrindoPainelInicial_CarregaComSucesso()
    {
        // Rota padrão da aplicação (Painel/Index) — exercita o PainelService de ponta a
        // ponta, cache incluso, contra uma base vazia.
        var client = _factory.CriarClienteComo(Perfis.Consulta);

        var resposta = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    public async Task PerfilTecnico_AbrindoAuditoria_RecebeProibido()
    {
        // Auditoria é exclusiva de Administrador — Tecnico não entra, mesmo operando o resto.
        var client = _factory.CriarClienteComo(Perfis.Tecnico);

        var resposta = await client.GetAsync("/Auditoria");

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }
}
