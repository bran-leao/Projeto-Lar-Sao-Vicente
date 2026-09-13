# Arquitetura

## 1. Visão geral

Aplicação web monolítica em **ASP.NET Core MVC**, organizada em camadas.

A escolha por um monolito é deliberada: o sistema atende uma instituição com cerca de
86 residentes e poucos usuários simultâneos. Microsserviços acrescentariam complexidade
de implantação e operação sem resolver nenhum problema real deste contexto.

```
┌──────────────────────────────────────────────┐
│  LarSaoVicente.Web            (apresentação) │
│  Controllers · Views · ViewModels            │
└───────────────┬──────────────────────────────┘
                │
┌───────────────▼──────────────────────────────┐
│  LarSaoVicente.Infrastructure  (persistência)│
│  AppDbContext · Migrations · Identity        │
└───────────────┬──────────────────────────────┘
                │
┌───────────────▼──────────────────────────────┐
│  LarSaoVicente.Domain              (negócio) │
│  Entidades · Enums · Regras                  │
└──────────────────────────────────────────────┘
```

A dependência aponta sempre para dentro. O projeto **Domain não referencia o Entity
Framework nem o ASP.NET Core**: as regras de negócio podem ser testadas sem banco de
dados e sem servidor web, e uma eventual troca de tecnologia de persistência não as afeta.

## 2. Projetos

| Projeto | Responsabilidade | Depende de |
|---------|------------------|------------|
| `LarSaoVicente.Domain` | Entidades, enumerações, regras de negócio e abstrações | — |
| `LarSaoVicente.Infrastructure` | Acesso a dados (EF Core), Identity, migrations, carga inicial | Domain |
| `LarSaoVicente.Web` | Controllers, Views, ViewModels, arquivos estáticos | Domain, Infrastructure |
| `LarSaoVicente.Tests` | Testes unitários e de integração | Todos |

## 3. Tecnologias

| Item | Versão | Observação |
|------|--------|------------|
| .NET | 10.0 (LTS) | Suporte até novembro de 2028 |
| ASP.NET Core MVC | 10.0 | Padrão MVC com Razor |
| Entity Framework Core | 10.0.11 | Provedor SQL Server |
| SQL Server | 2019 ou superior | Testado em SQL Server 2022 |
| ASP.NET Core Identity | 10.0 | Autenticação e perfis de acesso |
| Bootstrap | 5.3.3 | Servido localmente, sem CDN |
| jQuery / jQuery Validation | — | Apenas validação de formulários |
| xUnit | 2.9.3 | Testes automatizados |

O arquivo `global.json` fixa a versão do SDK, de modo que todos os integrantes compilem
o projeto com o mesmo compilador.

**Bootstrap e jQuery são servidos a partir do próprio servidor**, e não de uma CDN. A
instituição pode ter internet instável, e o sistema precisa funcionar mesmo sem acesso
externo.

## 4. Decisões de projeto

### 4.1 Entidades com estado encapsulado

As propriedades de `Resident` possuem `private set`. A entidade só é alterada pelo
construtor e pelos métodos `Update`, `Deactivate` e `Reactivate`, que validam as regras
antes de aceitar qualquer mudança.

Isso impede que um controller futuro grave um residente inválido por esquecimento. Uma
entidade em estado inconsistente não chega a existir.

### 4.2 ViewModels separados das entidades

Os formulários trabalham com ViewModels, nunca diretamente com as entidades.

Além de manter a interface independente do modelo de dados, isso evita *overposting*:
um campo acrescentado ao formulário pelo navegador — por exemplo, `CreatedAt` — não tem
como alcançar a entidade.

### 4.3 Identificadores em GUID

`Resident.Id` é um `Guid`, e não um inteiro sequencial.

Com identificadores sequenciais, as URLs revelariam quantos residentes existem e
permitiriam percorrer os cadastros por tentativa e erro (`/Residentes/1`, `/2`, `/3`).
Tratando-se de dados de pessoas idosas sob cuidado, essa exposição é indesejável.

### 4.4 Auditoria no contexto de dados

`AppDbContext.SaveChanges` preenche automaticamente `CreatedAt`, `CreatedByUserId`,
`UpdatedAt` e `UpdatedByUserId` de qualquer entidade que herde de `AuditableEntity`.

