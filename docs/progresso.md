# VKeeper — Registro de Progresso

> Documento oficial de acompanhamento de progresso conforme `docs/PROCESS.md`.

---

## Status geral

- **Fase atual:** Fase 2 — Domínio e Persistência Core (**em andamento — código implementado, revisado e aprovado com ressalvas; pendente apenas aplicação da migration `InitialCreate` no SQL Server `VKeeperDb`**).
- **Repositório remoto:** `origin` configurado em `https://github.com/Vanderluizsj/VanderKeep.git` (Fase 0 e Fase 1 publicadas).

---

## Fases concluídas

### Fase 0 — Descoberta, Planejamento e Arquitetura (Concluída em 2026-11-26)
- Análise inicial do repositório concluída (confirmada ausência de documentos legados de referência em `docs/`, detectado SDK `.NET 10.0.115`).
- Entrevista interativa e refinamento arquitetural concluídos cobrindo produto, escopo do MVP (incluindo Pin, Arquivamento e Lixeira com soft delete e restauração), regras de negócio, segurança/Identity, Clean Architecture organizada por feature, convenção de pastas/namespaces (ADR-16), persistência SQL Server (container local + workflow CI futuro) e testes xUnit.
- Documentação oficial consolidada e aprovada: `docs/planejamento-de-produto.md`, `docs/decisoes-tecnicas.md`, `docs/progresso.md`.
- Referências padronizadas em `docs/PROCESS.md` e `.agents/agents/architect/agent.md`; seção 2 de `AGENTS.md` alinhada à **ADR-16**.

### Fase 1 — Fundação da Solução e Infraestrutura Base (Concluída em 2026-11-26)
- Criada solução `VKeeper.sln` em `.NET 10.0` (pacotes Microsoft fixados em `10.0.15`, FluentValidation `12.2.0`, xUnit `2.9.3`) com os 6 projetos respeitando a direção de dependências da Clean Architecture e a ADR-16.
- Inicializado `UserSecretsId` em `VKeeper.Web` e `VKeeper.TestesIntegracao`, mantendo `appsettings.json` sem credenciais.
- Criado `.gitignore` padrão `.NET` e publicado na branch `main` do GitHub.

---

## Fases em andamento

### Fase 2 — Domínio e Persistência Core (Implementação e revisão de código concluídas em 2026-11-27; pendente `database update`)
- **Domínio (`VKeeper.Dominio`):**
  - Enums `NotePriority` (`Low`, `Medium`, `High`) e `NoteSortBy`.
  - Entidades `Note` (máquina de estados completa Ativa/Arquivada/Lixeira, `Pin`/`Unpin`/`Archive`/`Unarchive`/`MoveToTrash`/`Restore`, diff incremental em `UpdateCategories`, limites `150`/`5000`, idempotência de `UpdatedAtUtc`), `Category` (`NormalizedTitle` único por usuário, limite `50`) e `NoteCategory` (relação N:N explícita).
  - Contratos `INoteRepository` (incluindo `asNoTracking`), `ICategoryRepository`, `CategorySummary` e `NoteQueryFilter` com isolamento obrigatório por `UserId`.
- **Persistência (`VKeeper.Infraestrutura`):**
  - `VKeeperDbContext` atualizado com `Notes`, `Categories` e `NoteCategories`.
  - Configurações Fluent API (`ApplicationUserConfiguration`, `NoteConfiguration`, `CategoryConfiguration`, `NoteCategoryConfiguration`) com `ValueGeneratedNever()` para UUID v7, índice único `UX_Categories_UserId_NormalizedTitle`, índices compostos em `Notes` e `NoteCategories` e mitigação de *Multiple Cascade Paths* (`ADR-13`).
  - Repositórios `NoteRepository` (`AsNoTracking`, retorno antecipado quando `totalCount == 0`, paginação SQL, `AsSplitQuery`) e `CategoryRepository` (contagem ignorando `IsDeleted` e reforçando `UserId`) registrados no container DI.
  - Migration inicial gerada: `20261127001307_InitialCreate.cs`.
