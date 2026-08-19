# ELCOP · Gestão de TI

Sistema web para **controle de estoque/inventário de ativos de TI** e **gestão de demandas**,
construído em ASP.NET Core 8 (MVC) com EF Core, arquitetura em camadas e front-end próprio.

---

## Como executar

Abra o terminal na pasta raiz Web do projeto e execute:
dotnet run

Cole o endereço  ex : `https://localhost:5001` no navegador. A primeira execução cria o banco.

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
- **Abertura em um campo**: só o título é obrigatório — categoria, prioridade, prazo, solicitante
  e descrição têm padrão, e qualquer perfil pode registrar a própria demanda.
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
| `Consulta`      | Leitura de inventário e movimentações; **abre e acompanha demandas** |

Toda a aplicação exige autenticação por padrão (`FallbackPolicy`); o acesso anônimo é declarado
explicitamente apenas nas telas de login e erro.

Abrir demanda é a única escrita liberada ao perfil `Consulta` (política `AbrirDemanda`): ele
registra o chamado num formulário enxuto e entra como solicitante. A condução do atendimento —
status, prazo, responsável, progresso e solução — continua restrita a `Administrador` e `Tecnico`
(política `Operar`), inclusive contra POST forjado: o controller descarta esses campos em vez de
apenas escondê-los do formulário.
