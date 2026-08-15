using Elcop.TI.Application.Common;
using Elcop.TI.Application.Services;
using Elcop.TI.Domain.Enums;
using Elcop.TI.Infrastructure.Identity;
using Elcop.TI.Web.Infra;
using Elcop.TI.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Elcop.TI.Web.Controllers;

/// <summary>Autenticação, perfil do usuário logado e troca de senha.</summary>
[Route("Conta")]
public class ContaController : Controller
{
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly UserManager<ApplicationUser> _users;
    private readonly IAuditoriaService _auditoria;
    private readonly ILogger<ContaController> _logger;
    private readonly ElcopFirebaseOptions _firebaseOpcoes;
    private readonly ElcopAutoCadastroOptions _autoCadastro;
    private readonly IFirebaseAutenticacaoService? _firebase;

    public ContaController(
        SignInManager<ApplicationUser> signIn,
        UserManager<ApplicationUser> users,
        IAuditoriaService auditoria,
        ILogger<ContaController> logger,
        IOptions<ElcopFirebaseOptions> firebaseOpcoes,
        IOptions<ElcopAutoCadastroOptions> autoCadastro,
        // Só é registrado quando Elcop:Firebase:Habilitado = true.
        IFirebaseAutenticacaoService? firebase = null)
    {
        _signIn = signIn;
        _users = users;
        _auditoria = auditoria;
        _logger = logger;
        _firebaseOpcoes = firebaseOpcoes.Value;
        _autoCadastro = autoCadastro.Value;
        _firebase = firebase;
    }

    [HttpGet("Entrar")]
    [AllowAnonymous]
    public IActionResult Entrar(string? urlRetorno = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Painel");

        ViewData["AutoCadastro"] = _autoCadastro.Habilitado;

        return View(new LoginViewModel
        {
            UrlRetorno = urlRetorno,
            Firebase = MontarDadosFirebase()
        });
    }

    // ------------------------------------------------------------------ Autocadastro

    [HttpGet("Registrar")]
    [AllowAnonymous]
    public IActionResult Registrar()
    {
        if (!_autoCadastro.Habilitado) return NotFound();

        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Painel");

        PrepararViewDeRegistro();
        return View(new RegistroViewModel());
    }

    [HttpPost("Registrar")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Registrar(RegistroViewModel model)
    {
        if (!_autoCadastro.Habilitado) return NotFound();

        PrepararViewDeRegistro();

        var email = model.Email.Trim();

        if (!_autoCadastro.DominioAceito(email))
        {
            ModelState.AddModelError(nameof(model.Email),
                $"O cadastro está restrito aos domínios: {string.Join(", ", _autoCadastro.DominiosPermitidos)}.");
        }

        if (!ModelState.IsValid) return View(model);

        // Mensagem idêntica à de sucesso quando o e-mail já existe: revelar a diferença
        // transformaria esta tela em um verificador de quem tem conta no sistema.
        if (await _users.FindByEmailAsync(email) is not null)
        {
            _logger.LogWarning("Tentativa de autocadastro com e-mail já existente: {Email}", email);
            return RedirectToAction(nameof(RegistroConcluido));
        }

        var usuario = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            NomeCompleto = model.NomeCompleto.Trim(),
            Cargo = string.IsNullOrWhiteSpace(model.Cargo) ? null : model.Cargo.Trim(),
            Habilitado = _autoCadastro.AprovacaoAutomatica
        };

        var resultado = await _users.CreateAsync(usuario, model.Senha);

        if (!resultado.Succeeded)
        {
            foreach (var erro in resultado.Errors)
                ModelState.AddModelError(string.Empty, TraduzirErro(erro));

            return View(model);
        }

        var perfil = Perfis.Todos.Contains(_autoCadastro.PerfilPadrao)
            ? _autoCadastro.PerfilPadrao
            : Perfis.Consulta;

        await _users.AddToRoleAsync(usuario, perfil);

        await _auditoria.RegistrarESalvarAsync(
            TipoAcaoAuditoria.Criacao, "Usuario", null,
            $"Autocadastro de {usuario.NomeCompleto} ({email}) com o perfil {perfil}. " +
            $"Situação: {(usuario.Habilitado ? "liberado" : "aguardando aprovação")}.");

        _logger.LogInformation(
            "Autocadastro de {Email} com o perfil {Perfil} (habilitado: {Habilitado}).",
            email, perfil, usuario.Habilitado);

        return RedirectToAction(nameof(RegistroConcluido));
    }

