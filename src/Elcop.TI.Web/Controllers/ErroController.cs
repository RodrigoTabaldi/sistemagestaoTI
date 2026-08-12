using System.Diagnostics;
using Elcop.TI.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Elcop.TI.Web.Controllers;

/// <summary>Páginas de erro amigáveis para status HTTP e exceções não tratadas.</summary>
[AllowAnonymous]
[Route("Erro")]
public class ErroController : Controller
{
    [HttpGet("")]
    [HttpGet("{codigo:int}")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Index(int codigo = 500)
    {
        var (titulo, mensagem) = codigo switch
        {
            400 => ("Requisição inválida", "Os dados enviados não puderam ser interpretados."),
            401 => ("Sessão expirada", "Faça login novamente para continuar."),
            403 => ("Acesso negado", "Seu perfil não permite acessar esta área do sistema."),
            404 => ("Página não encontrada", "O endereço acessado não existe ou foi movido."),
            405 => ("Operação não permitida", "Esta ação não está disponível para este endereço."),
            _ => ("Algo deu errado", "Ocorreu um erro inesperado. A equipe de TI foi notificada pelos logs.")
        };

        Response.StatusCode = codigo;

        return View(new ErroViewModel
        {
            CodigoStatus = codigo,
            Titulo = titulo,
            Mensagem = mensagem,
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }
}
