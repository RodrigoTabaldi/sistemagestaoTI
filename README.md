# ELCOP · Gestão de TI

Sistema web para **controle de estoque/inventário de ativos de TI** e **gestão de demandas**,
construído em ASP.NET Core 8 (MVC) com EF Core, arquitetura em camadas e front-end próprio.

---

## Como executar

Defina a senha do administrador **antes da primeira execução** e suba a aplicação:

```bash
cd C:\Softelcop
dotnet user-secrets set "Elcop:Admin:Senha" "SuaSenhaForte1" --project src\Elcop.TI.Web
dotnet run --project src\Elcop.TI.Web
```

Acesse a porta indicada no console (por padrão **http://localhost:5057**).

Na primeira execução o sistema cria o banco SQLite (`elcop-ti.db`), aplica as migrations,
cadastra os perfis de acesso e cria **um único usuário administrador**. O banco nasce vazio:
todo o conteúdo é cadastrado por você.

### Primeiro acesso

| | |
|---|---|
| E-mail | o valor de `Elcop:Admin:Email` (padrão `admin@elcop.com.br`) |
| Senha  | o valor de `Elcop:Admin:Senha` |

Se `Elcop:Admin:Senha` não estiver definida, o sistema **gera uma senha aleatória e a escreve
no log de inicialização** — ela aparece uma única vez, na execução que criou o usuário.
Alternativa ao user-secrets: a variável de ambiente `Elcop__Admin__Senha`.

Os demais usuários são criados em **Usuários → Novo**, ou se cadastram sozinhos pela tela de
login (veja abaixo).

> A senha nunca fica no `appsettings.json` e não é exibida na tela de login.

### Autocadastro

A tela de login traz um link **Criar conta**. Com `AprovacaoAutomatica: true` (padrão atual do
projeto), quem se cadastra recebe o perfil `Consulta` e **entra imediatamente**, sem depender de
um administrador. Ajuste o campo abaixo caso prefira liberar manualmente em **Usuários**.

```jsonc
"Elcop": {
  "AutoCadastro": {
    "Habilitado": true,
    "PerfilPadrao": "Consulta",
    "AprovacaoAutomatica": true,       // false = conta nasce desabilitada até um admin liberar
    "DominiosPermitidos": []           // ex.: [ "elcop.com.br" ] restringe o e-mail
  }
}
```

> Uma conta criada **antes** de mudar `AprovacaoAutomatica` fica com o valor que estava em vigor
> na hora do cadastro — mudar a configuração não libera retroativamente quem já estava pendente.
> Libere manualmente em **Usuários** ou cadastre a conta de novo.

- `Habilitado: false` remove o link e bloqueia as rotas de cadastro (404).
- `AprovacaoAutomatica: true` dá acesso imediato a qualquer pessoa que abra a tela de login —
  use apenas em rede interna confiável, e de preferência junto com `DominiosPermitidos`.
- Cadastro com e-mail já existente devolve a mesma tela de sucesso, sem criar nada: a tela não
  serve para descobrir quem tem conta.

---

## Arquitetura

```
Elcop.TI.sln
└── src
    ├── Elcop.TI.Domain          Entidades, enums e regras de negócio puras (sem dependências)
    ├── Elcop.TI.Application     Contratos, DTOs, filtros e serviços de aplicação
    ├── Elcop.TI.Infrastructure  EF Core, Identity, Firebase, armazenamento, seed, migrations
    └── Elcop.TI.Web             MVC: controllers, views Razor, design system, wwwroot
```

**Fluxo de dependência:** `Web → Infrastructure → Application → Domain`.

A camada Application depende apenas de abstrações (`IAppDbContext`, `IArmazenamentoArquivos`),
então o provedor de banco e o destino dos arquivos ficam confinados à Infrastructure e os
serviços permanecem testáveis.

### Decisões de projeto

- **Exclusão lógica** em todas as entidades (`Excluido` + *global query filters*): o histórico de
  posse de um equipamento nunca é perdido.
- **A posse é derivada das movimentações**, não editável pelo formulário do ativo — impede que o
  inventário e o histórico divirjam.
- **`RegraDeNegocioException`** para violações previstas (patrimônio duplicado, ativo já entregue,
  desligar colaborador com equipamento). Um *exception filter* converte em toast, sem página de erro.
- **Trilha de auditoria** gravada na mesma transação da operação auditada.
- **Carimbo de criação/alteração** aplicado no `SaveChanges` como rede de segurança.

---

## Infraestrutura e escalabilidade

Preparado para rodar em PaaS (**Render** ou **Firebase/Cloud Run**), que já fornecem load
balancer, TLS e escalonamento horizontal como recurso de plataforma — nenhum desses três é
implementado em código. O que o código faz é a contraparte necessária para funcionar
corretamente atrás desse tipo de proxy, além de cache e fila para reduzir latência percebida.

| Peça | Implementação | Onde |
|---|---|---|
| **Cache de leitura** | `IMemoryCache` via `ICacheService`, TTL 5 min (listas de seleção) / 60 s (painel e resumos) | `Infrastructure/Caching/MemoryCacheService.cs` |
| **Rate limiting** | `Microsoft.AspNetCore.RateLimiting` nativo do .NET 8: 300 req/min por IP global, 10 tentativas/5 min no login, 20 req/min em upload | `Program.cs` |
| **Fila de background** | `System.Threading.Channels` + `BackgroundService`, usada para remover foto antiga sem bloquear a resposta HTTP | `Infrastructure/BackgroundJobs/` |
| **Forwarded headers** | Confia em `X-Forwarded-For`/`X-Forwarded-Proto` do proxy da plataforma — necessário para o rate limiting por IP e a detecção de HTTPS funcionarem atrás do load balancer | `Program.cs` |
| **Health check** | `GET /health` verifica `Database.CanConnectAsync()` — é o endpoint que a plataforma usa para saber se a instância está saudável | `Infrastructure/Health/DbHealthCheck.cs` |

**Por que não Redis/Docker:** com uma única instância (o caso de uso atual em Render/Firebase),
cache e fila em memória do processo já resolvem o problema, sem infraestrutura externa para
manter. Cache e fila tolerando alguma inconsistência entre instâncias (dashboard, listas de
apoio, limpeza de foto antiga) — nada crítico depende deles. Se um dia o sistema escalar para
múltiplas instâncias, o caminho é trocar `MemoryCacheService`/`BackgroundTaskQueue` por uma
implementação sobre Redis, sem tocar em nenhum consumidor: ambos são acessados só pela
interface (`ICacheService`/`IBackgroundTaskQueue`), definida na camada Application.

---

## Funcionalidades

### Inventário de ativos
- 24 tipos: notebook, desktop, monitor, **celular**, tablet, **bodycam**, impressora, scanner,
  servidor, switch, roteador, access point, nobreak, projetor, headset, teclado, mouse, docking,
  HD externo, rádio comunicador, chip/linha móvel, câmera, leitor biométrico, licença.
- Identificação: patrimônio (único), **número de série** (único), **IMEI/IMEI 2**, linha, operadora,
  MAC, etiqueta, cor.
- Especificação técnica (processador, RAM, armazenamento, SO, hostname) e dados de aquisição
  (fornecedor, NF, valor, **garantia**, contrato) — os blocos aparecem conforme o tipo escolhido.
- **Foto do ativo** com pré-visualização (JPG/PNG/WebP, até 4 MB).
- 8 status e 6 condições, filtros combináveis, busca por qualquer identificador, ficha imprimível.

### Movimentações — os "slots" de retirada e devolução
- **Entrega:** ativo, colaborador, tipo (entrega/empréstimo/transferência/manutenção/baixa),
  data e hora, previsão de devolução, condição na retirada, acessórios, responsável, local,
  observações e assinatura do termo. Pré-visualização ao vivo do ativo e do colaborador.
- **Devolução:** data, condição na devolução, acessórios conferidos, responsável pelo recebimento,
  marcação de avaria (que envia o ativo para manutenção) e destino do equipamento.
- **Transferência** entre colaboradores em uma operação atômica (encerra um termo e abre outro).
- **Termo de responsabilidade** imprimível, com texto jurídico e campos de assinatura.
- Protocolo automático `MOV-2026-00001` e reclassificação automática de devoluções em atraso.

### Demandas
- Código automático `DEM-2026-0001`, 11 categorias, 4 prioridades, 6 status, etiquetas.
- **SLA sugerido pela prioridade** (crítica 4 h · alta 1 dia · média 3 dias · baixa 7 dias).
- **Linha do tempo** com todos os andamentos, apontamento de horas e progresso.
- **Quadro kanban com arrastar e soltar** — cada movimento vira um registro na linha do tempo.
- Vínculo opcional com ativo e solicitante.

### Painel, relatórios e administração
- Painel com 6 KPIs, alertas acionáveis, gráficos SVG (rosca, barras, linhas de 6 meses),
  devoluções pendentes e garantias vencendo.
- Exportações CSV (UTF-8 com BOM, separador `;` — abre direto no Excel pt-BR).
- Cadastros de apoio, gestão de usuários com 3 perfis e consulta à trilha de auditoria.

---

## Front-end

Design system próprio, sem frameworks CSS, derivado da identidade ELCOP
(vinho `#8B1F26` + grafite `#3D3D3D`):

- **Tipografia IBM Plex Sans/Mono**, auto-hospedada em `wwwroot/fonts` — nenhuma requisição a
  CDN, então a interface carrega igual em rede interna sem saída para a internet.
- **Telas de acesso** (login e cadastro) em coluna única centrada, com a logo como elemento de
  maior peso visual e o cartão delimitado só por borda, sem fundo próprio.
- **Tema claro/escuro** com persistência e detecção da preferência do sistema.
- Cor chapada em vez de gradiente, borda de 1px em vez de sombra pesada. Movimento apenas onde
  comunica mudança de estado (toast, modal, menu, cartão sendo arrastado) — números e listas
  aparecem prontos, sem animação de entrada.
- Sidebar recolhível com contadores ao vivo, toasts, modais de confirmação, menus suspensos e abas.
- Máscaras (CPF, CNPJ, telefone, IMEI), validação remota de duplicidade e validação
  client-side via jQuery Validation Unobtrusive.
- **Responsivo de verdade em telas de toque**: alvos de 40px, ações de linha sempre visíveis
  (sem depender de hover), tabelas e kanban com rolagem própria em vez de estourar a página,
  filtros e KPIs em coluna única e barra de ações rente às bordas no celular.
- Atalhos (`/` busca, `Ctrl+K` tema) e `prefers-reduced-motion` respeitado.

---

## Configuração

`src/Elcop.TI.Web/appsettings.json`:

```jsonc
{
  "ConnectionStrings": { "Padrao": "Data Source=elcop-ti.db" },
  "Elcop": {
    "Provedor": "Sqlite",          // Sqlite | SqlServer | Postgres
    "Armazenamento": "Local",      // Local | Firebase
    "Admin": { "Email": "admin@elcop.com.br" },
    "Firebase": { "Habilitado": false }
  }
}
```

### Trocar de banco

| Provedor | Quando usar | Connection string |
|---|---|---|
| `Sqlite` | Desenvolvimento e instalação em máquina única | `Data Source=elcop-ti.db` |
| `SqlServer` | Servidor Windows já existente | `Server=...;Database=ElcopTI;User Id=...;Password=...;TrustServerCertificate=True` |
| `Postgres` | **Nuvem** (Cloud SQL / Firebase Data Connect) | veja abaixo |

Migrations são específicas do provedor. Sqlite e SQL Server compartilham o conjunto em
`Persistence/Migrations`; o PostgreSQL tem o seu próprio em `Persistence/Migrations/Postgres`,
usado através do contexto `AppDbContextPostgres`. Os dois convivem — não é preciso apagar nada
para alternar.

Para regerar o conjunto do Postgres depois de mudar o modelo:

```bash
dotnet ef migrations add NomeDaMigration ^
  --project src\Elcop.TI.Infrastructure ^
  --startup-project src\Elcop.TI.Web ^
  --context AppDbContextPostgres ^
  --output-dir Persistence\Migrations\Postgres
```

---

## Nuvem: Google Cloud + Firebase

O sistema é relacional (EF Core + ASP.NET Identity). O **Firestore não substitui o banco** —
é NoSQL e não tem provider de EF Core. A divisão adotada:

| Necessidade | Serviço | Configuração |
|---|---|---|
| Banco de dados | **Cloud SQL for PostgreSQL** (o mesmo que o Firebase Data Connect usa) | `Elcop:Provedor = "Postgres"` |
| Login | **Firebase Authentication** | `Elcop:Firebase:Habilitado = true` |
| Fotos e anexos | **Firebase Cloud Storage** | `Elcop:Armazenamento = "Firebase"` |

### 1. Banco no Cloud SQL

```jsonc
"ConnectionStrings": {
  // Cloud Run / App Engine (socket Unix):
  "Padrao": "Host=/cloudsql/PROJETO:REGIAO:INSTANCIA;Database=elcopti;Username=USUARIO;Password=SENHA"
  // Fora do Google Cloud, via Cloud SQL Auth Proxy:
  // "Padrao": "Host=127.0.0.1;Port=5432;Database=elcopti;Username=USUARIO;Password=SENHA;SSL Mode=Require"
},
"Elcop": { "Provedor": "Postgres" }
```

As migrations do Postgres são aplicadas automaticamente na inicialização.

### 2. Login via Firebase Authentication

No console do Firebase: **Authentication → Sign-in method → Google**, e em
**Configurações → Seus apps → App da Web** copie a `apiKey` e o `authDomain`.

```jsonc
"Elcop": {
  "Firebase": {
    "Habilitado": true,
    "ProjectId": "seu-projeto",
    "ApiKeyWeb": "AIza...",
    "AuthDomain": "seu-projeto.firebaseapp.com",
    "ProvisionarAutomaticamente": false,
    "PerfilPadrao": "Consulta"
  }
}
```

O botão **Entrar com o Google** passa a aparecer na tela de login. O fluxo é:
o navegador obtém um ID token pelo SDK do Firebase → o servidor **valida a assinatura do token**
pelo Admin SDK → localiza o usuário local pelo e-mail → abre a sessão.

**O Firebase apenas prova a identidade.** Perfil de acesso, bloqueio de conta e trilha de
auditoria continuam no ASP.NET Identity, então nenhuma regra de autorização depende da nuvem.
O login por e-mail e senha continua funcionando em paralelo.

- `ProvisionarAutomaticamente: false` (recomendado) — só entra quem já foi cadastrado em
  **Usuários**. Com `true`, qualquer conta aceita pelo projeto vira usuário com o `PerfilPadrao`.
- Contas sem e-mail verificado são recusadas.

### 3. Anexos no Cloud Storage

```jsonc
"Elcop": {
  "Armazenamento": "Firebase",
  "Firebase": { "Bucket": "seu-projeto.appspot.com" }
}
```

As fotos passam a ser gravadas no bucket em vez de `wwwroot/uploads`. Registros antigos
continuam apontando para o disco — a troca vale para os envios seguintes.

### Credenciais do Google

Em produção no Google Cloud, **não use arquivo de chave**: a identidade do serviço (ADC) é
detectada sozinha. Fora do Google Cloud, aponte o JSON da conta de serviço:

```jsonc
"Elcop": { "Firebase": { "CaminhoCredencial": "C:\\segredos\\service-account.json" } }
```

---

## Perfis de acesso

| Perfil          | Permissões |
|-----------------|-----------|
| `Administrador` | Tudo, incluindo usuários, cadastros de apoio e auditoria |
| `Tecnico`       | Cadastra ativos/colaboradores, movimenta e trata demandas |
| `Consulta`      | Leitura de inventário, movimentações e demandas, **e abrir novas demandas** |

Qualquer usuário autenticado pode **registrar uma nova demanda** — é o fluxo normal de um
colaborador abrir um chamado para o setor de TI. Editar, mover no quadro kanban e excluir uma
demanda continuam restritos a `Tecnico`/`Administrador`.

Toda a aplicação exige autenticação por padrão (`FallbackPolicy`); o acesso anônimo é declarado
explicitamente apenas nas telas de login e erro.
