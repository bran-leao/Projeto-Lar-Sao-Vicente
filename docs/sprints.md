# Registro de Sprints

Histórico das Sprints do projeto. Cada Sprint registra objetivo, escopo, resultado e
aprendizados, servindo de base para o capítulo de metodologia do TCC.

## Calendário

Sprints de **7 dias**, com entrega aos **sábados**.

| Sprint | Entrega | Situação |
|--------|---------|----------|
| Sprint 1 | sábado, 12/09/2026 | Concluída, aguardando Review |
| Sprint 2 | sábado, 19/09/2026 | Concluída, aguardando Review |
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

Registrados por serem material relevante para a discussão de qualidade no TCC. **Cinco
dos sete só apareceram ao executar a aplicação**; nenhum deles seria percebido lendo o
código ou compilando.

| # | Defeito | Causa | Correção |
|---|---------|-------|----------|
| 1 | Tela de login exibida sem estilo e sem JavaScript | A política de autorização padrão se aplica também aos endpoints de arquivos estáticos criados por `MapStaticAssets` | `app.MapStaticAssets().AllowAnonymous()`, com teste de regressão |
| 2 | Barra lateral sem cor de fundo, com texto branco sobre branco | O Bootstrap impõe `background-color: transparent !important` ao `offcanvas` acima do breakpoint | Cor aplicada a um painel interno próprio, sem recorrer a `!important` |
| 3 | Barra lateral encolhia em telas com conteúdo largo | Item flex com `flex-shrink` padrão | `flex: 0 0 260px` |
| 4 | Validação do formulário de login inoperante | O layout de login não carregava o jQuery exigido pelos scripts de validação | jQuery incluído antes dos scripts de validação |
| 5 | **Possível gravação sobre o cadastro de outro residente** | No ASP.NET Core, o provedor de valores de formulário tem precedência sobre o de rota: o parâmetro `id` era preenchido pelo campo oculto enviado pelo navegador | `[FromRoute]` nas ações, com teste de regressão |
| 6 | Contagem de testes incorreta nesta documentação | Erro de contagem manual ao redigir. O total de 48 estava certo; a divisão entre os grupos, não | Tabela corrigida e demais fatos verificáveis auditados |
| 7 | Cadeia de conexão de desenvolvimento no arquivo de produção | O `appsettings.json` guarda a configuração de produção, mas era ele que precisava ser editado para rodar localmente | Cadeia movida para `appsettings.Development.json`, apontando para LocalDB |

O defeito 5 é o mais relevante: era uma falha de segurança real, encontrada por um teste
de integração escrito justamente para verificar essa hipótese.

## Primeira execução em máquina de desenvolvimento

Em 08/09/2026 o sistema foi executado pela primeira vez em uma máquina de
desenvolvimento independente daquela em que foi construído — Windows, Visual Studio 2026
e LocalDB —, e respondeu corretamente.

O procedimento revelou três obstáculos que a documentação não cobria, todos corrigidos:

1. **A branch.** O `README.md` não avisava que a `main` contém apenas o próprio README.
   Quem clonasse sem indicar a branch de desenvolvimento encontraria o projeto
   aparentemente vazio, sem explicação.
2. **O escape da barra invertida.** Escrever `Server=.\SQLEXPRESS` com uma única barra
   torna o `appsettings.json` ilegível, e a aplicação encerra com
   `InvalidDataException` antes de tentar conectar ao banco. A mensagem não menciona a
   cadeia de conexão, o que dificulta associar a causa.
3. **O arquivo errado.** A configuração que precisava ser editada era a de produção
   (defeito 7). Além do risco de quebrá-la, isso levaria o nome do servidor local de
   cada integrante para dentro do repositório.

O episódio confirma o aprendizado registrado abaixo: **executar em outra máquina revela
o que executar na própria não revela**. Nenhum dos três apareceu durante o
desenvolvimento, porque o ambiente de construção já estava configurado.

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
- **Executar em outra máquina revela o que executar na própria não revela.** Três
  obstáculos de instalação só apareceram quando alguém clonou o repositório do zero, em
  ambiente não preparado. A documentação de instalação só se prova acompanhando alguém
  que a siga pela primeira vez.

## Próxima Sprint

Sugestão a validar no planejamento: **US08 — cadastro de usuários e perfis de acesso**.

É pré-requisito das demais: sem perfis definidos, não há como restringir quem registra a
administração de medicamentos, que é o requisito central do projeto. Depende de definir
com a instituição quais funções existem (item IT01 do backlog).

---

# Sprint 2

## Sprint Goal

> Permitir que a instituição identifique um medicamento pela leitura do código da
> embalagem e registre o que chegou, informando de uma vez quantas unidades foram
> recebidas.

## Período

| | |
|---|---|
| Entrega e Sprint Review | sábado, **19/09/2026** |
| Desenvolvimento versionado | 13/09 a 15/09/2026 |

## Escopo

| ID | User Story |
|----|------------|
| US09 | Cadastrar medicamentos |
| US23 | Identificar medicamento pela leitura do código |
| US24 | Pesquisar por nome comercial ou princípio ativo |
| US10 | Registrar entrada, com lote, validade e origem |
| US26 | Registrar manualmente quando o código está ausente ou danificado |
| US27 | Conferir as entradas pendentes antes de comporem o estoque |

