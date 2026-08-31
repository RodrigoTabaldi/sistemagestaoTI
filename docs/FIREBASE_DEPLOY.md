# Deploy no Firebase Hosting + Cloud Run

Este projeto e um ASP.NET Core MVC com EF Core. O Firebase App Hosting tem suporte direto para Next.js e Angular; para .NET, o caminho profissional e publicar o app como container no Cloud Run e usar Firebase Hosting como CDN/SSL com rewrite para esse container.

Arquivos adicionados:

- `Dockerfile`: publica o projeto Web em imagem Linux para Cloud Run.
- `firebase.json`: envia todo trafego do Firebase Hosting para o servico Cloud Run `elcop-ti-web` em `southamerica-east1`.
- `cloudbuild.yaml`: builda a imagem no Artifact Registry e faz deploy no Cloud Run.
- `.firebaserc.example`: modelo para vincular o repositorio ao projeto Firebase.

## Pre-requisitos no Google Cloud

1. Criar um projeto Firebase.
2. Ativar Cloud Run, Cloud Build, Artifact Registry, Secret Manager, Cloud SQL Admin e Firebase Hosting.
3. Criar uma instancia Cloud SQL for PostgreSQL e um banco `elcopti`.
4. Criar um repositorio Docker no Artifact Registry:

```bash
gcloud artifacts repositories create elcop-ti \
  --repository-format=docker \
  --location=southamerica-east1
```

## Variaveis obrigatorias

Configure no Cloud Run, de preferencia usando Secret Manager para valores sensiveis:

```text
AllowedHosts=SEU_PROJECT_ID.web.app;SEU_PROJECT_ID.firebaseapp.com;seudominio.com.br
ConnectionStrings__Padrao=Host=/cloudsql/PROJETO:REGIAO:INSTANCIA;Database=elcopti;Username=USUARIO;Password=SENHA
Elcop__Admin__Email=admin@seudominio.com.br
Elcop__Admin__Senha=SENHA_FORTE_INICIAL
Elcop__Firebase__ProjectId=SEU_PROJECT_ID
Elcop__Firebase__ApiKeyWeb=SUA_API_KEY_WEB
Elcop__Firebase__AuthDomain=SEU_PROJECT_ID.firebaseapp.com
Elcop__Firebase__Bucket=SEU_BUCKET
```

Configuracoes ja definidas pelo `cloudbuild.yaml`:

```text
ASPNETCORE_ENVIRONMENT=Production
Elcop__Provedor=Postgres
Elcop__Armazenamento=Firebase
Elcop__Firebase__Habilitado=true
Elcop__AplicarMigrationsNoStartup=false
```

## Migrations

Em producao, mantenha `Elcop__AplicarMigrationsNoStartup=false`. Aplique migrations uma vez antes de trocar trafego:

```bash
dotnet ef database update \
  --project src/Elcop.TI.Infrastructure \
  --startup-project src/Elcop.TI.Web \
  --context AppDbContextPostgres
```

## Deploy

Copie `.firebaserc.example` para `.firebaserc` e troque `SEU_PROJECT_ID`.

Build e deploy do container:

```bash
gcloud builds submit --config cloudbuild.yaml .
```

Deploy da configuracao do Firebase Hosting:

```bash
firebase deploy --only hosting
```

Depois disso o sistema responde em:

```text
https://SEU_PROJECT_ID.web.app
https://SEU_PROJECT_ID.firebaseapp.com
```

## Ajustes finais para operacao profissional

- Configure dominio proprio no Firebase Hosting.
- Defina backups automaticos no Cloud SQL.
- Garanta que o Cloud Run tenha permissao `Cloud SQL Client`.
- Use Secret Manager para senha do banco e senha inicial do administrador.
- Habilite Firebase Authentication com Google e restrinja dominios permitidos quando autocadastro estiver ativo.
- Monitore `/health/live` no Cloud Run; `/health/ready` exige administrador fora de Development.
