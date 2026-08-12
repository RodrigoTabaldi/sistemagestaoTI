using Elcop.TI.Domain.Common;
using Elcop.TI.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Elcop.TI.Web.Infra;

/// <summary>
/// Utilitários de apresentação compartilhados pelas views: listas de seleção a partir
/// de enums e tradução de estados em classes CSS/ícones.
/// </summary>
public static class ViewHelpers
{
    /// <summary>Lista de seleção com os valores de um enum, usando os nomes amigáveis.</summary>
    public static IEnumerable<SelectListItem> ListaDeEnum<TEnum>(TEnum? selecionado = null, string? textoVazio = null)
        where TEnum : struct, Enum
    {
        if (textoVazio is not null)
            yield return new SelectListItem(textoVazio, string.Empty, selecionado is null);

        foreach (var valor in Enum.GetValues<TEnum>())
        {
            yield return new SelectListItem(
                ((Enum)(object)valor).ObterNome(),
                valor.ToString(),
                selecionado.HasValue && selecionado.Value.Equals(valor));
        }
    }

    /// <summary>Sufixo da classe CSS que colore o selo de status do ativo.</summary>
    public static string ClasseStatusAtivo(StatusAtivo status) => status switch
    {
        StatusAtivo.Disponivel => "sucesso",
        StatusAtivo.EmUso => "primario",
        StatusAtivo.Reservado => "aviso",
        StatusAtivo.EmManutencao => "atencao",
        StatusAtivo.Emprestado => "info",
        StatusAtivo.Extraviado => "roxo",
        StatusAtivo.Danificado => "perigo",
        _ => "neutro"
    };

    public static string ClasseStatusDemanda(StatusDemanda status) => status switch
    {
        StatusDemanda.Aberta => "info",
        StatusDemanda.EmAndamento => "primario",
        StatusDemanda.AguardandoTerceiros => "aviso",
        StatusDemanda.Pausada => "neutro",
        StatusDemanda.Concluida => "sucesso",
        _ => "neutro"
    };

    public static string ClassePrioridade(PrioridadeDemanda prioridade) => prioridade switch
    {
        PrioridadeDemanda.Critica => "perigo",
        PrioridadeDemanda.Alta => "atencao",
        PrioridadeDemanda.Media => "info",
        _ => "sucesso"
    };

    public static string ClasseStatusMovimentacao(StatusMovimentacao status) => status switch
    {
        StatusMovimentacao.EmAberto => "primario",
        StatusMovimentacao.Devolvido => "sucesso",
        StatusMovimentacao.Atrasado => "perigo",
        _ => "neutro"
    };

    public static string ClasseCondicao(CondicaoAtivo condicao) => condicao switch
    {
        CondicaoAtivo.Novo or CondicaoAtivo.Otimo => "sucesso",
        CondicaoAtivo.Bom => "info",
        CondicaoAtivo.Regular => "aviso",
        CondicaoAtivo.Ruim => "atencao",
        _ => "perigo"
    };

    public static string ClasseStatusColaborador(StatusColaborador status) => status switch
    {
        StatusColaborador.Ativo => "sucesso",
        StatusColaborador.Ferias => "info",
        StatusColaborador.Afastado => "aviso",
        _ => "neutro"
    };

    /// <summary>Identificador do ícone SVG (ver _Icones.cshtml) que representa o tipo de ativo.</summary>
    public static string IconeTipoAtivo(TipoAtivo tipo) => tipo switch
    {
        TipoAtivo.Notebook => "notebook",
        TipoAtivo.Desktop => "desktop",
        TipoAtivo.Monitor => "monitor",
        TipoAtivo.Celular => "celular",
        TipoAtivo.Tablet => "tablet",
        TipoAtivo.Bodycam => "bodycam",
        TipoAtivo.Impressora or TipoAtivo.Scanner => "impressora",
        TipoAtivo.Servidor => "servidor",
        TipoAtivo.Switch or TipoAtivo.Roteador or TipoAtivo.AccessPoint => "rede",
        TipoAtivo.Nobreak => "energia",
        TipoAtivo.Headset => "headset",
        TipoAtivo.CameraSeguranca => "camera",
        TipoAtivo.RadioComunicador => "radio",
        TipoAtivo.ChipLinhaMovel => "chip",
        TipoAtivo.LicencaSoftware => "licenca",
        _ => "caixa"
    };

    /// <summary>Cor de fundo determinística para avatares sem foto.</summary>
    public static string CorAvatar(string? semente)
    {
        var cores = new[] { "#8B1F26", "#3F6FB5", "#2E9E6B", "#B98A2E", "#9C2B7A", "#C4741C", "#4A5568" };
        if (string.IsNullOrWhiteSpace(semente)) return cores[0];

        var soma = semente.Sum(c => c);
        return cores[soma % cores.Length];
    }

    /// <summary>Texto relativo curto ("há 3 dias") usado nas linhas do tempo.</summary>
    public static string TempoRelativo(DateTime data)
    {
        var diferenca = DateTime.Now - data;

        if (diferenca.TotalSeconds < 0) return data.ToString("dd/MM/yyyy HH:mm");
        if (diferenca.TotalMinutes < 1) return "agora há pouco";
        if (diferenca.TotalMinutes < 60) return $"há {(int)diferenca.TotalMinutes} min";
        if (diferenca.TotalHours < 24) return $"há {(int)diferenca.TotalHours}h";
        if (diferenca.TotalDays < 30) return $"há {(int)diferenca.TotalDays} dia(s)";
        if (diferenca.TotalDays < 365) return $"há {(int)(diferenca.TotalDays / 30)} mês(es)";

        return $"há {(int)(diferenca.TotalDays / 365)} ano(s)";
    }
}
