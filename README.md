# Sistema de Gestão — Lar São Vicente de Paulo

Sistema web de gestão para uma Instituição de Longa Permanência para Idosos (ILPI),
desenvolvido como Trabalho de Conclusão de Curso de Análise e Desenvolvimento de
Sistemas, em parceria com o Lar São Vicente de Paulo.

O objetivo é centralizar as informações da instituição e reduzir processos manuais,
com atenção especial ao controle e à rastreabilidade de medicamentos.

**Situação atual:** Sprint 1 concluída — autenticação e cadastro de residentes.

## Documentação

| Documento | Conteúdo |
|-----------|----------|
| [`docs/requisitos.md`](docs/requisitos.md) | Contexto, requisitos funcionais e não funcionais |
| [`docs/arquitetura.md`](docs/arquitetura.md) | Camadas, decisões de projeto e modelo de dados |
| [`docs/regras-de-negocio.md`](docs/regras-de-negocio.md) | Regras implementadas e onde estão no código |
| [`docs/product-backlog.md`](docs/product-backlog.md) | Backlog priorizado |
| [`docs/sprints.md`](docs/sprints.md) | Histórico das Sprints |
| [`docs/sprint-1-review.md`](docs/sprint-1-review.md) | Roteiro da demonstração e registro de feedback da Sprint 1 |

## Tecnologias

.NET 10 · ASP.NET Core MVC · Entity Framework Core 10 · SQL Server · ASP.NET Core
Identity · Bootstrap 5.3 · xUnit

## Pré-requisitos

- [.NET SDK 10.0](https://dotnet.microsoft.com/download/dotnet/10.0)
- SQL Server 2019 ou superior (Express, Developer ou LocalDB)
- Visual Studio 2026, Visual Studio Code ou Rider

> O Visual Studio 2022 **não** abre projetos .NET 10. Use o Visual Studio 2026 ou
> execute pela linha de comando.

## Como executar

### 1. Clonar o repositório

O desenvolvimento acontece na branch `claude/tcc-project-96i658`. A branch `main`
contém apenas este README.

```bash
git clone -b claude/tcc-project-96i658 https://github.com/bran-leao/Projeto-Lar-Sao-Vicente.git
cd Projeto-Lar-Sao-Vicente
```

> Clonar sem o parâmetro `-b` traz a `main`, e o projeto parecerá vazio.

Para descobrir qual instância do SQL Server existe na máquina, no PowerShell:

```powershell
Get-Service | Where-Object { $_.Name -like 'MSSQL*' } | Select-Object Name, Status
```

`MSSQLSERVER` indica instância padrão, usada como `localhost`. `MSSQL$NOME` indica
instância nomeada, usada como `.\NOME`. Se nada aparecer, verifique o LocalDB com
`sqllocaldb info`.

### 2. Configurar a conexão com o banco

Edite `src/LarSaoVicente.Web/appsettings.json` e ajuste o nome do servidor:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.\\SQLEXPRESS;Database=LarSaoVicente;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
}
```

> **Atenção às barras invertidas.** No JSON, a barra invertida inicia uma sequência de
> escape, e `\S` não é válida. O arquivo deixa de ser lido e a aplicação encerra com
> `System.IO.InvalidDataException: Failed to load configuration from file ... appsettings.json`,
> antes mesmo de tentar conectar ao banco.
>
> | | |
> |---|---|
> | ❌ Errado | `"Server=.\SQLEXPRESS;..."` |
> | ✅ Correto | `"Server=.\\SQLEXPRESS;..."` |
>
> A instância padrão dispensa barras: `"Server=localhost;..."`. Se o arquivo quebrar,
> restaure-o com `git checkout src/LarSaoVicente.Web/appsettings.json` e edite novamente.

Para descobrir o nome da sua instância, execute no SQL Server Management Studio:

```sql
SELECT @@SERVERNAME;
```

Exemplos conforme a instalação:

| Instalação | Servidor |
|------------|----------|
| SQL Server Express | `.\SQLEXPRESS` |
| Instância padrão | `localhost` ou `.` |
| Instância nomeada | `.\NOME_DA_INSTANCIA` |
| LocalDB | `(localdb)\MSSQLLocalDB` |

A conexão usa autenticação do Windows (`Trusted_Connection=True`), portanto nenhuma
senha é gravada no repositório. Se precisar de autenticação do SQL Server, **não coloque
a senha no `appsettings.json`** — use o User Secrets:

```bash
cd src/LarSaoVicente.Web
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=...;User Id=...;Password=..."
```

### 3. Definir o usuário administrador

O arquivo `appsettings.Development.json` já traz um administrador para desenvolvimento:

| Campo | Valor |
|-------|-------|
| E-mail | `admin@larsaovicente.local` |
| Senha | `Sprint1@Demo` |

> Credencial exclusiva de desenvolvimento. **Nunca** utilize em produção: defina outra
> pela seção `Seed:Administrator` ou por variáveis de ambiente.

### 4. Executar

```bash
dotnet run --project src/LarSaoVicente.Web
```

O banco de dados é criado automaticamente na primeira execução: as migrations são
aplicadas e o administrador é criado. Em ambiente de desenvolvimento também são criados
cinco residentes fictícios para demonstração.

Acesse o endereço exibido no terminal (por padrão `https://localhost:7xxx`).