    [HttpGet("RegistroConcluido")]
    [AllowAnonymous]
    public IActionResult RegistroConcluido()
    {
        if (!_autoCadastro.Habilitado) return NotFound();

        ViewData["AprovacaoAutomatica"] = _autoCadastro.AprovacaoAutomatica;
        return View();
    }

    private void PrepararViewDeRegistro()
    {
        ViewData["AprovacaoAutomatica"] = _autoCadastro.AprovacaoAutomatica;
        ViewData["DominiosPermitidos"] = _autoCadastro.DominiosPermitidos;
    }

    [HttpPost("Entrar")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Entrar(LoginViewModel model)
    {
        // A tela precisa dos dados do Firebase e do link de cadastro em qualquer retorno com erro.
        model.Firebase = MontarDadosFirebase();
        ViewData["AutoCadastro"] = _autoCadastro.Habilitado;

        if (!ModelState.IsValid) return View(model);

        var usuario = await _users.FindByEmailAsync(model.Email.Trim());

        if (usuario is null)
        {
            // Mensagem genérica de propósito: não revela se o e-mail existe.
            ModelState.AddModelError(string.Empty, "E-mail ou senha inválidos.");
            return View(model);
        }

        if (!usuario.Habilitado)
        {
            // Aqui a mensagem é específica: quem acabou de se cadastrar (ou foi
            // desativado) precisa saber que o problema não é a senha — do contrário
            // fica tentando redefinir uma senha que está correta. O custo é revelar
            // que existe conta desabilitada para este e-mail.
            ModelState.AddModelError(string.Empty,
                "Este acesso ainda não foi liberado. Procure um administrador do sistema.");
            return View(model);
        }

        var resultado = await _signIn.PasswordSignInAsync(
            usuario, model.Senha, model.Lembrar, lockoutOnFailure: true);

        if (resultado.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty,
                "Conta temporariamente bloqueada por tentativas inválidas. Tente novamente em alguns minutos.");
            return View(model);
        }

        if (!resultado.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "E-mail ou senha inválidos.");
            return View(model);
        }

        await RegistrarAcessoAsync(usuario, "senha");

        if (!string.IsNullOrWhiteSpace(model.UrlRetorno) && Url.IsLocalUrl(model.UrlRetorno))
            return Redirect(model.UrlRetorno);

