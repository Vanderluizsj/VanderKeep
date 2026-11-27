# VKeeper — Planejamento de Produto

> **Status do documento:** Aprovado (Fase 0 concluída)  
> **Idioma da interface:** Português Brasileiro (`pt-BR`)  
> **Escopo atual:** Definição funcional consolidada do MVP e visão de evolução

---

## 1. Visão geral e objetivos

O **VKeeper** é uma aplicação web de gerenciamento de anotações pessoais construída com o ecossistema Microsoft (.NET, ASP.NET Core MVC, Entity Framework Core e SQL Server).

O produto tem dois objetivos complementares:

1. **Objetivo funcional:** oferecer um ambiente simples, rápido, organizado e seguro para que usuários criem, classifiquem, busquem, priorizem, fixem, arquivem e gerenciem o descarte seguro (lixeira com restauração) de suas anotações pessoais, associando-as livremente a múltiplas categorias.
2. **Objetivo de engenharia (portfólio profissional):** demonstrar domínio sólido de arquitetura limpa (*Clean Architecture*), modelagem relacional (incluindo relacionamento N:N e exclusão lógica), segurança aplicada (autenticação, proteção CSRF e isolamento de dados por usuário), renderização no servidor com ASP.NET Core MVC e testes automatizados com xUnit.

---

## 2. Público-alvo

- **Usuários individuais autenticados** que desejam centralizar anotações pessoais (estudos, trabalho, ideias, lembretes de texto, checklists simples) com organização flexível por categorias e priorização visual.
- Cada usuário possui sua própria conta e visualiza/gerencia exclusivamente seus próprios dados (modelo multiusuário com isolamento estrito por conta).

---

## 3. Escopo funcional do MVP

O MVP (*Minimum Viable Product*) contempla os seguintes blocos funcionais:

1. **Autenticação e gestão básica de conta**
   - Cadastro de novo usuário (registro).
   - Login e logout com sessão segura baseada em cookies.
   - Alteração de senha pelo usuário autenticado.
2. **Gerenciamento de notas (ciclo de vida completo)**
   - Criar, visualizar detalhes e editar notas.
   - Atribuir prioridade (`Baixa`, `Média`, `Alta`).
   - Associar nenhuma, uma ou várias categorias pertencentes ao próprio usuário.
   - Fixar ou desafixar notas importantes no topo (*Pin*).
   - Arquivar ou desarquivar notas (separando visão de notas ativas e arquivadas).
   - **Lixeira (*soft delete*):** enviar notas para a lixeira, listar notas na lixeira, restaurar notas para ativas e excluir permanentemente (individualmente ou esvaziando a lixeira).
3. **Gerenciamento de categorias**
   - Criar, listar, renomear (editar) e excluir categorias pessoais.
   - Visualizar a quantidade de notas não excluídas associadas a cada categoria.
4. **Busca, filtros combinados e ordenação**
   - Pesquisa textual por título e descrição.
   - Filtragem por categoria e por nível de prioridade.
   - Navegação clara entre **Notas Ativas**, **Arquivadas** e **Lixeira**.
   - Ordenação da listagem (mantendo notas fixadas em destaque no topo na visão ativa) e paginação (12 itens por página).

---

## 4. Requisitos funcionais

### 4.1. Contas e autenticação

- **RF01 — Cadastro de usuário:** o sistema deve permitir que um visitante crie uma conta informando nome de exibição, e-mail e senha (com confirmação de senha).
- **RF02 — Login:** o sistema deve autenticar usuários cadastrados via e-mail e senha, oferecendo opção de manter sessão ativa (*Lembrar-me*).
- **RF03 — Logout:** o sistema deve permitir encerrar a sessão autenticada de forma segura (via requisição `POST` protegida contra CSRF).
- **RF04 — Alteração de senha:** o usuário autenticado deve poder alterar sua senha informando a senha atual e a nova senha com confirmação.
- **RF05 — Controle de acesso:** todas as funcionalidades de notas, categorias, lixeira e conta devem exigir usuário autenticado; acessos anônimos devem redirecionar para a tela de login preservando `ReturnUrl` seguro (somente URLs locais).

### 4.2. Notas

