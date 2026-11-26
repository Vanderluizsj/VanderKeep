# Vanderkeep — Processo de Trabalho

## 1. Objetivo

Definir o fluxo de trabalho dos agentes de IA envolvidos no planejamento, desenvolvimento e manutenção do Vanderkeep.

O objetivo é garantir continuidade, rastreabilidade, qualidade e consistência entre as diferentes etapas do projeto.

## 2. Papéis e responsabilidades

### Architect

Responsável por:

- Analisar requisitos e necessidades.
- Propor soluções arquiteturais.
- Avaliar impactos técnicos.
- Documentar decisões.
- Definir ou revisar o roadmap.
- Orientar mudanças estruturais.

Não deve implementar funcionalidades quando sua atuação for exclusivamente arquitetural.

### Developer

Responsável por:

- Interpretar os requisitos aprovados.
- Planejar as alterações necessárias.
- Implementar funcionalidades.
- Criar e executar testes.
- Corrigir defeitos.
- Atualizar a documentação de progresso.

Não deve alterar decisões arquiteturais aprovadas sem avaliação prévia.

### Reviewer

Responsável por:

- Revisar implementações.
- Verificar critérios de aceite.
- Avaliar arquitetura e qualidade.
- Identificar defeitos e riscos.
- Emitir parecer técnico independente.

Não deve modificar código durante uma revisão sem autorização.

## 3. Fluxo de desenvolvimento

O desenvolvimento deverá seguir este fluxo:

### Etapa 1 — Planejamento

O Architect avalia requisitos, dependências e impactos técnicos.

Quando necessário, apresenta alternativas e solicita aprovação.

As decisões relevantes devem ser registradas na documentação.

### Etapa 2 — Preparação da tarefa

O Developer consulta os documentos oficiais e analisa a implementação existente.

Antes de alterações relevantes, apresenta um plano contendo:

- Objetivo.
- Escopo.
- Arquivos e camadas envolvidos.
- Dependências.
- Estratégia de testes.

### Etapa 3 — Implementação

O Developer executa somente o escopo aprovado.

Deve preservar funcionalidades existentes, seguir os padrões arquiteturais e executar as validações pertinentes.

### Etapa 4 — Revisão

O Reviewer analisa a implementação de maneira independente.

Os problemas encontrados devem ser apresentados com evidências e classificação de gravidade.

### Etapa 5 — Correções

Quando houver reprovação ou ressalvas relevantes, o Developer realiza os ajustes necessários.

O Reviewer pode executar uma nova revisão para verificar as correções.

### Etapa 6 — Conclusão

Após a validação:

- Registrar o resultado da tarefa.
- Atualizar o progresso do projeto.
- Atualizar a documentação quando houver mudanças relevantes.
- Identificar pendências conhecidas.

## 4. Gestão de decisões

Decisões arquiteturais devem ser registradas em `docs/decisoes-tecnicas.md` e requisitos de produto em `docs/planejamento-de-produto.md`.

Quando uma decisão aprovada precisar mudar:

1. Identificar o motivo.
2. Avaliar impactos.
3. Apresentar alternativas.
4. Obter aprovação quando houver impacto relevante.
5. Atualizar a documentação.

Não mantenha decisões antigas como se ainda estivessem vigentes.

## 5. Gestão de escopo

Cada tarefa deve possuir um objetivo definido e critérios de conclusão.

Os agentes não devem aproveitar solicitações para implementar melhorias não relacionadas.

Se identificarem oportunidades adicionais, devem apresentá-las separadamente.

## 6. Tratamento de dúvidas e conflitos

Quando houver requisitos incompletos, documentação contraditória ou decisões técnicas não definidas:

- Interromper a parte da execução que depende da informação.
- Identificar exatamente a dúvida.
- Apresentar alternativas quando possível.
- Solicitar esclarecimento antes de assumir uma decisão.

## 7. Registro de progresso

O progresso deverá ser atualizado em `docs/progresso.md`.

Registrar:

- Fases concluídas.
- Funcionalidades implementadas.
- Testes executados.
- Revisões realizadas.
- Pendências.
- Próximas atividades.

Não registrar como concluída uma atividade sem validação suficiente.

## 8. Princípio fundamental

A documentação aprovada é a referência para o desenvolvimento.

O objetivo não é apenas produzir código funcional, mas manter uma aplicação coerente, segura, sustentável e alinhada aos requisitos do Vanderkeep.