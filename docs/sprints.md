# Registro de Sprints

Histórico das Sprints do projeto. Cada Sprint registra objetivo, escopo, resultado e
aprendizados, servindo de base para o capítulo de metodologia do TCC.

## Calendário

Sprints de **7 dias**, com entrega aos **sábados**.

| Sprint | Entrega | Situação |
|--------|---------|----------|
| Sprint 1 | sábado, 12/09/2026 | Concluída, aguardando Review |
| Sprint 2 | sábado, 19/09/2026 | A planejar |
| Sprint 3 | sábado, 26/09/2026 | A planejar |

As datas seguintes seguem a mesma cadência semanal.

---

# Sprint 1

## Sprint Goal

> Disponibilizar a primeira versão funcional do sistema, permitindo acesso autenticado e
> cadastro, consulta e manutenção básica dos residentes da instituição.

## Período

| | |
|---|---|
| Entrega e Sprint Review | sábado, **12/09/2026** |
| Desenvolvimento versionado | 07/09/2026 |

A data de início formal da Sprint não foi registrada no início dos trabalhos. O que o
repositório comprova é que todo o código versionado foi produzido em 07/09/2026.

## Escopo

Sete User Stories: US01 a US07 (ver `product-backlog.md`).

### Explicitamente fora do escopo

Medicamentos, estoque, estoque da cozinha, agenda, ocorrências, atividades, cadastro de
funcionários, relatórios e consulta ao histórico de auditoria.

A tela de perfil do residente exibe seções de Medicamentos, Agenda e Ocorrências
**apenas como espaços reservados**, identificados como módulos previstos para uma
próxima Sprint. Nenhuma delas apresenta dado ou funcionalidade.

## Definition of Done

Critérios acordados e sua verificação nesta Sprint:

| # | Critério | Situação |
|---|----------|----------|
| 1 | Implementada | Atendido — US01 a US07 |
| 2 | Compila sem erros | Atendido — build sem erros e sem warnings |
| 3 | Integrada ao banco de dados | Atendido — executada contra SQL Server 2022 |
| 4 | Possui as validações necessárias | Atendido — RN01 a RN06 |
| 5 | Possui tratamento básico de erros | Atendido — mensagens ao usuário, páginas de erro, log |
| 6 | Visualmente utilizável | Atendido — verificado em navegador, 1440px e 390px |
| 7 | Testes relacionados passando | Atendido — 48 testes, todos passando |
| 8 | Não quebra funcionalidades existentes | Atendido — suíte completa executada a cada etapa |
| 9 | Código desnecessário removido | Atendido — arquivos do template e pacotes redundantes removidos |
| 10 | Documentação relevante atualizada | Atendido — `docs/`, `README.md` e `CLAUDE.md` |

## Resultado

### Entregue

- **US01** Autenticação: login, logout, proteção de rotas, bloqueio por tentativas
- **US02** Cadastro de residente com validações
- **US03** Listagem com pesquisa por nome e estados vazios tratados
- **US04** Perfil do residente, preparado para receber os módulos futuros
- **US05** Edição com as mesmas validações do cadastro
- **US06** Inativação sem exclusão física, com reativação
- **US07** Dashboard com contagens reais

### Testes

| Categoria | Quantidade |
|-----------|-----------|
| Unitários (regras de domínio) | 19 |
| Integração (autenticação) | 13 |
| Integração (residentes) | 14 |
| Integração (dashboard) | 2 |
| **Total** | **48** |

Todos passando. A suíte foi executada três vezes seguidas para confirmar que não há
dependência de ordem entre os testes.

## Defeitos encontrados e corrigidos durante a Sprint

Registrados por serem material relevante para a discussão de qualidade no TCC. Os quatro
primeiros só apareceram ao **executar** a aplicação; não seriam percebidos apenas lendo
o código ou compilando.

| # | Defeito | Causa | Correção |
|---|---------|-------|----------|
| 1 | Tela de login exibida sem estilo e sem JavaScript | A política de autorização padrão se aplica também aos endpoints de arquivos estáticos criados por `MapStaticAssets` | `app.MapStaticAssets().AllowAnonymous()`, com teste de regressão |
| 2 | Barra lateral sem cor de fundo, com texto branco sobre branco | O Bootstrap impõe `background-color: transparent !important` ao `offcanvas` acima do breakpoint | Cor aplicada a um painel interno próprio, sem recorrer a `!important` |
| 3 | Barra lateral encolhia em telas com conteúdo largo | Item flex com `flex-shrink` padrão | `flex: 0 0 260px` |
| 4 | Validação do formulário de login inoperante | O layout de login não carregava o jQuery exigido pelos scripts de validação | jQuery incluído antes dos scripts de validação |
| 5 | **Possível gravação sobre o cadastro de outro residente** | No ASP.NET Core, o provedor de valores de formulário tem precedência sobre o de rota: o parâmetro `id` era preenchido pelo campo oculto enviado pelo navegador | `[FromRoute]` nas ações, com teste de regressão |

O defeito 5 é o mais relevante: era uma falha de segurança real, encontrada por um teste
de integração escrito justamente para verificar essa hipótese.

## Sprint Review

O roteiro completo da demonstração, as perguntas dirigidas à instituição e o formulário
de registro de feedback estão em [`sprint-1-review.md`](sprint-1-review.md).

Resultado da reunião: *a preencher após a Review.*

## Aprendizados

- **Executar o sistema é indispensável.** Quatro dos cinco defeitos passaram pela
  compilação sem erro algum. Só apareceram com a aplicação em execução no navegador.
- **Teste escrito para verificar uma hipótese encontra defeito real.** O teste de
  vinculação do identificador foi escrito para confirmar um comportamento presumido; ele
  revelou que a presunção estava errada e que havia uma falha de segurança.
- **O padrão do framework nem sempre é o esperado.** A precedência do formulário sobre a
  rota na vinculação de parâmetros é documentada, mas contraintuitiva.

## Próxima Sprint

Sugestão a validar no planejamento: **US08 — cadastro de usuários e perfis de acesso**.

É pré-requisito das demais: sem perfis definidos, não há como restringir quem registra a
administração de medicamentos, que é o requisito central do projeto. Depende de definir
com a instituição quais funções existem (item IT01 do backlog).