- **RF06 — Criação de nota:** o usuário autenticado deve poder criar uma nota informando título, descrição (opcional), prioridade (padrão `Média`) e zero, uma ou várias categorias próprias.
- **RF07 — Listagem de notas ativas:** o sistema deve exibir as notas ativas (`!IsDeleted && !IsArchived`) do usuário autenticado, apresentando título, resumo da descrição, prioridade (com destaque visual), categorias associadas, indicador de fixação e data da última atualização.
- **RF08 — Visualização de detalhes:** o usuário deve poder abrir a visualização completa de uma nota própria (ativa ou arquivada) com todo o seu conteúdo, metadados e ações disponíveis.
- **RF09 — Edição de nota:** o usuário deve poder alterar título, descrição, prioridade e o conjunto de categorias associadas de uma nota própria que não esteja na lixeira.
- **RF10 — Fixar/Desafixar nota (Pin):** o usuário deve poder fixar ou desafixar uma nota ativa para destacá-la no topo da listagem.
- **RF11 — Arquivar/Desarquivar nota:** o usuário deve poder arquivar uma nota ativa (retirando-a da listagem principal) e desarquivar uma nota arquivada (retornando-a para as notas ativas).
- **RF12 — Listagem de notas arquivadas:** o sistema deve disponibilizar uma visão dedicada às notas arquivadas (`!IsDeleted && IsArchived`) do usuário, permitindo visualização, desarquivamento e envio para a lixeira.

### 4.3. Lixeira (Exclusão lógica e restauração)

- **RF13 — Enviar nota para a lixeira (*soft delete*):** o usuário deve poder mover uma nota ativa ou arquivada para a lixeira sem perdê-la imediatamente.
- **RF14 — Listagem da lixeira:** o sistema deve disponibilizar uma área dedicada exibindo as notas excluídas logicamente (`IsDeleted == true`) do usuário, ordenadas pela data de envio para a lixeira (mais recentes primeiro), com data de exclusão e categorias ainda associadas.
- **RF15 — Restaurar nota:** o usuário deve poder restaurar uma nota da lixeira, retornando-a para a visão de **Notas Ativas**.
- **RF16 — Excluir nota permanentemente:** o usuário deve poder excluir definitivamente uma nota que está na lixeira mediante confirmação explícita, removendo a nota e seus vínculos com categorias do banco de dados.
- **RF17 — Esvaziar lixeira:** o usuário deve poder excluir permanentemente todas as notas presentes em sua própria lixeira mediante confirmação explícita.

### 4.4. Categorias

- **RF18 — Criação de categoria:** o usuário autenticado deve poder criar categorias pessoais informando um título.
- **RF19 — Listagem de categorias:** o sistema deve listar as categorias do usuário em ordem alfabética, indicando quantas notas não excluídas (`!IsDeleted`) estão vinculadas a cada uma.
- **RF20 — Edição de categoria:** o usuário deve poder renomear uma categoria própria.
- **RF21 — Exclusão de categoria:** o usuário deve poder excluir definitivamente uma categoria própria mediante confirmação explícita, removendo apenas o vínculo com as notas existentes.

### 4.5. Busca, filtragem, ordenação e paginação

- **RF22 — Busca textual:** o usuário deve poder filtrar notas por termo de busca presente no título ou na descrição (*case-insensitive*).
- **RF23 — Filtros combinados:** o usuário deve poder combinar busca textual com filtro por categoria e filtro por prioridade, tanto na visão ativa quanto na visão arquivada.
- **RF24 — Ordenação:** o usuário deve poder ordenar notas por última atualização (padrão), data de criação, prioridade ou título, mantendo notas fixadas no topo na visão de notas ativas.
- **RF25 — Paginação:** a listagem de notas (ativas, arquivadas e lixeira) deve ser paginada (padrão de **12 itens por página**) preservando os parâmetros de busca, filtros e ordenação ativos.

---

## 5. Requisitos não funcionais

- **RNF01 — Isolamento de dados e segurança (anti-IDOR):** nenhuma consulta, visualização ou operação de escrita (incluindo lixeira, restauração e exclusão permanente) pode expor ou alterar notas ou categorias pertencentes a outro usuário.
- **RNF02 — Proteção web padrão:** proteção obrigatória contra CSRF em formulários e ações mutáveis, proteção contra XSS via encoding padrão do Razor, cookies `HttpOnly` + `Secure` + `SameSite=Lax`, redirecionamento HTTPS e HSTS em produção.
- **RNF03 — Proteção de credenciais:** senhas armazenadas exclusivamente com hash seguro via ASP.NET Core Identity (PBKDF2) e política de bloqueio temporário (*lockout*) após tentativas inválidas consecutivas.
- **RNF04 — Arquitetura e manutenibilidade:** adesão estrita à Clean Architecture definida em `docs/decisoes-tecnicas.md`, com renderização server-side em ASP.NET Core MVC (padrão PRG — *Post/Redirect/Get*).
- **RNF05 — Usabilidade e responsividade:** interface limpa, responsiva e acessível em português brasileiro utilizando Bootstrap 5, com feedback claro de sucesso, erro e validação.
- **RNF06 — Desempenho:** consultas de leitura otimizadas com paginação no banco de dados, `AsNoTracking` e índices adequados para filtros por usuário, estado (`IsDeleted`, `IsArchived`, `IsPinned`) e ordenação.
- **RNF07 — Testabilidade:** regras de negócio, serviços de aplicação e fluxos críticos (incluindo ciclo de vida completo da nota, autorização e isolamento de dados) devem possuir cobertura automatizada com xUnit.
- **RNF08 — Observabilidade:** registro estruturado de erros e eventos relevantes com `ILogger<T>`, sem registrar senhas, tokens ou dados sensíveis.

