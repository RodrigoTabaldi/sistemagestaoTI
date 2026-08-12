using System.ComponentModel.DataAnnotations;

namespace Elcop.TI.Domain.Enums;

/// <summary>
/// Categorias de ativos de TI controlados pelo inventário.
/// </summary>
public enum TipoAtivo
{
    [Display(Name = "Notebook", GroupName = "Computadores")]
    Notebook = 1,

    [Display(Name = "Desktop", GroupName = "Computadores")]
    Desktop = 2,

    [Display(Name = "Monitor", GroupName = "Periféricos")]
    Monitor = 3,

    [Display(Name = "Celular", GroupName = "Mobilidade")]
    Celular = 4,

    [Display(Name = "Tablet", GroupName = "Mobilidade")]
    Tablet = 5,

    [Display(Name = "Bodycam", GroupName = "Operacional")]
    Bodycam = 6,

    [Display(Name = "Impressora", GroupName = "Periféricos")]
    Impressora = 7,

    [Display(Name = "Scanner", GroupName = "Periféricos")]
    Scanner = 8,

    [Display(Name = "Servidor", GroupName = "Datacenter")]
    Servidor = 9,

    [Display(Name = "Switch", GroupName = "Rede")]
    Switch = 10,

    [Display(Name = "Roteador", GroupName = "Rede")]
    Roteador = 11,

    [Display(Name = "Access Point", GroupName = "Rede")]
    AccessPoint = 12,

    [Display(Name = "Nobreak / UPS", GroupName = "Datacenter")]
    Nobreak = 13,

    [Display(Name = "Projetor", GroupName = "Periféricos")]
    Projetor = 14,

    [Display(Name = "Headset", GroupName = "Periféricos")]
    Headset = 15,

    [Display(Name = "Teclado", GroupName = "Periféricos")]
    Teclado = 16,

    [Display(Name = "Mouse", GroupName = "Periféricos")]
    Mouse = 17,

    [Display(Name = "Docking Station", GroupName = "Periféricos")]
    DockingStation = 18,

    [Display(Name = "HD / SSD Externo", GroupName = "Armazenamento")]
    ArmazenamentoExterno = 19,

    [Display(Name = "Rádio Comunicador", GroupName = "Operacional")]
    RadioComunicador = 20,

    [Display(Name = "Chip / Linha Móvel", GroupName = "Mobilidade")]
    ChipLinhaMovel = 21,

    [Display(Name = "Câmera de Segurança", GroupName = "Operacional")]
    CameraSeguranca = 22,

    [Display(Name = "Leitor Biométrico", GroupName = "Operacional")]
    LeitorBiometrico = 23,

    [Display(Name = "Licença de Software", GroupName = "Software")]
    LicencaSoftware = 24,

    [Display(Name = "Outro", GroupName = "Diversos")]
    Outro = 99
}

/// <summary>
/// Situação atual do ativo dentro do ciclo de vida do inventário.
/// </summary>
public enum StatusAtivo
{
    [Display(Name = "Disponível")]
    Disponivel = 1,

    [Display(Name = "Em uso")]
    EmUso = 2,

    [Display(Name = "Reservado")]
    Reservado = 3,

    [Display(Name = "Em manutenção")]
    EmManutencao = 4,

    [Display(Name = "Emprestado")]
    Emprestado = 5,

    [Display(Name = "Extraviado")]
    Extraviado = 6,

    [Display(Name = "Danificado")]
    Danificado = 7,

    [Display(Name = "Baixado")]
    Baixado = 8
}

/// <summary>
/// Estado físico/funcional do equipamento.
/// </summary>
public enum CondicaoAtivo
{
    [Display(Name = "Novo")]
    Novo = 1,

    [Display(Name = "Ótimo")]
    Otimo = 2,

    [Display(Name = "Bom")]
    Bom = 3,

    [Display(Name = "Regular")]
    Regular = 4,

    [Display(Name = "Ruim")]
    Ruim = 5,

    [Display(Name = "Inservível")]
    Inservivel = 6
}
