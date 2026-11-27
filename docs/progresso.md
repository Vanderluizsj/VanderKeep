# VKeeper — Registro de Progresso

> Documento oficial de acompanhamento de progresso conforme `docs/PROCESS.md`.

---

## Status geral

- **Fase atual:** Fase 3 concluída e aprovada em revisão independente de segurança — pronto para iniciar a **Fase 4 (Casos de Uso e MVC de Categorias)**.
- **Repositório remoto:** `origin` configurado em `https://github.com/Vanderluizsj/VanderKeep.git` (Fases 0, 1 e 2 publicadas; Fase 3 pronta para push).

---

## Fases concluídas

### Fase 0 — Descoberta, Planejamento e Arquitetura (Concluída em 2026-11-26)
- Entrevista interativa e refinamento arquitetural concluídos cobrindo produto, escopo do MVP (Pin, Arquivamento e Lixeira com soft delete e restauração), regras de negócio, segurança/Identity, Clean Architecture organizada por feature, convenção de pastas/namespaces (`ADR-16`), persistência SQL Server (container local + workflow CI futuro) e testes xUnit.
- Documentação oficial consolidada e aprovada: `docs/planejamento-de-produto.md`, `docs/decisoes-tecnicas.md`, `docs/progresso.md`.

### Fase 1 — Fundação da Solução e Infraestrutura Base (Concluída em 2026-11-26)
- Solução em `.NET 10.0` (pacotes Microsoft `10.0.15`, FluentValidation `12.2.0`, xUnit `2.9.3`) estruturada nos 6 projetos da Clean Architecture seguindo `ADR-16`, com CSRF global, tratamento de erros HTTP, layout Bootstrap 5 em `pt-BR` e suítes iniciais de testes.

### Fase 2 — Domínio e Persistência Core (Concluída em 2026-11-27)
- Entidades `Note`, `Category`, `NoteCategory`, enums `NotePriority` e `NoteSortBy`, repositórios `NoteRepository` e `CategoryRepository` isolados por `UserId`, Fluent API com `ADR-13` (sem *multiple cascade paths*), rebrand completo para **`VKeeper`** (`ADR-18`), proteção de `appsettings.Development.json` no `.gitignore` (`ADR-19`) e aplicação real da migration `20261127001307_InitialCreate` no banco **`VKeeperDb`** (container `sqlserver-backend`).

### Fase 3 — Autenticação, Contas e Segurança (Concluída em 2026-11-27)
- **Identidade e Segurança (`VKeeper.Infraestrutura` / `VKeeper.Web`):**
  - `ApplicationUser` inicializa `Id = Guid.CreateVersion7()` no construtor (atendendo `ADR-06` em runtime preservando o mapeamento padrão do Identity sem migration extra).
  - `VKeeperUserClaimsPrincipalFactory` emitindo `CustomClaimTypes.DisplayName` (definida em `VKeeper.Aplicacao.Comum`, mantendo views sem dependência de `Infraestrutura`).
  - Endurecimento consistente de cookies (`HttpOnly = true`, `SecurePolicy = Always` fora de `Development`) em Identity (`SameSite = Lax`), Antiforgery (`SameSite = Strict`) e `CookieTempDataProviderOptions` (`SameSite = Lax`).
  - `AccountController` + `IdentityErrorTranslator` + ViewModels (`Register`, `Login`, `ChangePassword`) + views MVC em Bootstrap 5 (`pt-BR`), navbar autenticada em `_Layout.cshtml` com logout `POST` anti-CSRF, anti-enumeração, lockout após 5 tentativas, proteção contra Open Redirect (`https://` e `//` com limpeza de `ModelState`), `RefreshSignInAsync` após troca de senha e `.validation-summary-valid` oculto no `site.css`.
- **Infraestrutura de testes de integração (`VKeeperDb_Test`):**
  - `VKeeperWebApplicationFactory` com `IntegrationTestCollection` (`ICollectionFixture`), fail-fast se sem string de teste, guarda explícita impedindo execução contra `VKeeperDb` e aplicação automática de migrations no banco isolado **`VKeeperDb_Test`**.

---

## Testes executados

### Fase 3 (2026-11-27)
- `dotnet build VKeeper.sln --no-restore`: **Êxito (0 avisos, 0 erros)**.
- `VKeeper.TestesUnitarios`: **46/46 aprovados** (`BaseEntityTests`, `ResultTests`, `PagedResultTests`, `CategoryTests`, `NoteCategoryTests`, `NoteTests`).
- `VKeeper.TestesIntegracao`: **33/33 aprovados** contra o banco real **`VKeeperDb_Test`**:
  - `SmokeTests` (`12`), `IdentityErrorTranslatorTests` (`3`), `CurrentUserServiceTests` (`4`) e `AccountIntegrationTests` (`14` cobrindo cadastro UUID v7, e-mail duplicado, senha fraca, anti-enumeração, `ReturnUrl` local e anti-Open Redirect, lockout efetivo rejeitando senha correta, CSRF no logout, invalidação de sessão, troca de senha, atributos `Set-Cookie` e `AccessDenied`).
- **Total consolidado:** **79/79 testes aprovados**.

---

## Revisões realizadas

### Fase 1 (2026-11-26)
- Rodada 1: Reprovado -> Rodada 2: **Aprovado**.

### Fase 2 (2026-11-27)
- **Aprovado com ressalvas** -> todos os 5 refinamentos de código aplicados e migration `InitialCreate` aplicada com sucesso em `VKeeperDb`.

### Fase 3 — Auditoria independente de segurança e arquitetura pelo Reviewer (2026-11-27)
- **Rodada 1:** Aprovado com ressalvas (0 Críticos, 0 Altos, 3 Médios: caixa `validation-summary-valid` vazia, `SecurePolicy` em Antiforgery/TempData, cobertura complementar em `CurrentUserService` e `AccountIntegrationTests`; 4 Baixos).
- **Rodada 2 (pós-correções):** **Aprovado** — todos os apontamentos resolvidos sem achados remanescentes.

---

## Pendências conhecidas

1. **Hospedagem e deploy em nuvem (`ADR-15`):** decisão adiada deliberadamente para pós-MVP.

---

## Próximas atividades

1. Publicar o commit da Fase 3 no GitHub (`git push origin main`).
2. Iniciar a **Fase 4 — Casos de Uso e MVC de Categorias** (`CategoryService`, FluentValidation, `CategoriesController`, views Bootstrap em `pt-BR`, testes unitários e testes de integração anti-IDOR contra `VKeeperDb_Test`).
