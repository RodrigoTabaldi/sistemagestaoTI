param(
    [Parameter(Mandatory = $true)]
    [string] $DiretorioResultados,
    [double] $MinimoLinha = 75,
    [double] $MinimoRamo = 65
)

$relatorios = Get-ChildItem -Path $DiretorioResultados -Filter coverage.cobertura.xml -Recurse
if ($relatorios.Count -eq 0) {
    throw "Nenhum relatório Cobertura foi encontrado em $DiretorioResultados."
}

[double] $linhasCobertas = 0; [double] $linhasValidas = 0
[double] $ramosCobertos = 0; [double] $ramosValidos = 0
foreach ($relatorio in $relatorios) {
    [xml] $xml = Get-Content -LiteralPath $relatorio.FullName
    $linhasCobertas += [double] $xml.coverage.'lines-covered'
    $linhasValidas += [double] $xml.coverage.'lines-valid'
    $ramosCobertos += [double] $xml.coverage.'branches-covered'
    $ramosValidos += [double] $xml.coverage.'branches-valid'
}

$linha = if ($linhasValidas) { 100 * $linhasCobertas / $linhasValidas } else { 100 }
$ramo = if ($ramosValidos) { 100 * $ramosCobertos / $ramosValidos } else { 100 }
Write-Host ("Cobertura consolidada: linhas {0:N2}% | ramos {1:N2}%" -f $linha, $ramo)

if ($linha -lt $MinimoLinha -or $ramo -lt $MinimoRamo) {
    throw ("Cobertura abaixo do mínimo: linhas >= {0}% e ramos >= {1}%" -f $MinimoLinha, $MinimoRamo)
}
