# VKeeper — Registro de Progresso

> Documento oficial de acompanhamento de progresso conforme `docs/PROCESS.md`.

---

## Status geral

- **Status do MVP:** **Todas as fases do MVP (Fases 0 a 6) concluídas e aprovadas em auditoria técnica independente!**
- **Build e Testes (Release):** **0 avisos, 0 erros** — **128/128 testes aprovados** (`72` unitários + `56` de integração contra SQL Server real `VKeeperDb_Test`).
- **Repositório remoto:** `origin` configurado em `https://github.com/Vanderluizsj/VanderKeep.git` com pipeline unificado CI/CD em `.github/workflows/main_vkeeper.yml` (Azure App Service `vkeeper` + Azure SQL via OIDC e EF Core Migration Bundle — `ADR-15` aprovada).

---

## Fases concluídas

### Fase 0 — Descoberta, Planejamento e Arquitetura (Concluída em 2026-11-26)
- Requisitos, regras de negócio, Clean Architecture por feature, convenção `ADR-16`, persistência SQL Server + CI futuro e roadmap consolidados em `docs/planejamento-de-produto.md`, `docs/decisoes-tecnicas.md` e `docs/progresso.md`.

### Fase 1 — Fundação da Solução e Infraestrutura Base (Concluída em 2026-11-26)
- Solução em `.NET 10.0` (pacotes Microsoft `10.0.15`, FluentValidation `12.2.0`, xUnit `2.9.3`) estruturada nos 6 projetos da Clean Architecture, com CSRF global, tratamento de erros HTTP, layout Bootstrap 5 em `pt-BR` e testes base.

### Fase 2 — Domínio e Persistência Core (Concluída em 2026-11-27)
- Entidades `Note`, `Category`, `NoteCategory`, enums `NotePriority` e `NoteSortBy`, repositórios isolados por `UserId`, Fluent API (`ADR-13`), rebrand completo para **`VKeeper`** (`ADR-18`), proteção de `appsettings.Development.json` no `.gitignore` (`ADR-19`) e aplicação real da migration `InitialCreate` no banco **`VKeeperDb`**.

### Fase 3 — Autenticação, Contas e Segurança (Concluída em 2026-11-27)
- `ApplicationUser` com UUID v7 (`ADR-06`), `VKeeperUserClaimsPrincipalFactory`, endurecimento de cookies (Identity, Antiforgery e TempData), `AccountController` + views MVC em Bootstrap (`Register`, `Login`, `ChangePassword`, `Logout` anti-CSRF, `AccessDenied`), e suíte de integração contra o banco isolado **`VKeeperDb_Test`**.

### Fase 4 — Casos de Uso e MVC de Categorias (Concluída em 2026-11-27)
- `CategoryService`, FluentValidation, `CategoriesController` e views MVC em Bootstrap 5 (`Index`, `Create`, `Edit`, `Delete` com preservação das notas), cobertura unitária e testes de integração anti-IDOR contra `VKeeperDb_Test`.

### Fase 5 — Casos de Uso e MVC de Notas e Lixeira (Concluída em 2026-11-27)
- `NoteService`, `NotesController`, `TrashController`, relacionamento N:N com validação de propriedade (`RN03`), Pin, Arquivamento, Lixeira (mover, restaurar, excluir permanentemente, esvaziar), busca textual, filtros combinados, ordenação, paginação de 12 itens com clamp de página fora do limite, redirecionamento autenticado direto para `/Notes` e cobertura completa unitária + integração anti-IDOR.

### Fase 6 — Polimento de UX, Hardening, Workflow CI e Fechamento do MVP (Concluída em 2026-11-27)
- **Hardening de segurança e robustez:**
  - `SecurityHeadersMiddleware` adicionando `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: strict-origin-when-cross-origin`, `Permissions-Policy` e `Content-Security-Policy`.
  - Tratamento de concorrência TOCTOU no índice único `UX_Categories_UserId_NormalizedTitle`: `UniqueConstraintViolationException` (em `VKeeper.Dominio`), tradução de `SqlException` (`2601`/`2627`) em `UnitOfWork`, tratamento para `Result.Conflict` em `CategoryService` e validação real contra SQL Server em `CategoriesIntegrationTests`.
  - Uniformização de `id == Guid.Empty` (`404 NotFound`) em `CategoriesController` e `CategoryService`.
  - Prevenção progressiva de double-submit em `wwwroot/js/site.js` com guarda `event.defaultPrevented`, controle de reentrada e restauração compatível com *bfcache* (`pageshow`).