- **Pendência para fechamento formal da Fase 2:** preencher a senha do container `sqlserver-backend` (`localhost,1433`) em `src/VKeeper.Web/appsettings.Development.json` (arquivo ignorado no `.gitignore`) e executar `dotnet ef database update` validando a criação do schema no banco `VKeeperDb`.
- **Rebrand concluído (2026-11-27 — ADR-18 e ADR-19):** solução, 6 projetos, namespaces, `VKeeperDbContext`, `VKeeperWebApplicationFactory`, migration, testes, UI e documentação migrados integralmente para **`VKeeper`**; `appsettings.Development.json` adicionado ao `.gitignore` com `appsettings.Development.Example.json` versionado.

---

## Testes executados

### Fase 1 (2026-11-26)
- `dotnet build VKeeper.sln --no-restore`: **Êxito (0 avisos, 0 erros)**.
- `VKeeper.TestesUnitarios`: **13/13 aprovados**.
- `VKeeper.TestesIntegracao`: **12/12 aprovados**.

### Fase 2 (2026-11-27)
- `dotnet build VKeeper.sln --no-restore`: **Êxito (0 avisos, 0 erros)**.
- `VKeeper.TestesUnitarios`: **46/46 aprovados** (`BaseEntityTests`, `ResultTests`, `PagedResultTests`, `CategoryTests`, `NoteCategoryTests`, `NoteTests` cobrindo todas as invariantes, casos de fronteira e máquina de estados).
- `VKeeper.TestesIntegracao`: **12/12 aprovados** (`SmokeTests` incluindo resolução DI de `INoteRepository` e `ICategoryRepository`).

---

## Revisões realizadas

### Fase 1 — Auditoria independente pelo agente Reviewer (2026-11-26)
- **Rodada 1:** Reprovado (1 Alto, 3 Médios, 4 Baixos).
- **Rodada 2 (pós-correções):** **Aprovado** — todos os 8 apontamentos resolvidos.

### Fase 2 — Auditoria independente pelo agente Reviewer (2026-11-27)
- **Resultado:** **Aprovado com ressalvas.**
- Todos os apontamentos de código (1 Médio: `asNoTracking` em `GetByIdAsync`; 4 Baixos: retorno antecipado em `ListPagedAsync`, reforço de `UserId` na contagem de categorias, guarda contra `null` em `Category.NormalizeTitle`, idempotência de `UpdatedAtUtc` em no-op de `Note` e testes de fronteira) foram corrigidos e revalidados (**58/58 testes aprovados**).
- Ressalva aberta (obrigatória para fechamento formal da Fase 2): aplicar `dotnet ef database update` no SQL Server real (`VKeeperDb`) após configuração dos User Secrets locais e inicialização do container `sqlserver-backend`.
- Observação registrada para a Fase 3: alinhar explicitamente a geração de `Id` em `ApplicationUser` (`ADR-06`).

---

## Pendências conhecidas

1. **Aplicação da migration `InitialCreate` no SQL Server (fechamento da Fase 2):** configurar `ConnectionStrings:DefaultConnection` (`VKeeperDb`) e `ConnectionStrings:TestConnection` (`VKeeperDb_Test`) nos User Secrets apontando para o container Docker `sqlserver-backend` e rodar `dotnet ef database update`.
2. **Alinhamento de `ApplicationUser.Id` (`ADR-06`) na Fase 3:** definir se `ApplicationUser` inicializa `Id = Guid.CreateVersion7()` com `ValueGeneratedNever()` ou mantém padrão do ASP.NET Core Identity.
3. **Hospedagem e deploy em nuvem (`ADR-15`):** decisão adiada deliberadamente para pós-MVP.

---

## Próximas atividades

1. Configurar User Secrets e executar `dotnet ef database update` no banco `VKeeperDb` (container `sqlserver-backend`) para encerrar formalmente a **Fase 2**.
2. Iniciar a **Fase 3 — Autenticação, Contas e Segurança (ASP.NET Core Identity)** seguindo `docs/PROCESS.md`.
