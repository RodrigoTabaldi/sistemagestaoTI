using Elcop.TI.Application.Services;
using Elcop.TI.Domain.Enums;
using Elcop.TI.Infrastructure.Identity;
using Elcop.TI.Web.Infra;
using Elcop.TI.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Elcop.TI.Web.Controllers;

/// <summary>Gestão dos usuários que operam o sistema e de seus perfis de acesso.</summary>
[Authorize(Policy = Politicas.Administrar)]
public class UsuariosController : Controller
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly IAuditoriaService _auditoria;

    public UsuariosController(UserManager<ApplicationUser> users, IAuditoriaService auditoria)
    {
        _users = users;
        _auditoria = auditoria;
    }

    public async Task<IActionResult> Index(string? busca, CancellationToken ct)
    {
        var consulta = _users.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim();
            consulta = consulta.Where(u =>
                EF.Functions.Like(u.NomeCompleto, $"%{termo}%") ||
                EF.Functions.Like(u.Email!, $"%{termo}%"));
        }

        var usuarios = await consulta.OrderBy(u => u.NomeCompleto).ToListAsync(ct);

        var modelo = new List<UsuarioViewModel>(usuarios.Count);
        foreach (var usuario in usuarios)
        {
            modelo.Add(new UsuarioViewModel
            {
                Id = usuario.Id,
                NomeCompleto = usuario.NomeCompleto,
                Email = usuario.Email ?? string.Empty,
                Cargo = usuario.Cargo,
                Habilitado = usuario.Habilitado,
                UltimoAcesso = usuario.UltimoAcesso,
                Perfil = (await _users.GetRolesAsync(usuario)).FirstOrDefault() ?? Perfis.Consulta
            });
        }

        ViewData["Busca"] = busca;
        return View(modelo);
    }

    public IActionResult Criar() => View("Formulario", new UsuarioViewModel { Perfil = Perfis.Tecnico });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Criar(UsuarioViewModel model, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(model.Senha))
            ModelState.AddModelError(nameof(model.Senha), "Informe a senha inicial do usuário.");

        if (!ModelState.IsValid) return View("Formulario", model);

        var usuario = new ApplicationUser
        {
            UserName = model.Email.Trim(),
            Email = model.Email.Trim(),
            EmailConfirmed = true,
            NomeCompleto = model.NomeCompleto.Trim(),
            Cargo = model.Cargo,
            Habilitado = model.Habilitado
        };

        var resultado = await _users.CreateAsync(usuario, model.Senha!);
        if (!resultado.Succeeded)
        {
            AdicionarErros(resultado);
            return View("Formulario", model);
        }

        await _users.AddToRoleAsync(usuario, model.Perfil);

        await _auditoria.RegistrarESalvarAsync(
            TipoAcaoAuditoria.Criacao, "Usuario", null,
            $"Usuário {usuario.NomeCompleto} criado com o perfil {model.Perfil}.", ct);

        this.NotificarSucesso($"Usuário {usuario.NomeCompleto} criado.");
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Editar(string id)
    {
        var usuario = await _users.FindByIdAsync(id);
        if (usuario is null) return NotFound();

        return View("Formulario", new UsuarioViewModel
        {
            Id = usuario.Id,
            NomeCompleto = usuario.NomeCompleto,
            Email = usuario.Email ?? string.Empty,
            Cargo = usuario.Cargo,
            Habilitado = usuario.Habilitado,
            UltimoAcesso = usuario.UltimoAcesso,
            Perfil = (await _users.GetRolesAsync(usuario)).FirstOrDefault() ?? Perfis.Consulta
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(UsuarioViewModel model, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(model.Id)) return BadRequest();
        if (!ModelState.IsValid) return View("Formulario", model);

        var usuario = await _users.FindByIdAsync(model.Id);
        if (usuario is null) return NotFound();

        var ehProprioUsuario = usuario.UserName == User.Identity?.Name;
        if (ehProprioUsuario && (!model.Habilitado || model.Perfil != Perfis.Administrador))
        {
            this.NotificarErro("Você não pode remover o próprio acesso de administrador.");
            return RedirectToAction(nameof(Index));
        }

        usuario.NomeCompleto = model.NomeCompleto.Trim();
        usuario.Cargo = model.Cargo;
        usuario.Habilitado = model.Habilitado;
        usuario.Email = model.Email.Trim();
        usuario.UserName = model.Email.Trim();

        var atualizacao = await _users.UpdateAsync(usuario);
        if (!atualizacao.Succeeded)
        {
            AdicionarErros(atualizacao);
            return View("Formulario", model);
        }

        var perfisAtuais = await _users.GetRolesAsync(usuario);
        if (!perfisAtuais.Contains(model.Perfil))
        {
            await _users.RemoveFromRolesAsync(usuario, perfisAtuais);
            await _users.AddToRoleAsync(usuario, model.Perfil);
        }

        if (!string.IsNullOrWhiteSpace(model.Senha))
        {
            var token = await _users.GeneratePasswordResetTokenAsync(usuario);
            var redefinicao = await _users.ResetPasswordAsync(usuario, token, model.Senha);

            if (!redefinicao.Succeeded)
            {
                AdicionarErros(redefinicao);
                return View("Formulario", model);
            }
        }

        await _auditoria.RegistrarESalvarAsync(
            TipoAcaoAuditoria.Alteracao, "Usuario", null,
            $"Usuário {usuario.NomeCompleto} atualizado (perfil {model.Perfil}).", ct);

        this.NotificarSucesso("Usuário atualizado.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Excluir(string id, CancellationToken ct)
    {
        var usuario = await _users.FindByIdAsync(id);
        if (usuario is null) return NotFound();

        if (usuario.UserName == User.Identity?.Name)
        {
            this.NotificarErro("Não é possível excluir o usuário com o qual você está conectado.");
            return RedirectToAction(nameof(Index));
        }

        // Desativa em vez de excluir: os registros de auditoria referenciam o nome do usuário.
        usuario.Habilitado = false;
        await _users.UpdateAsync(usuario);

        await _auditoria.RegistrarESalvarAsync(
            TipoAcaoAuditoria.Exclusao, "Usuario", null,
            $"Usuário {usuario.NomeCompleto} desativado.", ct);

        this.NotificarSucesso($"Usuário {usuario.NomeCompleto} desativado.");
        return RedirectToAction(nameof(Index));
    }

    private void AdicionarErros(IdentityResult resultado)
    {
        foreach (var erro in resultado.Errors)
            ModelState.AddModelError(string.Empty, ContaController.TraduzirErro(erro));
    }
}
