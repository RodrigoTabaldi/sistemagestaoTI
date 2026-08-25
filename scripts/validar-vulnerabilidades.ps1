param(
    [Parameter(Mandatory = $true)]
    [string] $Relatorio,
    [string[]] $BloquearSeveridade = @("High", "Critical")
)

if (-not (Test-Path -LiteralPath $Relatorio)) {
    throw "Relatório de vulnerabilidades não encontrado: $Relatorio"
}

$json = Get-Content -LiteralPath $Relatorio -Raw | ConvertFrom-Json
$vulnerabilidades = [System.Collections.Generic.List[object]]::new()

function Visitar-No($no) {
    if ($null -eq $no) { return }

    if ($no -is [System.Collections.IEnumerable] -and $no -isnot [string] -and $no -isnot [pscustomobject]) {
        foreach ($item in $no) { Visitar-No $item }
        return
    }

    if ($no -isnot [pscustomobject]) { return }

    $propriedade = $no.PSObject.Properties['vulnerabilities']
    if ($null -ne $propriedade -and $null -ne $propriedade.Value) {
        foreach ($vulnerabilidade in $propriedade.Value) {
            $vulnerabilidades.Add($vulnerabilidade)
        }
    }

    foreach ($propriedadeFilha in $no.PSObject.Properties) {
        if ($propriedadeFilha.Name -ne 'vulnerabilities') { Visitar-No $propriedadeFilha.Value }
    }
}

Visitar-No $json
$bloqueantes = $vulnerabilidades | Where-Object { $BloquearSeveridade -contains $_.severity }
Write-Host ("Vulnerabilidades encontradas: {0}; bloqueantes: {1}" -f $vulnerabilidades.Count, @($bloqueantes).Count)

if (@($bloqueantes).Count -gt 0) {
    $detalhes = $bloqueantes | ForEach-Object { "$($_.severity): $($_.advisoryurl)" }
    throw ("Dependências com vulnerabilidades bloqueantes: " + ($detalhes -join '; '))
}
