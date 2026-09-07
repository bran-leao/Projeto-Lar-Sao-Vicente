# Contexto do projeto para o Claude Code

Este arquivo preserva o contexto e as regras do projeto entre sessões.
**Leia por completo antes de alterar qualquer coisa.**

## O projeto

Sistema web de gestão para o **Lar São Vicente de Paulo**, uma Instituição de Longa
Permanência para Idosos (ILPI) com cerca de 86 residentes. É um Trabalho de Conclusão de
Curso de Análise e Desenvolvimento de Sistemas, desenvolvido em parceria real com a
instituição.

A principal dor levantada é o **gerenciamento do fluxo de medicamentos**. O sistema
deverá, no futuro, rastrear: entrada → armazenamento → identificação → vinculação ao
residente → separação → administração → registro do responsável.

Documentação completa em `docs/`. Comece por `docs/requisitos.md`.

## Metodologia

O projeto segue **Scrum**, com Sprints que produzem incrementos funcionais e
demonstráveis à instituição.

**Não implemente funcionalidades de Sprints futuras sem autorização explícita.** Cada
Sprint tem escopo acordado. Se identificar algo útil fora do escopo, registre em
`docs/product-backlog.md` e comente com o usuário — não implemente por conta própria.

Sprints de **7 dias**, com entrega aos **sábados**. Calendário em `docs/sprints.md`.

Situação atual: **Sprint 1 concluída** (US01 a US07), entrega em 12/09/2026.

Próxima Sprint: **catálogo de medicamentos com leitura de código de barras** (US09, US23
e US24). A ordem foi repriorizada após a Sprint 1 — ver `docs/product-backlog.md`. O
desenho do módulo está em `docs/arquitetura.md`, seção 5.1: **o sistema não depende de
API externa** para identificar medicamentos; o catálogo se constrói pelo uso.

## Tecnologias — não altere sem justificar

| Item | Versão |
|------|--------|
| .NET | 10.0 (LTS) |
| ASP.NET Core MVC | 10.0 |
| Entity Framework Core | 10.0.11 (provedor SQL Server) |
| SQL Server | 2019+ |
| Bootstrap | 5.3.3, servido localmente |
| xUnit | 2.9.3 |

Restrições acordadas com o usuário:

- **Sem microsserviços.** Monolito em camadas.
- **Sem frameworks adicionais** sem necessidade claramente justificada.
- **JavaScript apenas quando necessário.**
- **Sem CDN.** A instituição pode ter internet instável.
- Evitar *overengineering*. É um projeto acadêmico que crescerá por vários meses.

O `global.json` fixa a versão do SDK.

## Arquitetura

Três projetos, com dependência sempre apontando para dentro:

```
LarSaoVicente.Web  →  LarSaoVicente.Infrastructure  →  LarSaoVicente.Domain
```

**O projeto Domain não referencia Entity Framework nem ASP.NET Core.** Mantenha assim.

Decisões e a razão de cada uma estão em `docs/arquitetura.md`, seção 4. Leia antes de
propor mudanças estruturais.

## Regras que não podem ser violadas

### Dados pessoais e LGPD

- **Nunca use dados reais** de residentes do Lar São Vicente de Paulo, em nenhuma
  circunstância — nem em seeds, testes, exemplos ou documentação.
- Dados de demonstração usam nomes fictícios e trazem marcação explícita nas observações.
- **Minimização de dados:** só colete o que a instituição precisar de fato. A Sprint 1
  deliberadamente não coleta CPF, RG, endereço nem telefone. Não acrescente campos
  pessoais sem necessidade concreta e acordada.

### Segurança

- **Senhas nunca em texto puro.** Use o ASP.NET Core Identity.
- **Nenhuma senha ou segredo no repositório.** Use User Secrets ou variáveis de ambiente.
- **Autorização exigida por padrão.** A política é `FallbackPolicy` com usuário
  autenticado; libere o acesso anônimo apenas com `[AllowAnonymous]` explícito.
- **Use `[FromRoute]`** ao receber identificadores pela rota. No ASP.NET Core o provedor
  de formulário tem precedência sobre o de rota — sem a marcação, um campo oculto
  alterado no navegador sobrescreve o valor da URL. Isso já causou uma falha real neste
  projeto (`docs/sprints.md`, defeito 5).
- Requisições que alteram dados vão por POST, com token antifalsificação.

### Dados e histórico

- **Não exclua fisicamente** registros que precisem de histórico. Use mudança de
  situação (`ResidentStatus.Inativo`).
- Entidades relevantes herdam de `AuditableEntity`. Os campos de auditoria são
  preenchidos por `AppDbContext.SaveChanges` — nunca nos controllers.

### Interface

- Português do Brasil, datas em dd/MM/yyyy, fuso `America/Sao_Paulo`.
- Nomes de rotas e textos visíveis em português; código em inglês, seguindo a convenção
  do .NET.
- Toda listagem precisa tratar o estado vazio.
- Toda operação precisa informar sucesso ou erro ao usuário.

## Convenções de código

- Entidades de domínio têm `private set`; alterações passam por métodos que validam.
- Formulários usam **ViewModels**, nunca entidades diretamente.
- Validação em duas camadas: DataAnnotations no ViewModel (mensagem amigável) e regra no
  domínio (rede de segurança).
- Mapeamentos do EF ficam em `IEntityTypeConfiguration`, fora das entidades.
- Comentários explicam **por quê**, não o quê. Em português.
- Identificadores de entidades expostas em URL são `Guid`.

## Como trabalhar

1. **Compile com frequência:** `dotnet build`
2. **Rode os testes antes de concluir:** `dotnet test` (48 testes devem passar)
3. **Execute a aplicação.** Quatro dos cinco defeitos da Sprint 1 compilavam sem erro e
   só apareceram com o sistema rodando no navegador. Compilar não é verificar.
4. **Corrija a causa raiz.** Não desabilite validações nem esconda erros para fazer algo
   funcionar.
5. **Atualize a documentação** em `docs/` quando alterar comportamento.

### Executar

```bash
dotnet run --project src/LarSaoVicente.Web
```

Requer SQL Server acessível conforme `appsettings.json`. As migrations são aplicadas
automaticamente na inicialização.

### Testar

```bash
dotnet test
```

Os testes de integração hospedam a aplicação real e substituem o SQL Server por SQLite
em memória — não exigem banco instalado.

### Migrations

```bash
dotnet ef migrations add NomeDaMigration \
  --project src/LarSaoVicente.Infrastructure \
  --startup-project src/LarSaoVicente.Web \
  --output-dir Persistence/Migrations
```

## Pontos em aberto

Aguardando confirmação da instituição (ver `docs/requisitos.md`, seção 6):

1. O campo Quarto deve ser sempre obrigatório?
2. Quais perfis de acesso existem e o que cada um pode fazer? Hoje há apenas
   Administrador.
3. Qualquer funcionário pode reativar um residente inativado?
4. Como os quartos são identificados?

Não decida esses pontos sozinho: pergunte ao usuário.

## Git

Branch de desenvolvimento: `claude/tcc-project-96i658`.

Commits em português, no formato `tipo(escopo): descrição`, explicando **por que** a
mudança foi feita.
