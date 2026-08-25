# Operação em produção

## Antes de publicar

- Defina `Elcop__Admin__Senha` por secret manager ou variável de ambiente.
- Defina `AllowedHosts` com os hosts reais; nunca use `*` fora de Development.
- Mantenha `Elcop__AplicarMigrationsNoStartup=false`. Aplique migrations uma única vez na etapa de deploy, antes de iniciar as réplicas.
- O autocadastro é desabilitado em `appsettings.Production.json`. Para habilitá-lo, informe domínios corporativos em `Elcop__AutoCadastro__DominiosPermitidos__0` etc. A aplicação não inicia em produção se autocadastro estiver habilitado sem essa lista.

## Saúde e observabilidade

- `/health/live` é anônimo e não consulta o banco: use-o como liveness probe.
- `/health/ready` verifica o banco e requer administrador fora de Development. Exponha-o somente na rede do monitoramento, ou use uma credencial de probe com privilégio mínimo.
- Colete o `Meter` `Elcop.TI.Web` com o host/OpenTelemetry. Crie alertas para taxa de respostas 5xx, duração p95/p99 e indisponibilidade do readiness.

## Qualidade e segurança contínuas

- A CI falha abaixo de 75% de linhas ou 65% de ramos na cobertura Cobertura consolidada.
- A CI falha para CVEs altos ou críticos, publica o relatório completo de dependências vulneráveis e o Dependabot abre atualizações semanais. Corrija CVEs antes de releases, registrando exceções com prazo e responsável.
- Exija revisão por pares em pull requests que alterem `src/Elcop.TI.Infrastructure`, `src/Elcop.TI.Web/Program.cs`, autenticação, autorização, migrations ou pipeline.
