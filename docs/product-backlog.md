# Product Backlog

Ordenado por prioridade. A ordem reflete a dor relatada pela instituição: o fluxo de
medicamentos é a principal necessidade, mas depende de cadastros básicos existirem antes.

## Repriorização acordada após a Sprint 1

O plano inicial colocava o cadastro de usuários e perfis (US08) como Sprint 2. A ordem
foi revista: o **catálogo de medicamentos não depende de perfis de acesso** — é um
cadastro básico, como o de residentes — e antecipá-lo entrega mais cedo aquilo que a
instituição relatou como sua maior dor.

O que **não** pode ser antecipado é o vínculo entre medicamento e residente: essa
informação é **dado pessoal sensível de saúde** pela LGPD, e a partir dali o controle de
acesso por perfil deixa de ser desejável e passa a ser exigência legal. Por isso a US08
permanece obrigatória antes das US11 e US12.

| Sprint | Entrega | Escopo previsto |
|--------|---------|-----------------|
| 2 | 19/09/2026 | US09, US23, US24 — catálogo de medicamentos e leitura de código de barras |
| 3 | 26/09/2026 | US10, US26, US27, US25 — entrada com lote, validade e origem; registro manual sem código; conferência; alerta de vencimento |
| 4 | 03/10/2026 | US08 — usuários e perfis de acesso |
| 5 | 10/10/2026 | US11 — vínculo entre medicamento e residente |
| 6 | 17/10/2026 | US12 — registro da administração, com responsável |
| 7 | 24/10/2026 | US14 — consulta de rastreabilidade ponta a ponta |

Datas conforme a cadência semanal. O escopo de cada Sprint é confirmado no respectivo
Sprint Planning, à luz do feedback da Review anterior.

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
| US09 | Como funcionário autorizado, quero cadastrar medicamentos, para que possam ser controlados pelo sistema | US01 | meia Sprint |
| US23 | Como funcionário autorizado, quero identificar um medicamento lendo o código de barras da caixa, para não precisar procurá-lo digitando | US09 | meia Sprint |
| US24 | Como funcionário autorizado, quero pesquisar o catálogo por nome comercial ou princípio ativo, para localizar um medicamento mesmo sem a caixa em mãos | US09 | pequena |
| US26 | Como funcionário autorizado, quero registrar manualmente um medicamento que chegou sem caixa ou sem código, para que ele não fique fora do controle | US09, US10 | meia Sprint |
| US27 | Como responsável pela conferência, quero revisar as entradas pendentes e liberá-las ou recusá-las, para que apenas medicamento verificado entre no estoque | US26 | meia Sprint |
| US25 | Como funcionário autorizado, quero ser avisado dos medicamentos próximos do vencimento, para consumi-los antes de perder | US10 | pequena |
| US08 | Como administrador, quero cadastrar usuários e definir seus perfis, para que cada funcionário acesse apenas o que lhe compete | US01 | 1 Sprint |
| US10 | Como funcionário autorizado, quero registrar a entrada de medicamentos, identificando a origem, inclusive doações, e informando lote e validade, para controlar o que chega à instituição | US09 | 1 Sprint |
| US11 | Como funcionário autorizado, quero vincular medicamentos a um residente, para saber a quem cada medicamento se destina | US09, **US08** | 1 Sprint |
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
| IT10 | Adquirir leitor de código de barras USB 2D, modo HID | Alta | Bloqueia a validação prática da US23. Leitores apenas 1D não leem DataMatrix e não atendem ao requisito de lote e validade |
| IT11 | Verificar se as caixas recebidas trazem DataMatrix | Alta | Pergunta para a instituição. Define se lote e validade poderão ser capturados automaticamente ou terão de ser digitados |
| IT12 | Avaliar a lista de preços da CMED como fonte de dados de medicamentos | Média | Confirmar se traz GTIN, formato e periodicidade. Importação para o banco local, nunca consulta em tempo de uso |
| IT08 | Definir com o orientador se o uso de IA será declarado no trabalho | Alta | Decide o item IT09; algumas instituições exigem declaração formal |
| IT09 | Limpeza das marcações de atribuição antes da entrega final | Baixa | Depende de IT08. Envolve reescrever as mensagens de commit, mesclar a branch de desenvolvimento na `main` e remover o `CLAUDE.md`. Exige reescrita de histórico, portanto deve ser feita **depois** do último commit e antes de o repositório ser compartilhado |

## Decisões de escopo registradas no planejamento

**Medicamentos sem código de barras (US26 e US27).** Requisito levantado pela
instituição: medicamentos que chegam fora da caixa precisam ser registrados, mas não
podem entrar no estoque antes de revisados.

A solução adotada não move o item entre áreas. Toda entrada possui uma situação, e o
estoque é a soma das entradas conferidas — uma entrada pendente simplesmente não é
contada. Detalhamento em `arquitetura.md`, seção 5.2.

As duas histórias ficam na **Sprint 3**, junto com a US10, porque compartilham a mesma
tela de entrada. Separá-las obrigaria a construir a mesma tela duas vezes.

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
