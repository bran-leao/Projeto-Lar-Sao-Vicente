# Requisitos

## 1. Contexto

O Lar São Vicente de Paulo é uma Instituição de Longa Permanência para Idosos (ILPI)
que atende aproximadamente **86 residentes**.

O levantamento inicial foi realizado em reunião presencial com representantes da
instituição. Os processos atuais são majoritariamente manuais e apoiados em papel.

## 2. Necessidades levantadas

Registradas conforme relatadas pela instituição:

1. Redução do uso de papel
2. Otimização do tempo dos funcionários
3. Melhor organização das informações
4. Controle de estoque
5. Controle e rastreabilidade de medicamentos
6. Controle de medicamentos recebidos por diferentes origens, inclusive doações
7. Identificação dos medicamentos destinados a cada residente
8. Controle da administração dos medicamentos
9. Registro do responsável por cada movimentação
10. Controle de estoque de outros setores, incluindo cozinha
11. Organização de atividades e agendas dos residentes
12. Geração futura de relatórios e indicadores

**Principal dor identificada:** o gerenciamento do fluxo de medicamentos.

O fluxo que o sistema deverá rastrear no futuro:

```
entrada do medicamento
   -> armazenamento
   -> identificação
   -> vinculação ao residente
   -> separação
   -> administração
   -> registro do profissional responsável
```

## 3. Requisitos funcionais

O identificador indica a Sprint em que o requisito foi ou será atendido.

### Implementados (Sprint 1)

| ID | Requisito | User Story |
|----|-----------|------------|
| RF01 | O sistema deve permitir que funcionários autorizados se autentiquem | US01 |
| RF02 | O sistema deve impedir o acesso não autenticado às páginas internas | US01 |
| RF03 | O sistema deve permitir o encerramento da sessão | US01 |
| RF04 | O sistema deve permitir cadastrar residentes | US02 |
| RF05 | O sistema deve listar os residentes cadastrados | US03 |
| RF06 | O sistema deve permitir pesquisar residentes pelo nome | US03 |
| RF07 | O sistema deve apresentar o perfil individual do residente | US04 |
| RF08 | O sistema deve permitir editar o cadastro de um residente | US05 |
| RF09 | O sistema deve permitir inativar um residente sem apagar seu histórico | US06 |
| RF10 | O sistema deve apresentar um resumo da instituição após o login | US07 |

### Previstos (Sprints futuras)

Não implementados. Constam aqui apenas para registrar o escopo acordado.

| ID | Requisito |
|----|-----------|
| RF11 | Cadastro e manutenção de usuários do sistema |
| RF12 | Cadastro de medicamentos |
| RF13 | Controle de estoque de medicamentos, incluindo doações |
| RF14 | Vinculação de medicamentos ao residente |
| RF15 | Registro da administração de medicamentos |
| RF16 | Rastreabilidade do fluxo completo do medicamento |
| RF17 | Controle de estoque geral |
| RF18 | Controle de estoque da cozinha |
| RF19 | Agenda dos residentes |
| RF20 | Registro de ocorrências |
| RF21 | Atividades |
| RF22 | Cadastro de funcionários |
| RF23 | Relatórios e indicadores |
| RF24 | Consulta ao histórico de auditoria |

## 4. Requisitos não funcionais

| ID | Requisito | Situação na Sprint 1 |
|----|-----------|----------------------|
| RNF01 | Interface em português do Brasil | Atendido |
| RNF02 | Datas exibidas no formato dd/MM/yyyy | Atendido |
| RNF03 | Fuso horário de referência: America/Sao_Paulo | Atendido |
| RNF04 | Senhas nunca armazenadas em texto puro | Atendido (hash PBKDF2 do ASP.NET Core Identity) |
| RNF05 | Arquitetura preparada para controle de acesso por perfis | Atendido (estrutura de perfis do Identity disponível) |
| RNF06 | Entidades relevantes preparadas para auditoria | Atendido (data e responsável por criação e alteração) |
| RNF07 | Ausência de exclusão física de dados com histórico | Atendido (inativação por mudança de situação) |
| RNF08 | Interface responsiva | Atendido |
| RNF09 | Aplicação web executável em navegador atual | Atendido |
| RNF10 | Conformidade com a LGPD e minimização de dados | Ver `regras-de-negocio.md`, seção 5 |

## 5. Restrições

- Nenhum dado real de residentes do Lar São Vicente de Paulo é utilizado em
  desenvolvimento, testes ou demonstrações.
- O sistema manipulará futuramente dados pessoais e informações de saúde, o que exige
  atenção contínua à LGPD.
- O projeto é acadêmico e será desenvolvido ao longo de vários meses, com entregas
  incrementais validadas pela instituição.

## 6. Pontos a confirmar com a instituição

Decisões tomadas na ausência de definição explícita, que devem ser validadas na
Sprint Review:

1. **Quarto obrigatório no cadastro.** Foi assumido que todo residente possui um
   quarto atribuído no momento do cadastro. Se houver período de espera por vaga,
   o campo precisará ser opcional.
2. **Perfis de acesso.** A Sprint 1 possui apenas o perfil Administrador, pois nenhuma
   regra de diferenciação por função foi definida. É preciso levantar quais funções
   existem (enfermagem, cuidadores, administração, cozinha) e o que cada uma acessa.
3. **Reativação de residente.** Foi implementada como contrapartida da inativação, para
   permitir corrigir uma inativação feita por engano. Confirmar se qualquer funcionário
   pode reativar ou se a operação deve ser restrita.
4. **Identificação do quarto.** Não se sabe se a instituição usa apenas número, número
   com letra, ou nome de ala. O campo aceita hoje até 20 caracteres livres.

### Sobre o módulo de medicamentos (Sprint 2 em diante)

5. **Tipo de código nas embalagens.** As caixas recebidas trazem o código bidimensional
   DataMatrix, ou apenas o código de barras tradicional? A resposta define se lote e
   validade poderão ser capturados automaticamente na leitura ou terão de ser digitados
   a cada entrada. Pedir para conferirem algumas caixas na despensa, de compra e de
   doação.
6. **Forma de chegada das doações.** Caixa lacrada, cartela avulsa ou frasco já aberto?
   Cartela avulsa costuma não ter código de barras algum, situação que o sistema precisa
   prever.
7. **Controle atual de lote e validade.** Existe algum controle hoje, em papel ou
   planilha, ou nenhum?
8. **Responsável pelo recebimento.** Quem recebe fisicamente a doação é a mesma pessoa
   que administra o medicamento ao residente?
9. **Farmacêutico responsável.** A instituição possui farmacêutico responsável? Isso
   afeta quem pode registrar determinadas operações.
