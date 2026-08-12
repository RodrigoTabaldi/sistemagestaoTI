using Microsoft.AspNetCore.Mvc;

namespace Elcop.TI.Web.Infra;

/// <summary>
/// Mensagens efêmeras (toasts) trafegadas via TempData entre uma ação e o redirect seguinte.
/// </summary>
public static class Notificacao
{
    public const string ChaveSucesso = "toast.sucesso";
    public const string ChaveErro = "toast.erro";
    public const string ChaveAviso = "toast.aviso";
    public const string ChaveInfo = "toast.info";

    public static void NotificarSucesso(this Controller controller, string mensagem) =>
        controller.TempData[ChaveSucesso] = mensagem;

    public static void NotificarErro(this Controller controller, string mensagem) =>
        controller.TempData[ChaveErro] = mensagem;

    public static void NotificarAviso(this Controller controller, string mensagem) =>
        controller.TempData[ChaveAviso] = mensagem;

    public static void NotificarInfo(this Controller controller, string mensagem) =>
        controller.TempData[ChaveInfo] = mensagem;
}
