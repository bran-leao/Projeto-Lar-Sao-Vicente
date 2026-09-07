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

### Previsto para as próximas Sprints

Ainda **não implementado**. Registrado para orientar o desenho das próximas entregas.

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