A alternativa — preencher nos controllers — dependeria de ninguém esquecer. Concentrando
no contexto, nenhuma gravação escapa da auditoria. É também a base do requisito de
registrar o responsável por cada movimentação de medicamento, previsto para as próximas
Sprints.

### 4.5 Autorização exigida por padrão

A política padrão da aplicação exige autenticação em **todos** os endpoints. Liberar o
acesso anônimo requer marcação explícita com `[AllowAnonymous]`.

O caminho inverso — proteger cada tela individualmente — deixaria uma tela nova
desprotegida ao primeiro esquecimento.

> Consequência prática: os arquivos estáticos também precisam ser liberados
> explicitamente (`app.MapStaticAssets().AllowAnonymous()`), pois `MapStaticAssets`
> cria endpoints sujeitos à mesma política. Sem isso, a própria tela de login é
> exibida sem estilo e sem JavaScript.

### 4.6 Identity sem a interface padrão

O ASP.NET Core Identity é utilizado pelos seus serviços (hash de senha, bloqueio por
tentativas, perfis), mas **sem as páginas que ele fornece prontas**.

A interface padrão traz autocadastro, recuperação de senha por e-mail, autenticação em
dois fatores e login por provedores externos. Nada disso faz sentido em um sistema
interno onde as contas são criadas pela administração da instituição. As telas de login
e logout são próprias e em português.

### 4.7 Fuso horário explícito

As datas de auditoria são gravadas em UTC e convertidas para `America/Sao_Paulo` apenas
na exibição. As regras que dependem do dia corrente usam a abstração `IDateTimeProvider`.

Isso mantém as validações determinísticas nos testes e garante que "hoje" seja sempre o
dia no Brasil, independentemente da configuração do servidor.

### 4.8 Vinculação de identificadores pela rota

As ações que recebem o identificador do residente usam `[FromRoute]`.

No ASP.NET Core, o provedor de valores de formulário tem precedência sobre o de rota.
Sem a marcação, o parâmetro `id` seria preenchido pelo campo oculto enviado pelo
navegador, permitindo abrir o próprio cadastro, alterar esse campo e gravar sobre o
registro de outro residente.

## 5. Modelo de dados

### Implementado na Sprint 1

```
┌────────────────────────────┐        ┌──────────────────────────┐
│ Residentes                 │        │ AspNetUsers              │
├────────────────────────────┤        ├──────────────────────────┤
│ Id                 (GUID)  │        │ Id             (string)  │
│ FullName           (150)   │        │ UserName                 │
│ BirthDate          (date)  │        │ Email                    │
│ AdmissionDate      (date)  │        │ PasswordHash             │
│ Room               (20)    │        │ FullName                 │
│ DependencyLevel    (int)   │        │ ...                      │
│ Status             (int)   │        └──────────────────────────┘
│ Notes              (1000)  │                    ▲
│ CreatedAt       (datetime2)│                    │
│ CreatedByUserId    (450)   │────────────────────┘
│ UpdatedAt       (datetime2)│   responsável pela operação
│ UpdatedByUserId    (450)   │   (referência lógica, sem chave estrangeira)
└────────────────────────────┘
```

Os campos de auditoria guardam o identificador do usuário sem chave estrangeira
declarada. Assim, o histórico permanece legível mesmo que uma conta seja removida no
futuro — o registro de quem realizou a operação não deve depender da conta continuar
existindo.

Tabelas do Identity criadas pela migration inicial: `AspNetUsers`, `AspNetRoles`,
`AspNetUserRoles`, `AspNetUserClaims`, `AspNetUserLogins`, `AspNetUserTokens`,
`AspNetRoleClaims`.

### Implementado na Sprint 2 — catálogo

```
┌──────────────────────────────────────┐
│ Medicamentos                         │
├──────────────────────────────────────┤
│ Id                       (GUID)      │
│ CommercialName           (150)       │  obrigatório
│ ActiveIngredient         (150, nulo) │  obrigatório se categoria = Medicamento
│ Strength                 (60, nulo)  │  texto: "100 mg", "2 mg + 5 mg", "0,004 mg/mL"
│ Form                     (int, nulo) │  obrigatório se categoria = Medicamento
│ UnitsPerPackage          (int, nulo) │  1 a 10.000
│ PackageUnit              (int)       │  obrigatório
│ Barcode                  (14, nulo)  │  índice único, admite vários nulos
│ Category                 (int)       │  Medicamento | Insumo
│ Status                   (int)       │  Ativo | Inativo
│ Notes                    (1000, nulo)│
│ NormalizedSearchKey      (300)       │  derivado; índice não único
│ + campos de AuditableEntity          │
└──────────────────────────────────────┘
```