- **Polimento visual e acessibilidade:**
  - Remoção de `<script type="importmap"></script>` vazio e de atributos `style="..."` inline (substituídos por `.note-description`, `.note-card`, `.note-card-actions` em `wwwroot/css/site.css`).
  - Adição de `aria-current="page"` nos links ativos da navbar e da paginação.
- **CI/CD e Documentação de portfólio:**
  - Workflow inicial `.github/workflows/ci.yml` evoluído e consolidado em `.github/workflows/main_vkeeper.yml` (CI com *service container* SQL Server 2022 Express + CD para Azure App Service `vkeeper` e Azure SQL via OIDC e `efbundle`, com `EnableRetryOnFailure` validado em `128/128` testes em Release).
  - `README.md` completo na raiz do repositório com visão do produto, arquitetura, segurança, instruções de setup local, testes e CI/CD Azure.

---

## Testes executados

### Validação final do MVP — Fase 6 (2026-11-27)
- `dotnet build VKeeper.sln --no-restore --configuration Release`: **Êxito (0 avisos, 0 erros)**.
- `VKeeper.TestesUnitarios`: **72/72 aprovados** (`298 ms`) — `BaseEntityTests`, `ResultTests`, `PagedResultTests`, `CategoryTests`, `NoteCategoryTests`, `NoteTests`, `CategoryServiceTests` (incluindo fronteira de 50 chars, `Guid.Empty`, títulos inválidos e `UniqueConstraintViolationException`) e `NoteServiceTests`.
- `VKeeper.TestesIntegracao`: **56/56 aprovados** contra o SQL Server real **`VKeeperDb_Test`** — `SmokeTests` (incluindo headers HTTP/CSP), `IdentityErrorTranslatorTests`, `CurrentUserServiceTests`, `AccountIntegrationTests`, `CategoriesIntegrationTests` (incluindo tradução real de `SqlException 2601/2627` em `UnitOfWork`, conflito em `Edit`, `Guid.Empty`, estado vazio e anti-IDOR) e `NotesAndTrashIntegrationTests`.
- **Total consolidado:** **128/128 testes aprovados**.

---

## Revisões realizadas

- **Fase 1 (2026-11-26):** Rodada 1 Reprovado -> Rodada 2 **Aprovado**.
- **Fase 2 (2026-11-27):** Aprovado com ressalvas -> refinamentos + migration `InitialCreate` aplicada em `VKeeperDb`.
- **Fase 3 (2026-11-27):** Rodada 1 Aprovado com ressalvas -> Rodada 2 **Aprovado**.
- **Fase 4 (2026-11-27):** Rodada 1 **Aprovado**.
- **Fase 5 (2026-11-27):** Rodada 1 Aprovado com ressalvas -> Rodada 2 **Aprovado**.
- **Fase 6 (2026-11-27):** Rodada 1 Aprovado com ressalvas (2 Médios: `bfcache`/reentrada em `site.js` e teste real de `UnitOfWork` contra SQL Server; 2 Baixos: `cancel-in-progress` no `ci.yml` e `aria-current="page"`) -> Rodada 2 **Aprovado** com zero achados remanescentes.

---

## Pendências conhecidas (pós-MVP)

1. **Validação da 1ª execução do pipeline CI/CD no GitHub Actions:** acompanhar a execução verde de `.github/workflows/main_vkeeper.yml` (testes, `efbundle` no Azure SQL e deploy no Azure App Service `vkeeper`) após o `git push`.
2. **Rodada de ajustes de UX com o solicitante:** conforme sinalizado pelo solicitante, ajustes visuais/UX pós-testes manuais serão coletados após a publicação inicial.
