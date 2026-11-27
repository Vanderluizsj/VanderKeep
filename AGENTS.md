# VKeeper — Regras Gerais dos Agentes

## 1. Identidade do projeto

O VKeeper é uma aplicação de gerenciamento de anotações pessoais, desenvolvida com C#, ASP.NET Core MVC e Clean Architecture.

A documentação existente em `docs/` representa a referência oficial dos requisitos, decisões técnicas e planejamento do projeto.

## 2. Idioma e nomenclatura

- Comunicação com o usuário: português brasileiro.
- Documentação: português brasileiro.
- Projetos, pastas e namespaces de código autoral: português brasileiro (conforme ADR-16 em `docs/decisoes-tecnicas.md`).
- Pastas e arquivos exigidos ou convencionados pelo framework (.NET / ASP.NET Core MVC): inglês (`Controllers`, `Views`, `Shared`, `ViewModels`, `wwwroot`, `Identity`, `Enums`, `Program.cs`, `appsettings.json`, etc.).
- Classes, métodos, propriedades, variáveis e comentários técnicos no código: inglês.
- Mensagens e textos apresentados na interface: português brasileiro.

Siga as convenções oficiais da comunidade .NET.

## 3. Regras arquiteturais

- Respeite a Clean Architecture aprovada.
- Preserve a direção correta das dependências.
- Não misture responsabilidades entre camadas.
- Utilize ASP.NET Core MVC com renderização no servidor.
- Não introduza uma API backend independente ou frontend separado sem aprovação.
- Evite complexidade desnecessária.
- Não altere decisões arquiteturais aprovadas unilateralmente.

## 4. Regras de desenvolvimento

- Analise o código existente antes de modificá-lo.
- Não invente requisitos ou regras de negócio.
- Não remova funcionalidades sem autorização.
- Não realize refatorações fora do escopo.
- Evite duplicação de código.
- Priorize soluções simples, seguras e testáveis.
- Não considere uma implementação concluída sem validação.
- Não esconda erros ou limitações encontrados.

## 5. Documentação oficial

Consulte os documentos pertinentes antes de executar qualquer tarefa:

- `docs/planejamento-de-produto.md`
- `docs/decisoes-tecnicas.md`
- `docs/PROCESS.md`

Caso existam divergências entre requisitos, decisões documentadas e implementação atual, identifique o conflito antes de tomar decisões.

Não altere decisões aprovadas sem registrar e justificar a necessidade.

## 6. Organização do trabalho

Siga o processo definido em `docs/PROCESS.md`.

Os agentes possuem responsabilidades distintas:

- Architect: planejamento, arquitetura e decisões técnicas.
- Developer: implementação e manutenção do código.
- Reviewer: revisão independente e validação técnica.

Nenhum agente deve assumir que uma etapa foi aprovada sem evidências ou autorização correspondente.

## 7. Segurança e qualidade

- Não exponha credenciais, senhas ou informações sensíveis.
- Não armazene segredos diretamente no código.
- Valide entradas do usuário.
- Considere autorização e isolamento de dados quando aplicáveis.
- Priorize testes automatizados.
- Não ignore vulnerabilidades identificadas.

## 8. Atualização documental

Mantenha a documentação consistente com a evolução do projeto.

Decisões técnicas relevantes devem ser registradas no documento técnico.

Atualize o progresso quando houver conclusão de funcionalidades, mudanças de escopo ou pendências relevantes.

Não declare como concluída uma funcionalidade que ainda não foi validada.