Cada decisão abaixo veio de uma contagem sobre os 144 itens do catálogo real da
instituição, não de suposição.

| Decisão | O que o dado mostrou |
|---------|----------------------|
| `Strength` é texto, não número com unidade | Aparecem associações de dois fármacos, proporções por mililitro e unidades internacionais. Estruturar obrigaria a recusar o cadastro ou adivinhar — e adivinhar concentração é risco clínico |
| `Strength` é opcional | 28 dos 144 itens não trazem concentração no nome (`ÁCIDO FÓLICO C/ 30CP`, `COMPLEXO B`, colírios) |
| `UnitsPerPackage` é opcional | 17 itens não informam quantidade por embalagem — colírios, gotas e um inalador |
| `PackageUnit` é obrigatório e de lista fechada | A planilha registra "17CX" e "1FR". Trinta sem unidade não significa nada |
| `ActiveIngredient` é obrigatório só para medicamento | Luva e fralda não são fármacos. Exigir o campo obrigaria a equipe a inventar um valor |
| `Barcode` é opcional, com índice único | Cartela avulsa de doação frequentemente não tem código. O índice único do SQL Server admite vários nulos |
| `NormalizedSearchKey` existe | 14 itens estão gravados em duas grafias que diferem só por um espaço ou um ponto: `Losartana 50 mg` e `Losartana 50mg`, `Vitamina D 7.000 UI` e `Vitamina D 7000 UI` |

**A chave de busca não é única.** O mesmo medicamento de laboratórios diferentes gera
registros distintos, o que é correto para rastreabilidade. A chave serve para localizar
e para **avisar** sobre um provável duplicado, nunca para impedir o cadastro.

**Validação do código de barras.** `Gtin.IsValid` aceita EAN-8, UPC-A, EAN-13 e DUN-14 —
todos da mesma família e com o mesmo dígito verificador. A conferência acontece antes de
qualquer consulta ao banco e sem depender de rede, conforme a seção 5.1.

### Previsto para as próximas Sprints

Ainda **não implementado**, com exceção de `Medication`, descrito acima. Registrado para orientar o desenho das próximas entregas.

O fluxo de medicamentos exige **quatro entidades distintas**. Reuni-las em uma única
tabela "Medicamentos" é o erro de modelagem mais comum nesse tipo de sistema e
inviabiliza a rastreabilidade, porque cada uma responde a uma pergunta diferente:

| Entidade | Responde | Exemplo |
|----------|----------|---------|
| `Medication` | Que remédio é? | Losartana 50&nbsp;mg comprimido, laboratório X |
| `MedicationBatch` | Qual remessa? | 20 caixas, lote ABC123, validade 03/2027, doação |
| `MedicationPrescription` | Quem toma o quê? | Maria Oliveira, 1 comprimido às 8h |
| `MedicationAdministration` | O que aconteceu? | Fulano administrou às 8h05 de 12/09 |

```
Medication ──< MedicationBatch                    (entrada: compra ou doação)
     │                  │
     │                  ▼
     │          MedicationAdministration ──> User (responsável)
     │                  ▲
     └──< MedicationPrescription >── Resident
```

A estrutura atual comporta essa evolução sem alterações estruturais: `Resident` já
possui identificador estável, e `AuditableEntity` já registra o responsável por cada
operação.

## 5.1 Identificação de medicamentos por código de barras

Decisão de projeto tomada no planejamento da Sprint 2, registrada aqui porque orienta
o desenho de todo o módulo.

### O que cada código identifica

A caixa de medicamento pode trazer dois códigos, que dizem coisas diferentes:

| Código | Identifica | Traz lote e validade? |
|--------|-----------|----------------------|
| **EAN-13** (barras tradicional) | O **produto**: apresentação comercial | Não. Toda caixa do mesmo produto tem código idêntico |
| **DataMatrix** (2D, do SNCM) | A **caixa específica**: produto, série, lote e validade | Sim |

A consequência é direta: com EAN-13 apenas, lote e validade precisam ser digitados a
cada entrada. Para doações isso é crítico, pois validade curta é o principal risco.