US10, US26 e US27 estavam previstas para a Sprint 3. Foram antecipadas a pedido do
Product Owner, com uma razão concreta: sem a entrada, o catálogo é uma lista que não
responde "quanto temos", e a demonstração não mostraria o problema resolvido. As três
compartilham a mesma tela — separá-las obrigaria a construí-la duas vezes.

### Explicitamente fora do escopo

- **Vínculo entre medicamento e residente.** É dado pessoal sensível de saúde, e depende
  do controle de acesso por perfil (US08).
- **Importação das planilhas da instituição** (US28). Bloqueada por IT14 e IT15.
- **Alerta de vencimento** (US25). A informação já é registrada e destacada na
  conferência, mas o aviso ativo fica para a Sprint 3.

## Decisões tomadas a partir do dado real

A inspeção das planilhas da instituição (`requisitos.md`, seção 2.1) aconteceu no meio
desta Sprint e corrigiu a modelagem **antes** de ela ser escrita. Cada decisão abaixo
saiu de uma contagem, não de suposição:

| Decisão | Evidência |
|---------|-----------|
| Concentração é texto e é opcional | 28 dos 144 itens não a informam; os demais trazem associações de dois fármacos e proporções por mililitro |
| Quantidade por embalagem é opcional | 17 itens não a informam — colírios, gotas e um inalador |
| Unidade de embalagem é obrigatória, de lista fechada | A planilha registra "17CX" e "1FR"; trinta sem unidade não significa nada |
| Chave de busca normalizada | 14 itens estão gravados em duas grafias que diferem só por um espaço: `Losartana 50 mg` e `Losartana 50mg` |
| Cinco origens, não duas | Distribuidora, Farmácia Popular, UBS, família e doação |
| Princípio ativo obrigatório só para medicamento | Luva e fralda não são fármacos |

## Resultado

### Entregue

- Catálogo de medicamentos e insumos, com cadastro, edição, inativação e reativação
- Leitura de código de barras e de DataMatrix, com validação local em três camadas
- Captura automática de lote, validade e número de série a partir do DataMatrix
- Busca por nome comercial e por princípio ativo
- Registro de entrada com quantidade, lote, validade, origem e data de recebimento
- Marcação de **validade não identificada**, para a cartela avulsa sem embalagem legível
- Conferência das entradas pendentes, com liberação ou recusa motivada
- Estoque calculado como soma das entradas conferidas

### Confirmação recebida da instituição

**As caixas recebidas trazem DataMatrix** (item IT11, respondido em 15/09/2026). Lote e
validade passam a vir da leitura em vez de digitados a cada entrada. O leitor 2D (IT10)
deixa de ser conveniência e passa a ser requisito.

### Testes

| Grupo | Quantidade |
|-------|-----------|
| Unitários do domínio | 104 |
| Integração | 61 |
| **Total** | **165** |

Todos passando. Build sem erros e sem avisos.

### Verificação com o sistema em execução

A aplicação foi executada contra um **SQL Server 2022 real**, com banco criado do zero
pelas migrations, e dirigida por navegador no fluxo completo: leitura de DataMatrix de
código conhecido e desconhecido, código com dígito verificador errado, registro de onze
unidades em um único registro, conferência e estoque. Nenhum erro de JavaScript e nenhum
erro 500.

## Defeitos encontrados e corrigidos durante a Sprint

| # | Defeito | Como apareceu |
|---|---------|---------------|
| 1 | O POST de edição de entrada devolvia o formulário para uma entrada já conferida, enquanto o GET redirecionava com a mensagem. O funcionário ficaria reenviando um dado que nunca seria aceito | Teste de integração |
| 2 | "Envelope com 1" e "Frasco com 1" apareciam na listagem; a quantidade por embalagem igual a um não informa nada | Captura de tela |
| 3 | Com a coluna de ações fixada em uma linha, o botão "Recusar" saía da área visível da tabela | Captura de tela, após uma tentativa de deixar as linhas mais baixas |

## Aprendizado

**Conferir a suposição contra o dado antes de escrever o código.** O desenho anterior
previa duas origens de medicamento; o dado real mostrou cinco. Previa concentração
estruturada; o dado real mostrou associações de dois fármacos. A marcação de
"conservação refrigerada", que havia sido registrada como necessidade confirmada,
vinha da leitura da cor de uma linha em foto — as 365 linhas do plano consolidado não
mencionam refrigeração uma única vez, e o campo foi retirado.

**A captura de tela encontra o que o código de status não encontra.** Os três defeitos
desta Sprint retornavam HTTP 200 e passavam nos testes.

## Próxima Sprint

Sprint 3, entrega em 26/09/2026. Candidatos, a confirmar no Planning à luz da Review:

- **US25** alerta de medicamentos próximos do vencimento
- **US28** importação do catálogo a partir das planilhas, se IT14 e IT15 forem resolvidos
- **US08** usuários e perfis de acesso, que destrava o vínculo com o residente
