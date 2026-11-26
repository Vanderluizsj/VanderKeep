# Vanderkeep — Registro de Progresso

> Documento oficial de acompanhamento de progresso conforme `docs/PROCESS.md`.

---

## Status geral

- **Fase atual:** Fase 1 concluída e aprovada em revisão técnica independente — pronto para iniciar a **Fase 2 (Domínio e Persistência Core)**.
- **Publicação remota (GitHub):** commits realizados localmente na branch `main`; aguardando URL do repositório remoto (`origin`) para `git push`.

---

## Fases concluídas

### Fase 0 — Descoberta, Planejamento e Arquitetura (Concluída em 2026-11-26)
- Análise inicial do repositório concluída (confirmada ausência de documentos legados de referência em `docs/`, detectado SDK `.NET 10.0.115`).
- Entrevista interativa e refinamento arquitetural concluídos cobrindo produto, escopo do MVP (incluindo Pin, Arquivamento e Lixeira com soft delete e restauração), regras de negócio, segurança/Identity, Clean Architecture organizada por feature, convenção de pastas/namespaces (ADR-16), persistência SQL Server (container local + workflow CI futuro) e testes xUnit.
- Documentação oficial consolidada e aprovada: `docs/planejamento-de-produto.md`, `docs/decisoes-tecnicas.md`, `docs/progresso.md`.
- Referências padronizadas em `docs/PROCESS.md` e `.agents/agents/architect/agent.md`; seção 2 de `AGENTS.md` alinhada à **ADR-16**.

### Fase 1 — Fundação da Solução e Infraestrutura Base (Concluída em 2026-11-26)
- Criada solução `Vanderkeep.sln` em `.NET 10.0` (pacotes Microsoft fixados em `10.0.15`, FluentValidation `12.2.0`, xUnit `2.9.3`) com os 6 projetos respeitando a direção de dependências da Clean Architecture e a ADR-16:
  - `src/Vanderkeep.Dominio` (`BaseEntity` com UUID v7 via `Guid.CreateVersion7()`, `DomainException`, `IUnitOfWork`)
  - `src/Vanderkeep.Aplicacao` (`Result`, `Result<T>`, `ErrorType`, `PagedResult<T>`, `ICurrentUserService`, `IDateTimeProvider`, `DependencyInjection`)
  - `src/Vanderkeep.Infraestrutura` (`ApplicationUser : IdentityUser<Guid>`, `VanderkeepDbContext`, `UnitOfWork`, `SystemDateTimeProvider`, `DependencyInjection`)
  - `src/Vanderkeep.Web` (Composition Root em `Program.cs`, CSRF global, cookies seguros, `WebEncoderOptions` UTF-8 seguro contra XSS, `CurrentUserService`, `HomeController`, `ErrorController` com suporte a re-execução de qualquer verbo HTTP e validação de faixa `400..599`, layout base Bootstrap 5 em `pt-BR`, `_Alerts.cshtml` e views de erro `404`/`403`/`500`)
  - `tests/Vanderkeep.TestesUnitarios` e `tests/Vanderkeep.TestesIntegracao` (`VanderkeepWebApplicationFactory` com `https://localhost` e resolução `Env -> User Secrets` para `ConnectionStrings:TestConnection`)
- Inicializado `UserSecretsId` em `Vanderkeep.Web` e `Vanderkeep.TestesIntegracao`, mantendo `appsettings.json` sem credenciais.
- Criado `.gitignore` padrão `.NET` e inicializado versionamento local na branch `main`.

---

## Testes executados

### Fase 1 (2026-11-26)
- `dotnet build Vanderkeep.sln --no-restore`: **Êxito (0 avisos, 0 erros)**.
- `Vanderkeep.TestesUnitarios`: **13/13 aprovados** (`BaseEntityTests`, `ResultTests`, `PagedResultTests`).
- `Vanderkeep.TestesIntegracao`: **12/12 aprovados** (`SmokeTests` cobrindo `GET /`, `GET` rota inexistente `404`, `POST /` re-executando `405` sem falha CSRF, `POST /Error/404` sem falha CSRF, códigos fora da faixa `200`/`204`/`304`/`-1`/`999` normalizados para `404`, `403`, `500` sem vazamento de stack trace e resolução do container DI).

---

## Revisões realizadas

### Fase 1 — Auditoria independente pelo agente Reviewer (2026-11-26)
- **Rodada 1:** **Reprovado** (1 achado Alto: `ErrorController` restrito a `[HttpGet]` e sem `[IgnoreAntiforgeryToken]`; 3 achados Médios: validação de faixa `statusCode`, verificação de resolução DI e `BaseAddress = https://localhost` na factory de testes; 4 achados Baixos: limpeza de resíduos do template MVC, checagem defensiva em `CurrentUserService`, refinamento de log em `/Error` e deduplicação em `Result.cs`).
- **Rodada 2 (pós-correções):** **Aprovado** — todos os 8 apontamentos verificados e resolvidos sem novos achados.

---

## Pendências conhecidas

1. **Publicação no GitHub:** configurar o `remote origin` com a URL do repositório GitHub e executar `git push -u origin main` (`gh` CLI não está instalado no ambiente local).
2. **User Secrets da connection string local (antes de rodar migrations na Fase 2):** configurar `ConnectionStrings:DefaultConnection` (para `VanderkeepDb`) em `Vanderkeep.Web` e `ConnectionStrings:TestConnection` (para `VanderkeepDb_Test`) em `Vanderkeep.TestesIntegracao` apontando para o container Docker `sqlserver-backend`.
3. **Hospedagem e deploy em nuvem (`ADR-15`):** decisão adiada deliberadamente para pós-MVP.

---

## Próximas atividades

1. Conectar o repositório remoto no GitHub e publicar a branch `main`.
2. Iniciar a **Fase 2 — Domínio e Persistência Core (Notas, Categorias N:N e Soft Delete)** seguindo `docs/PROCESS.md`.
