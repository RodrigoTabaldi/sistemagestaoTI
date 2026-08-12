using System.ComponentModel.DataAnnotations;
using System.Collections.Concurrent;
using System.Reflection;

namespace Elcop.TI.Domain.Common;

/// <summary>
/// Leitura dos atributos <see cref="DisplayAttribute"/> dos enums do domínio,
/// com cache para evitar reflexão repetida nas listagens.
/// </summary>
public static class EnumExtensions
{
    private static readonly ConcurrentDictionary<Enum, string> NomesCache = new();
    private static readonly ConcurrentDictionary<Enum, string> GruposCache = new();

    /// <summary>Nome amigável do valor (fallback: o próprio identificador).</summary>
    public static string ObterNome(this Enum valor) =>
        NomesCache.GetOrAdd(valor, v => ObterAtributo(v)?.Name ?? v.ToString());

    /// <summary>Grupo declarado no <see cref="DisplayAttribute.GroupName"/>.</summary>
    public static string ObterGrupo(this Enum valor) =>
        GruposCache.GetOrAdd(valor, v => ObterAtributo(v)?.GroupName ?? "Geral");

    /// <summary>Todos os valores de <typeparamref name="TEnum"/> em ordem de declaração.</summary>
    public static IReadOnlyList<TEnum> Valores<TEnum>() where TEnum : struct, Enum =>
        Enum.GetValues<TEnum>();

    private static DisplayAttribute? ObterAtributo(Enum valor) =>
        valor.GetType()
             .GetField(valor.ToString())
             ?.GetCustomAttribute<DisplayAttribute>();
}