        return RedirectToAction("Index", "Painel");
    }

    /// <summary>
    /// Recebe o ID token obtido pelo SDK web do Firebase, valida-o no servidor e abre a
    /// sessão local. O Firebase só prova a identidade — perfil, bloqueio e auditoria
    /// continuam vindo do ASP.NET Identity, igual ao login por senha.
    /// </summary>
    [HttpPost("EntrarFirebase")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EntrarFirebase(string idToken, string? urlRetorno = null)
    {
        if (_firebase is null || !_firebaseOpcoes.LoginDisponivel)
            return RedirectToAction(nameof(Entrar));

        var identidade = await _firebase.ValidarTokenAsync(idToken, HttpContext.RequestAborted);

        if (identidade is null)
        {
            this.NotificarErro("Não foi possível validar sua conta Google. Tente novamente.");
            return RedirectToAction(nameof(Entrar), new { urlRetorno });
        }

        var usuario = await _users.FindByEmailAsync(identidade.Email);

        if (usuario is null)
        {
            if (!_firebaseOpcoes.ProvisionarAutomaticamente)
            {
                _logger.LogWarning(
                    "Login via Firebase recusado: {Email} não está cadastrado em Usuários.", identidade.Email);

                this.NotificarErro(
                    "Esta conta não tem acesso ao sistema. Peça ao administrador para cadastrá-la.");
                return RedirectToAction(nameof(Entrar), new { urlRetorno });
            }

            usuario = await ProvisionarUsuarioAsync(identidade);

            if (usuario is null)
            {
                this.NotificarErro("Não foi possível criar seu acesso. Procure o administrador.");
                return RedirectToAction(nameof(Entrar), new { urlRetorno });
            }
        }

        if (!usuario.Habilitado)
        {
            _logger.LogWarning("Login via Firebase recusado: {Email} está desabilitado.", identidade.Email);
            this.NotificarErro("Seu acesso está desabilitado. Procure o administrador.");
            return RedirectToAction(nameof(Entrar), new { urlRetorno });
        }

        await _signIn.SignInAsync(usuario, isPersistent: true);
        await RegistrarAcessoAsync(usuario, "Firebase");

        if (!string.IsNullOrWhiteSpace(urlRetorno) && Url.IsLocalUrl(urlRetorno))
            return Redirect(urlRetorno);

        return RedirectToAction("Index", "Painel");
    }

    /// <summary>Cria o usuário local no primeiro login via Firebase, com o perfil padrão configurado.</summary>
    private async Task<ApplicationUser?> ProvisionarUsuarioAsync(IdentidadeFirebase identidade)
    {
        var novo = new ApplicationUser
        {
            UserName = identidade.Email,
            Email = identidade.Email,
            EmailConfirmed = true,
            NomeCompleto = string.IsNullOrWhiteSpace(identidade.Nome) ? identidade.Email : identidade.Nome
        };

        // Sem senha local: o acesso deste usuário passa exclusivamente pelo Firebase.
        var resultado = await _users.CreateAsync(novo);

        if (!resultado.Succeeded)
        {
            _logger.LogError("Falha ao provisionar {Email} via Firebase: {Erros}",
                identidade.Email, string.Join("; ", resultado.Errors.Select(e => e.Description)));
            return null;
        }

        var perfil = Perfis.Todos.Contains(_firebaseOpcoes.PerfilPadrao)
            ? _firebaseOpcoes.PerfilPadrao
            : Perfis.Consulta;

        await _users.AddToRoleAsync(novo, perfil);

        _logger.LogInformation(
            "Usuário {Email} provisionado via Firebase com o perfil {Perfil}.", identidade.Email, perfil);

        return novo;
    }

    private async Task RegistrarAcessoAsync(ApplicationUser usuario, string meio)
    {
        usuario.UltimoAcesso = DateTime.Now;
        await _users.UpdateAsync(usuario);

        await _auditoria.RegistrarESalvarAsync(
            TipoAcaoAuditoria.Login, "Usuario", null,
            $"Login realizado por {usuario.NomeCompleto} ({meio}).");

        _logger.LogInformation("Login efetuado por {Email} via {Meio}.", usuario.Email, meio);
    }

    /// <summary>Dados públicos do projeto para o SDK web; nulo quando o Firebase não está configurado.</summary>
    private FirebaseLoginViewModel? MontarDadosFirebase() =>
        _firebase is not null && _firebaseOpcoes.LoginDisponivel
            ? new FirebaseLoginViewModel
            {
                ApiKey = _firebaseOpcoes.ApiKeyWeb,
                AuthDomain = string.IsNullOrWhiteSpace(_firebaseOpcoes.AuthDomain)
                    ? $"{_firebaseOpcoes.ProjectId}.firebaseapp.com"
                    : _firebaseOpcoes.AuthDomain,
                ProjectId = _firebaseOpcoes.ProjectId
            }
            : null;

    [HttpPost("Sair")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sair()
    {
        await _auditoria.RegistrarESalvarAsync(
            TipoAcaoAuditoria.Logout, "Usuario", null, $"Logout de {User.Identity?.Name}.");

        await _signIn.SignOutAsync();
        return RedirectToAction(nameof(Entrar));
    }

    [HttpGet("AcessoNegado")]
    [AllowAnonymous]
    public IActionResult AcessoNegado() => View();

    [HttpGet("Perfil")]
    public async Task<IActionResult> Perfil()
    {
        var usuario = await _users.GetUserAsync(User);
        if (usuario is null) return Challenge();

        var perfis = await _users.GetRolesAsync(usuario);

        return View(new PerfilViewModel
        {
            NomeCompleto = usuario.NomeCompleto,
            Cargo = usuario.Cargo,
            Email = usuario.Email ?? string.Empty,
            Perfil = perfis.FirstOrDefault(),
            UltimoAcesso = usuario.UltimoAcesso
        });
    }

    [HttpPost("Perfil")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Perfil(PerfilViewModel model)
    {
        var usuario = await _users.GetUserAsync(User);
        if (usuario is null) return Challenge();

        // A troca de senha tem formulário próprio: aqui esse bloco não é validado.
        foreach (var chave in ModelState.Keys.Where(k => k.StartsWith(nameof(model.AlterarSenha))).ToList())
            ModelState.Remove(chave);

        if (!ModelState.IsValid)
        {
            model.Email = usuario.Email ?? string.Empty;
            return View(model);
        }

        usuario.NomeCompleto = model.NomeCompleto.Trim();
        usuario.Cargo = model.Cargo;
        await _users.UpdateAsync(usuario);

        await _signIn.RefreshSignInAsync(usuario);
        this.NotificarSucesso("Perfil atualizado com sucesso.");

        return RedirectToAction(nameof(Perfil));
    }

    [HttpPost("AlterarSenha")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AlterarSenha(AlterarSenhaViewModel model)
    {
        var usuario = await _users.GetUserAsync(User);
        if (usuario is null) return Challenge();

        if (!ModelState.IsValid)
        {
            this.NotificarErro("Verifique os campos da troca de senha.");
            return RedirectToAction(nameof(Perfil));
        }

        var resultado = await _users.ChangePasswordAsync(usuario, model.SenhaAtual, model.NovaSenha);

        if (!resultado.Succeeded)
        {
            this.NotificarErro(string.Join(" ", resultado.Errors.Select(TraduzirErro)));
            return RedirectToAction(nameof(Perfil));
        }

        await _signIn.RefreshSignInAsync(usuario);
        await _auditoria.RegistrarESalvarAsync(
            TipoAcaoAuditoria.Alteracao, "Usuario", null,
            $"{usuario.NomeCompleto} alterou a própria senha.");

        this.NotificarSucesso("Senha alterada com sucesso.");
        return RedirectToAction(nameof(Perfil));
    }

    /// <summary>Traduz os códigos de erro do Identity para mensagens em português.</summary>
    internal static string TraduzirErro(IdentityError erro) => erro.Code switch
    {
        "PasswordTooShort" => "A senha deve ter no mínimo 8 caracteres.",
        "PasswordRequiresDigit" => "A senha deve conter ao menos um número.",
        "PasswordRequiresUpper" => "A senha deve conter ao menos uma letra maiúscula.",
        "PasswordRequiresLower" => "A senha deve conter ao menos uma letra minúscula.",
        "PasswordRequiresNonAlphanumeric" => "A senha deve conter ao menos um caractere especial.",
        "PasswordMismatch" => "A senha atual informada está incorreta.",
        "DuplicateUserName" or "DuplicateEmail" => "Já existe um usuário com este e-mail.",
        "InvalidEmail" => "E-mail inválido.",
        _ => erro.Description
    };
}