### Executar pelo Visual Studio

Abra `LarSaoVicente.sln`, clique com o botão direito em `LarSaoVicente.Web` e escolha
**Definir como projeto de inicialização** — a solução tem quatro projetos, e o Visual
Studio pode selecionar outro. Em seguida, pressione F5.

Na primeira execução o Visual Studio pede para confiar no certificado HTTPS de
desenvolvimento. Aceite: ele vale apenas na máquina local.

A aplicação abre em `https://localhost:7102`.

## Testes

```bash
dotnet test
```

48 testes: 19 unitários das regras de domínio e 29 de integração que exercitam a
aplicação real de ponta a ponta.

Os testes de integração usam **SQLite em memória** e não exigem SQL Server instalado.

## Dados de demonstração

Controlados pela configuração `Seed:DemoData`:

- **Ligado** em `appsettings.Development.json`
- **Desligado** em `appsettings.json` (produção)

Só são criados quando o banco está vazio. Todos os nomes são fictícios e os registros
trazem, nas observações, a marcação "Registro fictício, criado apenas para demonstração
do sistema".

> Nenhum dado real de residentes do Lar São Vicente de Paulo é utilizado em
> desenvolvimento, testes ou demonstrações.

## Comandos úteis

```bash
# Compilar a solução
dotnet build

# Criar uma nova migration
dotnet ef migrations add NomeDaMigration \
  --project src/LarSaoVicente.Infrastructure \
  --startup-project src/LarSaoVicente.Web \
  --output-dir Persistence/Migrations

# Aplicar as migrations manualmente
dotnet ef database update \
  --project src/LarSaoVicente.Infrastructure \
  --startup-project src/LarSaoVicente.Web

# Gerar o script SQL do schema
dotnet ef migrations script --idempotent \
  --project src/LarSaoVicente.Infrastructure \
  --startup-project src/LarSaoVicente.Web \
  --output schema.sql
```

## Estrutura

```
src/
  LarSaoVicente.Domain/           entidades e regras de negócio, sem dependência de framework
  LarSaoVicente.Infrastructure/   Entity Framework, Identity e migrations
  LarSaoVicente.Web/              ASP.NET Core MVC
tests/
  LarSaoVicente.Tests/            testes unitários e de integração
docs/                             documentação do projeto
```

Detalhamento em [`docs/arquitetura.md`](docs/arquitetura.md).

## Metodologia

O desenvolvimento segue **Scrum**, com entregas incrementais validadas pela instituição:

```
Product Backlog → Sprint Planning → Desenvolvimento → Testes
    → Sprint Review → Feedback → Refinamento → próxima Sprint
```

O andamento de cada Sprint é registrado em [`docs/sprints.md`](docs/sprints.md).
