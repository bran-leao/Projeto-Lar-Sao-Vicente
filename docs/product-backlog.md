# Product Backlog

Ordenado por prioridade. A ordem reflete a dor relatada pela instituição: o fluxo de
medicamentos é a principal necessidade, mas depende de cadastros básicos existirem antes.

Legenda: **Concluído** · **Em andamento** · **Pendente**

## Sprint 1 — Base do sistema · Concluído

| ID | User Story | Situação |
|----|------------|----------|
| US01 | Como funcionário autorizado, quero realizar login no sistema, para acessar somente as funcionalidades permitidas | Concluído |
| US02 | Como funcionário autorizado, quero cadastrar um residente, para manter suas informações básicas centralizadas | Concluído |
| US03 | Como funcionário autorizado, quero visualizar os residentes cadastrados, para localizar rapidamente uma pessoa atendida | Concluído |
| US04 | Como funcionário autorizado, quero abrir o perfil de um residente, para consultar suas informações | Concluído |
| US05 | Como funcionário autorizado, quero atualizar as informações de um residente, para manter o cadastro atualizado | Concluído |
| US06 | Como funcionário autorizado, quero inativar um residente, para que ele não apareça como ativo sem apagar seu histórico | Concluído |
| US07 | Como funcionário autorizado, quero visualizar um resumo do sistema após o login, para ter uma visão rápida da instituição | Concluído |

## Backlog priorizado — Sprints futuras

As estimativas em Sprints são preliminares e serão revistas no refinamento.

### Prioridade alta

| ID | User Story | Depende de | Estimativa |
|----|------------|-----------|------------|
| US08 | Como administrador, quero cadastrar usuários e definir seus perfis, para que cada funcionário acesse apenas o que lhe compete | US01 | 1 Sprint |
| US09 | Como funcionário autorizado, quero cadastrar medicamentos, para que possam ser controlados pelo sistema | US08 | 1 Sprint |
| US10 | Como funcionário autorizado, quero registrar a entrada de medicamentos, identificando a origem, inclusive doações, para controlar o que chega à instituição | US09 | 1 Sprint |
| US11 | Como funcionário autorizado, quero vincular medicamentos a um residente, para saber a quem cada medicamento se destina | US09 | 1 Sprint |
| US12 | Como profissional responsável, quero registrar a administração de um medicamento, para que fique documentado quem administrou, o quê e quando | US11 | 1 Sprint |
| US13 | Como funcionário autorizado, quero consultar o estoque de medicamentos, para saber o que há disponível e o que está por acabar | US10 | 1 Sprint |

> US09 a US13 compõem, juntas, o fluxo de rastreabilidade que é a principal dor
> relatada: entrada → armazenamento → identificação → vinculação ao residente →
> separação → administração → registro do responsável.

### Prioridade média

| ID | User Story | Depende de | Estimativa |
|----|------------|-----------|------------|
| US14 | Como funcionário autorizado, quero consultar o histórico completo de um medicamento, para rastrear todo o seu percurso na instituição | US12 | 1 Sprint |
| US15 | Como funcionário autorizado, quero registrar ocorrências sobre um residente, para documentar acontecimentos relevantes | US04 | meia Sprint |
| US16 | Como funcionário autorizado, quero consultar a agenda de um residente, para me organizar quanto a consultas e compromissos | US04 | 1 Sprint |
| US17 | Como funcionário autorizado, quero controlar o estoque geral da instituição, para reduzir faltas e desperdício | US08 | 1 Sprint |
| US18 | Como funcionário da cozinha, quero controlar o estoque de alimentos, para organizar as compras | US17 | meia Sprint |

### Prioridade baixa

| ID | User Story | Depende de | Estimativa |
|----|------------|-----------|------------|
| US19 | Como funcionário autorizado, quero registrar atividades realizadas com os residentes, para acompanhar a rotina da instituição | US04 | meia Sprint |
| US20 | Como administrador, quero cadastrar os funcionários da instituição, para manter as informações da equipe organizadas | US08 | meia Sprint |
| US21 | Como administrador, quero emitir relatórios e indicadores, para apoiar decisões e prestações de contas | US12, US17 | 1 Sprint |
| US22 | Como administrador, quero consultar o histórico de alterações dos cadastros, para saber quem alterou o quê e quando | US08 | meia Sprint |

## Itens técnicos

Não são funcionalidades para o usuário, mas precisam ser considerados no planejamento.

| ID | Item | Prioridade | Observação |
|----|------|-----------|------------|
| IT01 | Definir os perfis de acesso junto à instituição | Alta | Bloqueia US08; hoje existe apenas o perfil Administrador |
| IT02 | Definir a política de senha inicial e sua troca no primeiro acesso | Alta | Vinculado a US08 |
| IT03 | Rotina de backup do banco de dados | Alta | Dados de saúde exigem plano de recuperação |
| IT04 | Paginação da listagem de residentes | Média | Com 86 residentes a listagem atual é utilizável, mas crescerá |
| IT05 | Filtro por situação na listagem | Média | Sugerido a partir do uso; confirmar necessidade com a instituição |
| IT06 | Publicação em servidor da instituição | Média | Definir onde o sistema será hospedado |
| IT07 | Confirmar campo Quarto como obrigatório | Baixa | Ver `requisitos.md`, seção 6 |

## Itens levantados durante a Sprint 1

Registrados a partir do desenvolvimento e da execução do sistema, para discussão na
Review:

- **Filtro por situação na listagem** (IT05). Com residentes inativos misturados aos
  ativos, a equipe provavelmente vai querer filtrar. Não foi implementado porque a US03
  pede apenas pesquisa por nome.
- **Paginação** (IT04). A listagem carrega todos os residentes de uma vez. Adequado para
  86 registros, mas convém resolver antes que o histórico de inativos cresça.
- **Reativação de residente**. Implementada na Sprint 1 como contrapartida da inativação.
  Confirmar se deve ser restrita a determinado perfil.