A implantação do SNCM (Lei nº 11.903/2009) sofreu sucessivos adiamentos, e não se pode
assumir que toda caixa recebida trará DataMatrix. **O sistema deve funcionar nos dois
casos**: quando houver DataMatrix, lote e validade são preenchidos automaticamente;
quando não houver, são digitados.

### Catálogo que se constrói pelo uso

O sistema **não depende de nenhuma API externa** para identificar medicamentos. Ao ler
um código:

- **código conhecido** — o medicamento é exibido imediatamente;
- **código desconhecido** — abre-se o cadastro com o código já preenchido, e alguém da
  instituição completa nome, princípio ativo e concentração uma única vez.

A partir da segunda caixa daquele produto, a identificação é automática. O catálogo
cresce com o uso real da instituição.

A razão é a mesma que levou a servir Bootstrap e jQuery localmente: a instituição pode
ter internet instável, e a leitura acontece no balcão, com alguém esperando. Uma
dependência de rede em tempo de uso transformaria uma falha de internet em impedimento
de trabalho — e, no contexto acadêmico, em risco de indisponibilidade durante a
apresentação.

A importação de uma base pública de medicamentos (a lista de preços da CMED é a
candidata mais promissora, por conter GTIN) permanece como **melhoria futura**, na
forma de importação para o banco local, nunca como consulta em tempo de uso.

### Validação em três camadas, toda local

1. **Formato** — o EAN-13 possui dígito verificador calculado a partir dos doze
   primeiros dígitos. O sistema refaz o cálculo e recusa leituras inconsistentes antes
   de consultar o banco.
2. **Tipo** — o comprimento e a estrutura distinguem um EAN-13 de um DataMatrix GS1.
3. **Existência** — consulta ao catálogo pelo código.

### Consequências para a modelagem

- O código de barras é **opcional** em `Medication`: cartelas avulsas recebidas por
  doação frequentemente não possuem código, e o cadastro manual precisa continuar
  possível.
- Índice **único** sobre o código, permitindo múltiplos registros sem código.
- O mesmo medicamento de laboratórios diferentes possui códigos diferentes e gera
  registros distintos, o que é correto para rastreabilidade. Por isso a busca por
  princípio ativo é obrigatória, e não apenas por nome comercial.

### Hardware

Leitor **USB 2D em modo HID** (emulação de teclado). Não exige driver nem SDK: o
aparelho digita o código no campo em foco e envia Enter, o que para a aplicação é um
envio de formulário comum. Leitores exclusivamente 1D não leem DataMatrix e, por isso,
não atendem ao requisito de lote e validade.

## 5.2 Conferência de entradas sem código de barras

Requisito levantado pela instituição no planejamento da Sprint 2: medicamentos que
chegam fora da caixa — cartelas avulsas, frascos soltos, doações abertas — precisam ser
registrados, mas **não podem entrar no estoque antes de revisados**.

### O estoque é calculado, não transferido

A solução intuitiva seria manter uma área de triagem e **mover** o item para o estoque
após a revisão. Foi descartada: uma operação de transferência pode falhar parcialmente,
duplica o registro e cria a possibilidade de alguém esquecer de executá-la.

Em vez disso, **toda entrada possui uma situação**, e o estoque é a soma das entradas
conferidas. Nada se move.

| Situação | Conta no estoque? | Significado |
|----------|-------------------|-------------|
| `AguardandoConferencia` | Não | Registrada, ainda não liberada para uso |
| `Conferida` | Sim | Revisada e liberada |
| `Recusada` | Não | Revisada e recusada, com o motivo registrado |

Consequências:

- Uma entrada pendente não precisa ser "segurada" em lugar nenhum: ela simplesmente não
  é contada. Não existe transferência que possa falhar ou ser esquecida.
- A entrada recusada permanece registrada, coerente com a regra de não excluir
  fisicamente informação com histórico. Além disso, é informação de gestão: permite
  responder quantas doações vencidas a instituição recebeu em um período.
- O histórico é único, do recebimento à conferência, com data e responsável em cada
  etapa — preenchidos por `AuditableEntity`.

### Quando a conferência é obrigatória

**Toda entrada registrada manualmente, sem leitura de código, nasce
`AguardandoConferencia`.** A razão é direta: os dados foram digitados, não verificados.

