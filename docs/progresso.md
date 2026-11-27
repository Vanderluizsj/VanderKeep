# VKeeper — Registro de Progresso

> Documento oficial de acompanhamento de progresso conforme `docs/PROCESS.md`.

---

## Status geral

- **Fase atual:** Fase 5 concluída e aprovada em revisão independente — todo o núcleo funcional do MVP está pronto! Próxima etapa: **Fase 6 (Polimento de UX, Hardening, Workflow CI no GitHub Actions e Fechamento do MVP)**.
- **Repositório remoto:** `origin` configurado em `https://github.com/Vanderluizsj/VanderKeep.git` (Fases 0 a 4 publicadas; Fase 5 pronta para push).

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
- **Aplicação (`VKeeper.Aplicacao/Notas/`):** `INoteService`, `NoteService`, feature folders (`CriarNota`, `AtualizarNota`, `ListarNotas`, `ObterNota`, `Lixeira`), validação de ownership N:N (`RN03`), máquina de estados Ativa/Arquivada/Lixeira (`RN07`) e normalização de página efetiva (`page > TotalPages`) em `NoteRepository` e `NoteService`.
- **Web (`VKeeper.Web`):** `NotesController`, `TrashController`, `NotePriorityDisplay`, ViewModels dedicados com `[EnumDataType]`, Razor Views Bootstrap 5 em `pt-BR` (`_NotesList`, `Index`, `Archived`, `Details`, `Create`, `Edit`, `Trash/Index`, `Trash/Delete` com metadados, `Trash/Empty`, paginação acessível com `<span aria-disabled="true">`), redirecionamento autenticado direto para `/Notes` em `HomeController` e `AccountController`, landing clara para visitantes (**"Entrar"** / **"Criar minha conta"**) e navbar completa (**Notas**, **Arquivadas**, **Lixeira**, **Categorias**).

---

## Testes executados

### Fase 5 (2026-11-27)
- `dotnet build VKeeper.sln --no-restore`: **Êxito (0 avisos, 0 erros)**.
- `VKeeper.TestesUnitarios`: **68/68 aprovados** (incluindo `NoteServiceTests`).
- `VKeeper.TestesIntegracao`: **55/55 aprovados** contra o banco real **`VKeeperDb_Test`** (incluindo `NotesAndTrashIntegrationTests` cobrindo acesso anônimo, redirecionamento autenticado `/` -> `/Notes`, CRUD + N:N com 0 e múltiplas categorias, `RN03` em `Create` e `Edit`, Pin/Arquivar/Desarquivar, busca textual + filtros combinados + ordenação + paginação de 12 itens com clamp de página fora do limite, ciclo completo da Lixeira, proteção contra Open Redirect em `NotesController`, CSRF e anti-IDOR completo entre Usuário A e Usuário B).
- **Total consolidado:** **123/123 testes aprovados**.

---

## Revisões realizadas

### Fase 1 (2026-11-26)
- Rodada 1: Reprovado -> Rodada 2: **Aprovado**.

### Fase 2 (2026-11-27)
- **Aprovado com ressalvas** -> refinamentos aplicados e migration `InitialCreate` aplicada em `VKeeperDb`.

### Fase 3 (2026-11-27)
- Rodada 1: Aprovado com ressalvas -> Rodada 2: **Aprovado**.

### Fase 4 (2026-11-27)
- Rodada 1: **Aprovado**.

### Fase 5 — Auditoria independente pelo Reviewer (2026-11-27)
- **Rodada 1:** Aprovado com ressalvas (0 Críticos, 0 Altos, 2 Médios: clamp de paginação `page > TotalPages` em Notas/Lixeira e ampliação de cobertura em `NotesAndTrashIntegrationTests` para `POST Edit` RN03/IDOR, `Unpin`/`Unarchive` IDOR e Open Redirect em `NotesController`; 2 Baixos: metadados em `Trash/Delete.cshtml`, `[EnumDataType]` nos ViewModels e acessibilidade da paginação).
- **Rodada 2 (pós-correções):** **Aprovado** — todos os apontamentos resolvidos sem achados remanescentes.

---

## Pendências conhecidas

1. **Itens da Fase 6 (Polimento de UX, Hardening e Workflow CI):**
   - Revisão de headers de segurança HTTP (`X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, etc.), uniformização de `id == Guid.Empty` em `POST /Categories/Edit`, tratamento opcional de concorrência TOCTOU em índice único de categorias.
   - Workflow CI em `.github/workflows/` com *service container* SQL Server executando build e todos os 123+ testes automaticamente.
   - `README.md` profissional de portfólio com visão arquitetural, screenshots/instruções de setup local e execução de testes.
2. **Hospedagem e deploy em nuvem (`ADR-15`):** decisão adiada deliberadamente para pós-MVP.

---

## Próximas atividades

1. Publicar o commit da Fase 5 no GitHub (`git push origin main`).
2. Iniciar a **Fase 6 — Polimento de UX, Hardening, Workflow CI e Fechamento do MVP**.