---

## 6. Regras de negócio

- **RN01 — Propriedade exclusiva:** toda nota e toda categoria pertencem obrigatoriamente a um único usuário (`UserId`). Tentativas de acesso a recursos de terceiros devem resultar em resposta de recurso não encontrado (`404`) ou acesso negado (`403`) sem vazar informações sobre a existência do registro.
- **RN02 — Relacionamento N:N entre Notas e Categorias:** uma nota pode estar associada a nenhuma, uma ou várias categorias; uma categoria pode estar associada a nenhuma, uma ou várias notas. Não pode haver associação duplicada entre a mesma nota e a mesma categoria.
- **RN03 — Integridade de propriedade na associação:** uma nota só pode ser associada a categorias pertencentes ao mesmo usuário proprietário da nota. Categorias inexistentes ou de outros usuários devem ser rejeitadas.
- **RN04 — Prioridade fixa:** toda nota possui exatamente uma prioridade pertencente ao conjunto fixo: `Baixa` (`Low`), `Média` (`Medium`) ou `Alta` (`High`), com valor padrão **`Média`** na criação.
- **RN05 — Unicidade de categoria por usuário:** o título de uma categoria deve ser único por usuário, ignorando diferenças de maiúsculas/minúsculas e espaços extras no início e no fim (*trim* + normalização). Usuários distintos podem criar categorias com o mesmo título.
- **RN06 — Exclusão não destrutiva de categoria:** categorias não possuem lixeira; ao excluir uma categoria, somente os vínculos na relação N:N com as notas são removidos, preservando intactas todas as notas (ativas, arquivadas ou na lixeira).
- **RN07 — Ciclo de vida e estados da nota (Ativa, Arquivada e Lixeira):**
  - **Ativa:** `IsDeleted = false`, `IsArchived = false`, `IsPinned = true | false`.
  - **Arquivada:** `IsDeleted = false`, `IsArchived = true`, `IsPinned = false`.
  - **Na Lixeira:** `IsDeleted = true`, `DeletedAtUtc != null`, `IsArchived = false`, `IsPinned = false`.
  - Ao **arquivar** uma nota fixada, o indicador de fixação é automaticamente removido (`IsPinned = false`). Não é permitido fixar uma nota arquivada.
  - Ao **enviar para a lixeira** uma nota (ativa ou arquivada), `IsDeleted = true`, `DeletedAtUtc = DateTime.UtcNow`, `IsPinned = false` e `IsArchived = false`.
  - Notas na lixeira **não podem ser editadas, fixadas ou arquivadas** diretamente; precisam ser restauradas primeiro.
  - Ao **restaurar** uma nota da lixeira, ela retorna sempre ao estado **Ativa** (`IsDeleted = false`, `DeletedAtUtc = null`, `IsArchived = false`, `IsPinned = false`), preservando suas associações com categorias ainda existentes.
  - A **exclusão permanente** só é permitida para notas que já estejam na lixeira (ou via ação explícita "Esvaziar lixeira"), removendo definitivamente a nota e seus vínculos N:N do banco de dados.
- **RN08 — Contagem de notas em categorias:** a contagem exibida na listagem de categorias considera exclusivamente notas não excluídas (`!IsDeleted`).
- **RN09 — Limites e obrigatoriedade de campos (Aprovados):**
  - **Nota — Título:** obrigatório, mínimo 1 caractere efetivo (após `Trim`), máximo 150 caracteres.
  - **Nota — Descrição:** opcional, máximo 5.000 caracteres.
  - **Categoria — Título:** obrigatório, mínimo 1 caractere efetivo (após `Trim`), máximo 50 caracteres.
  - **Usuário — Nome de exibição:** obrigatório, máximo 100 caracteres.