Para entradas identificadas por leitura de código, a exigência de conferência é decisão
da instituição, a ser confirmada em Review. Doação de caixa lacrada pode ou não exigir
revisão, dependendo de quem recebe fisicamente o material.

### Validade não identificada

Cartela avulsa frequentemente chega sem validade legível. O sistema precisa aceitar o
registro assinalando **"validade não identificada"**, em vez de recusar o cadastro — do
contrário, o caso mais comum em doação seria justamente o que o sistema não consegue
registrar.

Entradas nessa condição devem ser destacadas na tela de conferência: validade
desconhecida é o principal risco em medicamento doado.

### Quem confere

Enquanto não houver perfis de acesso (US08), qualquer usuário autenticado pode conferir.
A restrição por perfil entra junto com a US08, e a conferência é um dos casos que a
tornam necessária: liberar medicamento para uso é decisão de responsabilidade técnica,
não operacional.

## 5.3 O que os dados reais da instituição impõem à modelagem

As seções 5.1 e 5.2 foram desenhadas antes de conhecer as planilhas em uso. A inspeção
descrita em `requisitos.md`, seção 2.1, confirmou parte do desenho e obrigou a corrigir
outra parte. Esta seção registra o que mudou e por quê.

### Insumo não é medicamento

A instituição mantém luvas, fraldas, lancetas e materiais de procedimento em aba
separada dos medicamentos. A distinção é da própria equipe, não uma invenção do sistema,
e ignorá-la faria a listagem de medicamentos misturar coisas que ninguém procura juntas.

O catálogo prevê uma **categoria** que separa medicamento de insumo. Um único cadastro
com categoria é preferível a dois módulos: a entrada, a conferência, o estoque e o
alerta de vencimento são idênticos para os dois casos, e duplicar as telas duplicaria
também os defeitos. Confirmar antes da Sprint 2 se insumos entram nesta fase
(`requisitos.md`, seção 6, pergunta 14).

### A apresentação vem embutida no nome

Nas planilhas, um item é uma única linha de texto: `NOME 100MG C/ 30CP` — princípio
ativo, concentração e quantidade por embalagem no mesmo campo.

Isso não é erro da equipe; é o que uma planilha permite. Mas impede qualquer pesquisa
útil: não há como listar tudo de 100 mg, nem somar caixas de apresentações diferentes do
mesmo princípio ativo.

O catálogo separa em campos distintos:

| Campo | Exemplo | Observação |
|-------|---------|------------|
| Nome comercial | `AAS` | Obrigatório |
| Princípio ativo | `Ácido acetilsalicílico` | Obrigatório — é o que permite reconhecer o mesmo medicamento entre marcas |
| Concentração | `100 mg` | Obrigatório |
| Forma farmacêutica | comprimido | Obrigatório |
| Quantidade por embalagem | `30` | Obrigatório |
| Unidade | comprimido | Obrigatório |

A importação inicial precisará **quebrar** o texto das planilhas nesses campos. Isso não
é totalmente automatizável: o formato varia entre linhas, e um palpite errado sobre
concentração é risco clínico. A separação é sugerida pelo sistema e **confirmada por
pessoa** antes de gravar — a mesma lógica da conferência de entradas da seção 5.2.

### Unidade de medida é campo obrigatório

As planilhas usam comprimido, caixa, frasco, tubo, sachê, fardo, caixa master e unidade.
"30" sem unidade não significa nada: pode ser trinta comprimidos ou trinta caixas.

O campo é obrigatório e vem de uma lista fechada (`enum`), não de texto livre. Texto
livre reproduziria no sistema exatamente o problema das planilhas: `CX`, `Cx`, `caixa` e
`Caixas` como quatro coisas diferentes para o banco e a mesma coisa para a pessoa.

### Cinco origens, não duas

O desenho anterior previa compra e doação. As planilhas mostram cinco caminhos reais:
**distribuidora, Farmácia Popular, posto de saúde (UBS), família do residente e doação**.

A distinção importa além do registro: medicamento trazido pela família pertence àquele
residente e não deve ser consumido por outro; medicamento da Farmácia Popular e da UBS
tem prazo de retirada e pode faltar; medicamento de doação é o que mais chega perto do
vencimento. São regras diferentes sobre a mesma entrada, e só existem se a origem for um
campo, não uma observação.

