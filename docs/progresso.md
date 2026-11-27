# VKeeper — Registro de Progresso

> Documento oficial de acompanhamento de progresso conforme `docs/PROCESS.md`.

---

## Status geral

- **Fase atual:** Fase 4 concluída e aprovada na 1ª rodada de revisão técnica independente — pronto para iniciar a **Fase 5 (Casos de Uso e MVC de Notas e Lixeira)**.
- **Repositório remoto:** `origin` configurado em `https://github.com/Vanderluizsj/VanderKeep.git` (Fases 0, 1, 2 e 3 publicadas; Fase 4 pronta para push).

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
- **Aplicação (`VKeeper.Aplicacao/Categorias/`):** `ICategoryService`, `CategoryService`, `CreateCategoryRequest`/`Validator`, `UpdateCategoryRequest`/`Validator`, `CategoryListItemDto` e extensão `GetSummaryByIdAsync` (`AsNoTracking`) em `ICategoryRepository`/`CategoryRepository`.
- **Web (`VKeeper.Web`):** `CategoriesController` (`[Authorize]`, PRG + `TempData`, zero acesso a `VKeeperDbContext`), ViewModels dedicados (`CategoryList`, `CategoryItem`, `CreateCategory`, `EditCategory`, `DeleteCategory`), views Bootstrap 5 em `pt-BR` (`Index` com estado vazio e contagem de notas não excluídas, `Create`, `Edit`, `Delete` com aviso explícito de preservação das notas) e link de navegação na navbar autenticada.

---

## Testes executados

### Fase 4 (2026-11-27)
- `dotnet build VKeeper.sln --no-restore`: **Êxito (0 avisos, 0 erros)**.
- `VKeeper.TestesUnitarios`: **60/60 aprovados** (incluindo `14` novos casos em `CategoryServiceTests`).
- `VKeeper.TestesIntegracao`: **40/40 aprovados** contra o banco real **`VKeeperDb_Test`** (incluindo `7` novos casos em `CategoriesIntegrationTests`: acesso anônimo, CRUD alfabético + renomeação de caixa, unicidade por usuário vs. usuários distintos, contagem ignorando lixeira + exclusão preservando notas, anti-IDOR completo Usuário B -> Usuário A e CSRF).
- **Total consolidado:** **100/100 testes aprovados**.

---

## Revisões realizadas

### Fase 1 (2026-11-26)
- Rodada 1: Reprovado -> Rodada 2: **Aprovado**.

### Fase 2 (2026-11-27)
- **Aprovado com ressalvas** -> todos os 5 refinamentos aplicados e migration `InitialCreate` aplicada em `VKeeperDb`.

### Fase 3 (2026-11-27)
- Rodada 1: Aprovado com ressalvas (3 Médios, 4 Baixos) -> Rodada 2: **Aprovado**.

### Fase 4 — Auditoria independente pelo Reviewer (2026-11-27)
- **Rodada 1:** **Aprovado** (0 Críticos, 0 Altos, 0 Médios e 3 melhorias opcionais de gravidade Baixa anotadas para a Fase 6 — Hardening: uniformizar `Guid.Empty` no `POST /Categories/Edit`, tradução opcional de violação concorrente de índice único em `UnitOfWork`/prevenção de double-submit, e ampliação de testes de borda).

---

## Pendências conhecidas

1. **Melhorias opcionais de hardening para a Fase 6 (apontadas na revisão da Fase 4):** uniformizar `id == Guid.Empty` em `POST /Categories/Edit`, tratamento de concorrência TOCTOU em índice único / double-submit e casos complementares de teste de borda.
2. **Hospedagem e deploy em nuvem (`ADR-15`):** decisão adiada deliberadamente para pós-MVP.

---

## Próximas atividades

1. Publicar o commit da Fase 4 no GitHub (`git push origin main`).
2. Iniciar a **Fase 5 — Casos de Uso e MVC de Notas e Lixeira** (`NoteService`, `NotesController`, `TrashController`, relacionamento N:N com validação de ownership de categorias, Pin, Arquivamento, Lixeira/Restauração/Exclusão permanente/Esvaziar lixeira, busca textual, filtros combinados, ordenação, paginação de 12 itens e testes unitários + integração anti-IDOR).