- **RN10 — Timestamps auditáveis:** notas e categorias registram automaticamente data/hora de criação (`CreatedAtUtc`) e atualização (`UpdatedAtUtc`) em UTC, além de `DeletedAtUtc` para notas na lixeira.

---

## 7. Histórias de usuário

### Autenticação e conta
- **US01:** Como visitante, quero me cadastrar com nome, e-mail e senha para ter meu espaço privado de anotações.
- **US02:** Como usuário cadastrado, quero fazer login e logout com segurança para acessar minhas notas sem expô-las a terceiros.
- **US03:** Como usuário autenticado, quero alterar minha senha para manter minha conta segura.

### Notas
- **US04:** Como usuário, quero criar uma nota com título, descrição opcional, prioridade e zero ou mais categorias para registrar informações importantes rapidamente.
- **US05:** Como usuário, quero visualizar minhas notas ativas com indicação clara de prioridade e categorias para encontrar facilmente o que preciso.
- **US06:** Como usuário, quero editar o conteúdo, a prioridade e as categorias de uma nota para mantê-la sempre atualizada.
- **US07:** Como usuário, quero fixar e desafixar notas importantes para mantê-las sempre no topo da lista.
- **US08:** Como usuário, quero arquivar notas concluídas ou antigas e consultá-las em uma área separada (podendo desarquivá-las) para manter minha lista principal limpa sem perder o histórico.

### Lixeira
- **US09:** Como usuário, quero enviar notas indesejadas para a lixeira em vez de perdê-las imediatamente para me proteger contra exclusões acidentais.
- **US10:** Como usuário, quero visualizar as notas na lixeira, restaurar as que ainda preciso ou excluí-las permanentemente (uma a uma ou esvaziando a lixeira).

### Categorias
- **US11:** Como usuário, quero criar, listar e renomear categorias pessoais para organizar minhas notas por assunto.
- **US12:** Como usuário, quero excluir uma categoria que não uso mais sem perder as notas vinculadas a ela.

### Busca e organização
- **US13:** Como usuário, quero buscar notas por texto e filtrar por categoria e prioridade de forma combinada, além de ordenar e paginar os resultados, para localizar qualquer anotação em segundos.

---

## 8. Critérios de aceite

| Referência | Critérios de aceite objetivos |
| --- | --- |
| **US01 / US02 / US03** | 1. Não permite cadastro com e-mail duplicado ou senha fora da política.<br>2. Login inválido exibe mensagem genérica (sem informar se o e-mail existe).<br>3. Usuário não autenticado é redirecionado para `/Account/Login`.<br>4. Logout exige `POST` com token anti-CSRF e invalida o cookie.<br>5. Alteração de senha valida a senha atual antes de aplicar a nova. |
| **US04 / US06** | 1. Nota pode ser salva sem descrição e com zero, uma ou várias categorias do usuário.<br>2. Tentar associar `CategoryId` inexistente ou de outro usuário falha de forma segura.<br>3. Não permite editar nota que esteja na lixeira.<br>4. Campos fora dos limites retornam erro de validação na própria tela preservando os dados preenchidos.<br>5. Após criar/editar com sucesso, redireciona (PRG) com mensagem de confirmação. |
| **US05 / US13** | 1. Listagem exibe apenas notas ativas do usuário autenticado (nunca arquivadas nem na lixeira).<br>2. Notas fixadas aparecem sempre antes das não fixadas na visão ativa.<br>3. Busca textual encontra correspondências parciais em título ou descrição.<br>4. Filtros (busca + categoria + prioridade + ordenação) funcionam em conjunto e permanecem ativos na paginação (12 itens/página). |
| **US07 / US08** | 1. Fixar/desafixar atualiza imediatamente a posição da nota na listagem ativa.<br>2. Arquivar move a nota para "Arquivadas" e define `IsPinned = false`.<br>3. Não é possível fixar uma nota arquivada ou na lixeira.<br>4. Desarquivar retorna a nota para "Notas Ativas". |
| **US09 / US10** | 1. Enviar para a lixeira exige `POST` anti-CSRF, define `IsDeleted = true`, `DeletedAtUtc`, `IsPinned = false`, `IsArchived = false` e retira a nota de Ativas/Arquivadas.<br>2. Restaurar retorna a nota para Ativas preservando suas categorias.<br>3. Excluir permanentemente e Esvaziar lixeira exigem confirmação explícita + `POST` anti-CSRF e removem definitivamente os registros do banco.<br>4. Usuário não consegue enviar para a lixeira, restaurar ou excluir permanentemente nota de outro usuário. |
| **US11 / US12** | 1. Não permite criar ou renomear categoria para um título já existente para o mesmo usuário (ignorando maiúsculas/minúsculas e espaços nas pontas).<br>2. Dois usuários diferentes podem possuir categorias com o mesmo título.<br>3. Contagem de notas da categoria ignora notas na lixeira.<br>4. Excluir categoria remove a categoria e os vínculos N:N, mantendo todas as notas intactas. |

