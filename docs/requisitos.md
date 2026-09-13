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

## 2.1 Levantamento do ambiente atual

Realizado em setembro de 2026, pela inspeção das planilhas efetivamente em uso na
instituição. Está registrado aqui porque sustenta boa parte das decisões de modelagem do
módulo de medicamentos: são constatações sobre o dado real, não suposições.

### Como a informação é guardada hoje

Não existe sistema. O controle é feito em planilhas do Excel dentro de uma pasta
compartilhada de rede, hospedada em um computador da enfermagem.

Constatações:

- **Pastas e arquivos duplicados.** O mesmo conteúdo aparece em mais de um lugar com
  nomes diferentes. Dois arquivos de estrutura e dados idênticos foram encontrados em
  pastas distintas.
- **Versionamento pelo nome do arquivo.** A versão é indicada no próprio nome — o ano,
  a palavra "atualizado", a palavra "novo". Não há como saber qual é a versão corrente
  sem abrir e comparar.
- **Nome de residente no nome do arquivo.** Arquivos individuais trazem o nome completo
  da pessoa, o que expõe dado pessoal a qualquer um que apenas liste a pasta.
- **Sem evidência de rotina de cópia de segurança.** Nada indica backup: a perda daquele
  computador levaria todo o controle junto.
- **Informação codificada por cor.** Linhas amarelas e rosas carregam significado
  (medicamento "se necessário", insulina). Cor não é dado pesquisável, não sobrevive a
  uma cópia entre planilhas e não é acessível a quem tenha dificuldade de distinguir
  cores.

### A divergência entre planilhas

O mesmo residente aparece em duas planilhas com o medicamento registrado de forma
diferente. Dois casos foram observados:

| Planilha A | Planilha B | Situação |
|-----------|-----------|----------|
| Nome comercial | Princípio ativo correspondente | Mesmo medicamento escrito de duas formas. Só percebe quem conhece a equivalência |
| Formulação simples | Formulação de liberação prolongada | **São medicamentos diferentes.** Não há como saber, pelas planilhas, qual está correto |

O segundo caso é o argumento mais forte a favor do projeto: hoje **não existe fonte
única da verdade**. Duas planilhas discordam sobre o medicamento de uma pessoa e nenhuma
delas pode ser considerada mais confiável que a outra.

### Vocabulário da instituição

Termos e distinções que aparecem nas planilhas e que o sistema precisa respeitar:

- **Insumo não é medicamento.** Luvas, fraldas, lancetas e materiais de procedimento são
  controlados em aba separada. A instituição trata as duas coisas como categorias
  distintas.
- **A apresentação está embutida no nome.** O item é registrado em um único campo de
  texto que reúne princípio ativo, concentração e quantidade por embalagem — no formato
  `NOME 100MG C/ 30CP`.
- **Unidades de medida variadas:** comprimido, caixa, frasco, tubo, sachê, fardo, caixa
  master e unidade.
- **Cinco origens de medicamento**, e não duas: distribuidora, Farmácia Popular, posto de
  saúde (UBS), família do residente e doação.
- **Uso contínuo e uso "se necessário" (SOS)** são distinguidos hoje apenas pela cor da
  linha.
- **Observações clínicas** acompanham alguns itens, incluindo alertas de risco de queda e
  de vigilância redobrada.

### Planilhas identificadas

Descritas pelo conteúdo, sem reproduzir nomes de residentes.

| Planilha | Conteúdo | Contém dado pessoal? | Serve para importação? |
|----------|----------|----------------------|------------------------|
| Catálogo geral do ano | Duas colunas: medicação e quantidade | Não | **Sim** — é a base mais limpa para o catálogo inicial |
| Plano de controle geral | Residente, medicamento prescrito, princípio ativo, quantidade mensal e observações clínicas. Mais de 350 linhas | Sim, inclusive dado de saúde | Parcialmente: apenas as colunas de medicamento e princípio ativo, descartada a coluna de residente |
| Listagens por residente e check-list da família | Medicamentos por pessoa | Sim | Não nesta fase — depende dos perfis de acesso (US08) |
| Aba de insumos | Materiais de enfermagem e higiene | Não | Depende da decisão sobre insumos entrarem ou não no sistema |
| Histórico de compras | Compras realizadas | Não | Não — é histórico administrativo, fora do escopo atual |

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
| RF25 | Registro manual de medicamentos recebidos sem embalagem ou sem código de barras |
| RF26 | Conferência das entradas pendentes antes de comporem o estoque |
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
10. **Quem confere o medicamento recebido.** Toda entrada registrada manualmente ficará
    aguardando conferência antes de contar no estoque. É preciso saber quem tem
    competência para liberar: qualquer funcionário, apenas a enfermagem, ou apenas o
    farmacêutico? Enquanto a US08 não existir, qualquer usuário autenticado poderá
    conferir.
11. **Conferência também para caixas lacradas.** A conferência é obrigatória para o
    registro manual. Para entradas identificadas por leitura de código, a instituição
    deseja que passem igualmente por revisão, ou podem contar no estoque diretamente?

### Sobre as planilhas em uso (levantamento da seção 2.1)

12. **Qual planilha é a fonte da verdade?** Foram encontradas planilhas com o mesmo
    propósito e conteúdos diferentes, além de arquivos idênticos em pastas distintas. É
    preciso saber qual delas a equipe considera correta hoje — a resposta define o que
    será importado e o que será descartado.
13. **Divergência entre formulação simples e de liberação prolongada.** O medicamento de
    um mesmo residente está registrado de duas formas em planilhas diferentes, e as duas
    formulações não são intercambiáveis. Qual está correta? Esse caso serve também para
    mostrar à instituição, na Review, por que a fonte única importa.
14. **Insumos entram no sistema?** Luvas, fraldas, lancetas e materiais de procedimento
    são controlados hoje em aba separada. Devem ser tratados pelo sistema como categoria
    dentro do mesmo cadastro, como módulo próprio, ou ficam de fora nesta fase? A
    resposta muda a modelagem do catálogo, e por isso precisa vir antes da Sprint 2.
