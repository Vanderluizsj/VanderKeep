# VKeeper — Registro de Progresso

> Documento oficial de acompanhamento de progresso conforme `docs/PROCESS.md`.

---

## Status geral

- **Fase atual:** Fase 2 concluída e validada no SQL Server (`VKeeperDb`) — pronto para iniciar a **Fase 3 (Autenticação, Contas e Segurança — ASP.NET Core Identity)**.
- **Repositório remoto:** `origin` configurado em `https://github.com/Vanderluizsj/VanderKeep.git`.

---

## Fases concluídas

### Fase 0 — Descoberta, Planejamento e Arquitetura (Concluída em 2026-11-26)
- Análise inicial do repositório concluída (confirmada ausência de documentos legados de referência em `docs/`, detectado SDK `.NET 10.0.115`).
- Entrevista interativa e refinamento arquitetural concluídos cobrindo produto, escopo do MVP (Pin, Arquivamento e Lixeira com soft delete e restauração), regras de negócio, segurança/Identity, Clean Architecture organizada por feature, convenção de pastas/namespaces (`ADR-16`), persistência SQL Server (container local + workflow CI futuro) e testes xUnit.
- Documentação oficial consolidada e aprovada: `docs/planejamento-de-produto.md`, `docs/decisoes-tecnicas.md`, `docs/progresso.md`.
- Referências padronizadas em `docs/PROCESS.md` e `.agents/agents/architect/agent.md`; seção 2 de `AGENTS.md` alinhada à **ADR-16**.

### Fase 1 — Fundação da Solução e Infraestrutura Base (Concluída em 2026-11-26)
- Criada solução em `.NET 10.0` (pacotes Microsoft fixados em `10.0.15`, FluentValidation `12.2.0`, xUnit `2.9.3`) com os 6 projetos respeitando a direção de dependências da Clean Architecture e a `ADR-16`.
- Configurados CSRF global, cookies seguros, `WebEncoderOptions` UTF-8 seguro contra XSS, `ErrorController` para qualquer verbo HTTP com validação de faixa `400..599`, layout base Bootstrap 5 em `pt-BR` e projetos de testes unitários e de integração.

### Fase 2 — Domínio e Persistência Core (Concluída em 2026-11-27)
- **Domínio (`VKeeper.Dominio`):**
  - Enums `NotePriority` (`Low`, `Medium`, `High`) e `NoteSortBy`.
  - Entidades `Note` (máquina de estados Ativa/Arquivada/Lixeira, `Pin`/`Unpin`/`Archive`/`Unarchive`/`MoveToTrash`/`Restore`, diff incremental em `UpdateCategories`, limites `150`/`5000`, idempotência de `UpdatedAtUtc`), `Category` (`NormalizedTitle` único por usuário, limite `50`) e `NoteCategory` (junção N:N explícita).
  - Contratos `INoteRepository` (incluindo `asNoTracking`), `ICategoryRepository`, `CategorySummary` e `NoteQueryFilter` com isolamento obrigatório por `UserId`.
- **Persistência (`VKeeper.Infraestrutura`):**
  - `VKeeperDbContext` atualizado com `Notes`, `Categories` e `NoteCategories`.
  - Configurações Fluent API (`ApplicationUserConfiguration`, `NoteConfiguration`, `CategoryConfiguration`, `NoteCategoryConfiguration`) com `ValueGeneratedNever()` para UUID v7, índice único `UX_Categories_UserId_NormalizedTitle`, índices compostos em `Notes` e `NoteCategories` e mitigação de *Multiple Cascade Paths* (`ADR-13`).
  - Repositórios `NoteRepository` e `CategoryRepository` registrados no container DI.
- **Rebrand e configuração segura (`ADR-18` e `ADR-19`):**
  - Rebrand integral da solução, projetos, namespaces, `VKeeperDbContext`, testes, UI e documentação para **`VKeeper`**.
  - `appsettings.Development.json` adicionado ao `.gitignore` com `appsettings.Development.Example.json` versionado; segredos de produção previstos via GitHub Actions Secrets.
- **Validação real no SQL Server:**
  - Migration `20261127001307_InitialCreate` aplicada com sucesso via `dotnet ef database update` no container Docker `sqlserver-backend` (`localhost,1433`), criando o banco **`VKeeperDb`** com todas as tabelas do Identity, `Notes`, `Categories`, `NoteCategories`, FKs sem *multiple cascade paths* e todos os índices esperados.

---

## Testes executados

### Fase 1 (2026-11-26)
- `dotnet build`: **Êxito (0 avisos, 0 erros)**.
- `TestesUnitarios`: **13/13 aprovados**; `TestesIntegracao`: **12/12 aprovados**.

### Fase 2 + Rebrand `VKeeper` (2026-11-27)
- `dotnet build VKeeper.sln`: **Êxito (0 avisos, 0 erros)**.
- `VKeeper.TestesUnitarios`: **46/46 aprovados** (`BaseEntityTests`, `ResultTests`, `PagedResultTests`, `CategoryTests`, `NoteCategoryTests`, `NoteTests`).
- `VKeeper.TestesIntegracao`: **12/12 aprovados** (`SmokeTests` incluindo resolução DI de `INoteRepository` e `ICategoryRepository`).
- `dotnet ef database update`: **Êxito** — banco `VKeeperDb` criado e migration `20261127001307_InitialCreate` aplicada no SQL Server.

---

## Revisões realizadas

### Fase 1 — Auditoria independente pelo agente Reviewer (2026-11-26)
- **Rodada 1:** Reprovado (1 Alto, 3 Médios, 4 Baixos).
- **Rodada 2 (pós-correções):** **Aprovado** — todos os 8 apontamentos resolvidos.

### Fase 2 — Auditoria independente pelo agente Reviewer (2026-11-27)
- **Resultado:** **Aprovado com ressalvas** — todos os 5 apontamentos de código corrigidos e revalidados (**58/58 testes aprovados**) e a ressalva obrigatória (`dotnet ef database update` no SQL Server real) concluída com sucesso em `VKeeperDb`.

---

## Pendências conhecidas

1. **Alinhamento de `ApplicationUser.Id` (`ADR-06`) na Fase 3:** definir se `ApplicationUser` inicializa `Id = Guid.CreateVersion7()` com `ValueGeneratedNever()` ou mantém geração padrão do ASP.NET Core Identity.
2. **Connection string de testes de integração (`VKeeperDb_Test`):** configurar `ConnectionStrings:TestConnection` (ou ler de `appsettings.Development.json` derivando o banco `VKeeperDb_Test`) quando iniciarmos os testes de integração contra SQL Server na Fase 3.
3. **Hospedagem e deploy em nuvem (`ADR-15`):** decisão adiada deliberadamente para pós-MVP.

---

## Próximas atividades

1. Iniciar a **Fase 3 — Autenticação, Contas e Segurança (ASP.NET Core Identity)** seguindo `docs/PROCESS.md` (`AccountController`, views MVC em Bootstrap para Login, Registro, Alterar Senha e Acesso Negado, além de testes de integração contra `VKeeperDb_Test`).