---

## 9. Fluxos principais

### 9.1. Cadastro e primeiro acesso
1. Visitante acessa a aplicação e é direcionado para a tela de login (ou página inicial pública com CTA para login/cadastro).
2. Visitante acessa **Criar conta**, preenche nome, e-mail, senha e confirmação.
3. Sistema valida os dados, cria a conta via Identity, autentica o usuário e redireciona para a listagem de notas ativas (estado vazio amigável com atalho para criar a primeira nota e categorias).

### 9.2. Criação e edição de nota com múltiplas categorias
1. Usuário clica em **Nova Nota** (ou **Editar** em uma nota não excluída).
2. Sistema carrega o formulário com título, descrição (opcional), seletor de prioridade (padrão `Média`) e seleção múltipla (checkboxes) das categorias do usuário.
3. Usuário preenche os dados e submete (`POST` com anti-CSRF).
4. Sistema valida entrada, valida propriedade das categorias selecionadas, persiste a nota e seus vínculos N:N, grava feedback em `TempData` e redireciona (PRG) para a listagem ou detalhes.

### 9.3. Busca, filtragem, fixação e arquivamento
1. Na tela de Notas Ativas (ou Arquivadas), usuário informa termo de busca, escolhe categoria, prioridade e critério de ordenação (`GET` com query string).
2. Sistema consulta o banco filtrando por `UserId`, estado (`IsDeleted = false`, `IsArchived`) e filtros ativos, aplica paginação (12 itens) e renderiza a lista preservando os filtros nos controles e links de paginação.
3. Usuário aciona **Fixar/Desafixar**, **Arquivar/Desarquivar** ou **Mover para Lixeira** via `POST` com anti-CSRF e redirecionamento PRG.

### 9.4. Lixeira (restauração e exclusão definitiva)
1. Usuário acessa **Lixeira** pelo menu principal.
2. Sistema lista apenas as notas excluídas logicamente (`IsDeleted = true`) do usuário, com data de exclusão e opções de **Restaurar** ou **Excluir permanentemente**, além do botão **Esvaziar lixeira** (quando houver itens).
3. Ao restaurar, a nota volta imediatamente para **Notas Ativas**. Ao excluir permanentemente (ou esvaziar), mediante confirmação explícita, os registros são removidos definitivamente do banco.

### 9.5. Gerenciamento de categorias
1. Usuário acessa **Categorias** pelo menu principal.
2. Sistema lista todas as categorias do usuário em ordem alfabética com contagem de notas não excluídas.
3. Usuário cria nova categoria, renomeia uma existente ou aciona exclusão (exibindo confirmação clara de que as notas vinculadas permanecerão intactas).

---

## 10. Funcionalidades futuras (pós-MVP)

Itens intencionalmente postergados para evoluções futuras, após estabilização do MVP:

- Limpeza automática programada de notas antigas na lixeira (ex.: após 30 dias via background job).
- Suporte a formatação rica / Markdown na descrição da nota (com sanitização segura de HTML).
- Cores ou ícones personalizáveis nas categorias.
- Confirmação de e-mail e recuperação de senha via envio de e-mail (SMTP / provedor transacional).
- Endpoints REST/JSON complementares no próprio `VKeeper.Web` para ações instantâneas sem recarregamento ou integrações futuras.
- Compartilhamento de notas entre usuários.
- Exportação de notas (ex.: JSON, Markdown ou PDF).
- Exclusão de conta pelo próprio usuário (LGPD / autoatendimento).

---

## 11. Itens fora do escopo

Não fazem parte do escopo do projeto neste momento:

- Frontend SPA separado (React, Angular, Vue).
- API backend independente ou arquitetura de microsserviços.
- Edição colaborativa em tempo real.
- Upload/armazenamento de anexos binários (imagens, arquivos PDF, áudios).
- Lembretes agendados, calendário ou notificações push/e-mail.
- Autenticação social externa (Google, Microsoft, GitHub OAuth) no MVP.

---

## 12. Pendências de produto

*Todas as pendências de produto da Fase 0 foram resolvidas e consolidadas (descrição opcional até 5.000 caracteres, prioridade padrão `Média`, remoção de `Pin` ao arquivar, paginação de 12 itens por página e inclusão da Lixeira com soft delete e restauração no MVP).*