### Uso contínuo e uso "se necessário"

A planilha marca de amarelo a linha do medicamento SOS. É informação clínica relevante
guardada como cor: some ao copiar, não aparece em pesquisa e não é legível por quem tem
dificuldade de distinguir cores.

No sistema, é um campo do vínculo entre medicamento e residente (US11), não do catálogo:
o mesmo medicamento é contínuo para uma pessoa e "se necessário" para outra.

### Condições especiais de armazenamento

A insulina é destacada em cor própria nas planilhas, o que sugere tratamento
diferenciado — refrigeração e procedimento próprio de administração.

**Não confirmado.** A leitura da cor é interpretação nossa, não dado. As 365 linhas do
plano consolidado não mencionam refrigeração nem geladeira uma única vez. A marcação de
conservação refrigerada **não foi implementada** no catálogo: acrescentar campo pessoal
ou operacional sem necessidade concreta contraria a minimização de dados que o projeto
adotou. Fica como pergunta para a instituição — se houver item que exija geladeira, o
campo entra depois, com a razão registrada.

### Alertas clínicos

A planilha mais completa traz observações do tipo "risco de quedas" e "vigilância
redobrada" ao lado de certos medicamentos.

Esse conteúdo é **dado pessoal sensível de saúde** e acompanha o vínculo com o residente,
nunca o catálogo. Entra junto com a US11, depois dos perfis de acesso (US08) — pela mesma
razão já registrada no `product-backlog.md`.

### Importação do catálogo inicial

Estratégia recomendada, em ordem de risco crescente:

1. **Catálogo geral do ano** (colunas medicação e quantidade) como base. É a planilha
   mais limpa e **não contém dado pessoal algum** — pode ser trabalhada sem qualquer
   cuidado especial de privacidade.
2. **Plano de controle geral**, usado apenas para enriquecer com o princípio ativo.
   Descarta-se a coluna do residente **antes** de o arquivo sair do computador da
   instituição, e ficam apenas as duas colunas de medicamento, sem repetições. O
   resultado é um catálogo com princípio ativo e sem nenhum dado pessoal.
3. **Nada de listagens por residente** nesta fase. São dados de saúde e dependem dos
   perfis de acesso.
4. **Nada de histórico de compras.** É informação administrativa, fora do escopo.

A importação é um utilitário de carga inicial, executado uma vez, com revisão humana
antes da gravação. Não é funcionalidade permanente do sistema, e por isso não deve virar
tela de uso corrente.

## 6. Organização de pastas

```
Projeto-Lar-Sao-Vicente/
├── LarSaoVicente.sln
├── global.json                 versão do SDK
├── Directory.Build.props       propriedades comuns aos projetos
├── docs/                       documentação do projeto
├── src/
│   ├── LarSaoVicente.Domain/
│   │   ├── Abstractions/       IDateTimeProvider, ICurrentUser
│   │   ├── Common/             AuditableEntity, DomainValidationException
│   │   ├── Entities/           Resident
│   │   └── Enums/              DependencyLevel, ResidentStatus
│   ├── LarSaoVicente.Infrastructure/
│   │   ├── Identity/           ApplicationUser, Roles
│   │   ├── Persistence/        AppDbContext, Migrations, carga inicial
│   │   └── Services/           SystemDateTimeProvider
│   └── LarSaoVicente.Web/
│       ├── Controllers/
│       ├── Extensions/
│       ├── Services/           CurrentUser
│       ├── ViewModels/
│       ├── Views/
│       └── wwwroot/
└── tests/
    └── LarSaoVicente.Tests/
        ├── Domain/             testes unitários
        ├── Infraestrutura/     hospedagem da aplicação para testes
        └── Integracao/         testes de ponta a ponta
```

## 7. Estratégia de testes

| Tipo | Alvo | Banco |
|------|------|-------|
| Unitário | Regras de negócio de `Resident` | Nenhum |
| Integração | Aplicação real: HTTP, controller, domínio e persistência | SQLite em memória |

Os testes de integração hospedam a **aplicação real**, com o mesmo pipeline e as mesmas
políticas de autorização usadas em produção. Apenas o provedor de banco é substituído,
para que a suíte rode em qualquer máquina sem exigir um SQL Server instalado.

As migrations continuam sendo geradas e aplicadas exclusivamente para SQL Server, que é
o banco de destino.
