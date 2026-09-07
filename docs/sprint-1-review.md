# Sprint Review — Sprint 1

Documento de apoio para a reunião de revisão com o Lar São Vicente de Paulo.

**Sprint Goal:** disponibilizar a primeira versão funcional do sistema, permitindo
acesso autenticado e cadastro, consulta e manutenção básica dos residentes.

| | |
|---|---|
| Data | sábado, 12/09/2026 |
| Participantes | Equipe de desenvolvimento, representantes da instituição, orientador |
| Duração sugerida | 1 hora |
| Demonstração | 15 minutos |
| Feedback e discussão | 30 minutos |
| Encerramento e próximos passos | 15 minutos |

> **Antes de tudo:** a Sprint Review não é uma apresentação de slides, é uma
> **demonstração do software funcionando**. O objetivo principal não é mostrar o que foi
> feito, e sim **colher feedback** para o refinamento do backlog. Reserve mais tempo
> para ouvir do que para falar.

---

## 1. Preparação — fazer na véspera, não no dia

Demonstração que falha na frente do cliente custa credibilidade. Execute o roteiro
inteiro uma vez antes da reunião.

### Verificações

- [ ] SQL Server em execução na máquina que fará a demonstração
- [ ] `dotnet run --project src/LarSaoVicente.Web` sobe sem erro
- [ ] Login funciona com `admin@larsaovicente.local` / `Sprint1@Demo`
- [ ] Os 5 residentes de demonstração aparecem na listagem
- [ ] Roteiro da seção 2 percorrido do início ao fim, sem improviso
- [ ] Navegador em tela cheia, zoom em 100%, abas pessoais fechadas
- [ ] Se for projetar: testar o projetor e aumentar o zoom para 125% ou 150%

### Restaurar os dados de demonstração

Se você já mexeu nos dados durante os testes, apague o banco e execute novamente. Os
5 residentes fictícios são recriados automaticamente:

```sql
-- No SQL Server Management Studio
ALTER DATABASE LarSaoVicente SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
DROP DATABASE LarSaoVicente;
```

Em seguida, `dotnet run` recria tudo: estrutura, administrador e dados de demonstração.

### Estado esperado ao iniciar

| Nome | Idade | Quarto | Grau | Situação |
|------|-------|--------|------|----------|
| Antônio Souza | 89 | 102 | Grau II | Ativo |
| José Ferreira | 82 | 205 | Grau III | Ativo |
| Maria Oliveira | 86 | 101 | Grau I | Ativo |
| Terezinha Ramos | 78 | 206 | Grau II | Ativo |
| Sebastião Lima | 87 | 103 | Grau I | Inativo |

Dashboard: **5** cadastrados · **4** ativos · **1** inativo · Grau I **1** · Grau II **2** · Grau III **1**

> As idades são calculadas na data da consulta, então podem diferir em um ano conforme
> o dia da reunião. É justamente isso que se quer demonstrar: a idade nunca fica
> desatualizada.

---

## 2. Roteiro da demonstração

Sequência pensada para contar uma história: primeiro o sistema protege, depois informa,
depois permite trabalhar. Cada passo traz o que fazer e o ponto a comentar.

### Abertura (1 min)

> "Esta é a primeira entrega. Ela cobre o acesso ao sistema e o cadastro dos residentes.
> Ainda não há medicamentos nem estoque — isso vem nas próximas etapas. Começamos por
> aqui porque tudo o mais depende de saber quem são os residentes e quem está usando o
> sistema."

---

### Passo 1 — O sistema protege as informações

**Faça:** com o navegador em uma aba anônima, digite o endereço de `/Residentes`.

**Acontece:** o sistema desvia para a tela de login.

> "Nenhuma informação de residente aparece sem identificação. Isso vale para qualquer
> tela do sistema, inclusive as que ainda serão criadas."

---

### Passo 2 — Senha incorreta

**Faça:** informe o e-mail correto e uma senha qualquer errada.

**Acontece:** mensagem *"E-mail ou senha inválidos."*

> "A mensagem é a mesma se o e-mail não existir. É proposital: quem tentar adivinhar
> não descobre quais e-mails têm acesso. E após cinco tentativas seguidas a conta é
> bloqueada por 15 minutos."

---

### Passo 3 — Entrar no sistema

**Faça:** informe a senha correta.

**Acontece:** vai para o Dashboard, com o nome do usuário no canto superior direito.

---

### Passo 4 — Dashboard

**Faça:** apresente os números.

> "Cinco residentes cadastrados, quatro ativos, um inativo. Abaixo, a distribuição por
> grau de dependência, contando apenas os ativos. **Todos esses números vêm do banco de
> dados** — nada aqui é estimado. Se cadastrarmos alguém agora, o número muda."

*(Guarde esta frase: no passo 9 você volta aqui e mostra o número tendo mudado.)*

---

### Passo 5 — Listagem e pesquisa

**Faça:** clique em **Residentes**. Mostre a tabela. Depois pesquise por `souza`.

**Acontece:** a lista filtra para Antônio Souza.

**Faça:** pesquise por algo inexistente, como `abc`.

**Acontece:** mensagem *"Nenhum residente encontrado"* com a opção de limpar a pesquisa.

> "Com 86 residentes, a pesquisa pelo nome é o caminho mais rápido. Repare que os ativos
> aparecem primeiro, e o inativo continua na lista, apenas identificado — ele não some."

---

### Passo 6 — Cadastro, mostrando as validações

Este é o passo mais importante da demonstração. Mostre primeiro o **erro**, depois o
acerto — é o que dá confiança de que o sistema não aceita informação inconsistente.

**Faça:** clique em **Novo residente** e, sem preencher nada, clique em **Cadastrar residente**.

**Acontece:** cinco mensagens, uma sob cada campo obrigatório.

> "O sistema aponta todos os campos obrigatórios de uma vez, e não um por vez."

**Faça:** preencha os dados abaixo, mas coloque a **data de entrada em uma data futura**
(por exemplo, o ano que vem):

| Campo | Valor |
|-------|-------|
| Nome completo | Joana Prestes |
| Data de nascimento | 14/05/1943 |
| Data de entrada | *(uma data futura)* |
| Quarto | 301 |
| Grau de dependência | Grau II |

**Acontece:** *"A data de entrada não pode estar no futuro."*

> "Datas impossíveis são recusadas. Vale para o nascimento e para a entrada."

**Faça:** corrija a data de entrada para `02/11/2024` e cadastre.

**Acontece:** mensagem verde de sucesso e o sistema abre o perfil da residente.

---

### Passo 7 — Perfil do residente

**Faça:** percorra a tela.

> "A idade aparece ao lado da data de nascimento e é **calculada**, nunca digitada — não
> tem como ficar desatualizada."
>
> "Estes três blocos — Medicamentos, Agenda e Ocorrências — estão marcados como próximas
> etapas. Deixamos o espaço reservado para vocês verem onde essas informações vão
> aparecer, mas ainda não há nada neles."
>
> "No rodapé fica o registro de quando o cadastro foi feito. Toda alteração no sistema
> guarda quem fez e quando — é a base do controle de responsável que vocês pediram para
> os medicamentos."

---

### Passo 8 — Edição

**Faça:** clique em **Editar**. Mude o quarto de `301` para `404` e o grau de `Grau II`
para `Grau III`. Salve.

**Acontece:** perfil atualizado; o rodapé agora mostra também *"última alteração em..."*.

> "As mesmas validações do cadastro valem aqui."

---

### Passo 9 — Inativação

**Faça:** clique em **Inativar** e confirme.

**Acontece:** a situação muda para Inativo; o botão passa a ser **Reativar**.

> "Este é um ponto que discutimos: quando um residente deixa a instituição, **o cadastro
> não é apagado**. Ele muda de situação. Todo o histórico continua disponível — e vai
> continuar quando houver registro de medicamentos administrados."

**Faça:** volte a **Residentes** e mostre que Joana continua na lista, marcada como Inativa.

**Faça:** volte ao **Dashboard**.

**Acontece:** agora são **6** cadastrados, **4** ativos, **2** inativos.

> "Os números acompanharam o que acabamos de fazer."

*(Se quiser, clique em Reativar para mostrar que a operação pode ser desfeita — e
aproveite para fazer a pergunta 3 da seção 3.)*

---

### Passo 10 — Uso no celular

**Faça:** reduza a janela do navegador até ficar estreita, ou abra no celular.

**Acontece:** o menu vira um botão; a tabela se ajusta.

> "Pensando em quem circula pela instituição e não fica no computador."

---

### Fechamento da demonstração (1 min)

> "Sobre os dados: os nomes que vocês viram são **fictícios**, criados por nós para a
> demonstração. Nenhuma informação real de residentes foi usada no desenvolvimento, e
> não será — inclusive porque o sistema vai tratar dados de saúde, que a LGPD protege de
> forma mais rigorosa."

---

## 3. Perguntas para a instituição

O ponto central da Review. Faça as perguntas e **anote as respostas** — elas alimentam o
refinamento do backlog.

### Sobre o que foi demonstrado

1. **Quarto obrigatório.** Hoje o sistema exige informar o quarto no cadastro. Existe
   situação em que um residente é cadastrado antes de ter quarto definido?
2. **Identificação dos quartos.** Como vocês os identificam: só número, número com
   letra, nome de ala? O sistema aceita texto livre, mas convém seguir o padrão de vocês.
3. **Reativação.** Qualquer funcionário pode reativar um residente inativado, ou isso
   deveria ser restrito a determinadas pessoas?
4. **Campos do cadastro.** Falta alguma informação essencial sobre o residente para o
   trabalho do dia a dia?
5. **Filtro por situação.** Faria diferença poder filtrar a lista para ver só os ativos?

### Sobre a próxima etapa

6. **Funções na instituição.** Quais funções existem — enfermagem, cuidadores,
   administração, cozinha? Quantas pessoas em cada uma?
7. **Permissões.** O que cada função deve poder fazer no sistema? Especificamente: quem
   pode registrar a administração de um medicamento?
8. **Contas de acesso.** Cada funcionário terá conta individual, ou há postos de trabalho
   compartilhados? *(Isso afeta diretamente o registro de responsável.)*

### Sobre o módulo de medicamentos

Acrescentadas após a repriorização: a Sprint 2 passou a ser o catálogo de medicamentos,
e não o cadastro de usuários.

9. **As caixas que vocês recebem têm o quadradinho 2D (DataMatrix), ou só o código de
   barras tradicional?** Peça para olharem algumas caixas na despensa, de compra e de
   doação.
10. **Como chega uma doação?** Caixa lacrada, cartela avulsa, frasco já aberto?
11. **Vocês controlam lote e validade hoje?** Em papel, planilha, ou não há controle?
12. **Quem recebe a doação fisicamente** é a mesma pessoa que administra o medicamento?
13. **A instituição tem farmacêutico responsável?**
14. **Quem pode liberar um medicamento para uso?** Todo remédio registrado à mão vai
    ficar aguardando conferência antes de contar no estoque. Quem tem competência para
    conferir: qualquer funcionário, a enfermagem, o farmacêutico?
15. **Caixa lacrada também passa por conferência,** ou pode ir direto para o estoque?

> Duas perguntas decidem o rumo do trabalho. A **7** continua sendo a mais importante do
> projeto: sem saber quem pode fazer o quê, não há como registrar quem administrou cada
> dose. A **9** define o desenho da Sprint 3: se as caixas trouxerem DataMatrix, lote e
> validade vêm de graça na leitura; se não, terão de ser digitados a cada entrada.

---

## 4. Registro do feedback

Preencher **durante** a reunião. Este registro é a entrada do refinamento do backlog e
serve como evidência do processo no TCC.

| # | Quem falou | Observação / solicitação | Tipo | Encaminhamento |
|---|-----------|--------------------------|------|----------------|
| 1 | | | Ajuste / Novo requisito / Dúvida | |
| 2 | | | | |
| 3 | | | | |
| 4 | | | | |
| 5 | | | | |

**Tipo:** *Ajuste* (muda algo já entregue) · *Novo requisito* (vira item de backlog) ·
*Dúvida* (precisa de mais informação)

### Respostas às perguntas da seção 3

| Pergunta | Resposta |
|----------|----------|
| 1. Quarto obrigatório? | |
| 2. Como os quartos são identificados? | |
| 3. Quem pode reativar? | |
| 4. Falta algum campo? | |
| 5. Filtro por situação? | |
| 6. Quais funções existem? | |
| 7. Permissões por função? | |
| 8. Contas individuais ou compartilhadas? | |

---

## 5. Encerramento

**Recapitule** o que foi acordado, para confirmar que todos entenderam o mesmo:

> "Então, ficou combinado que [...]. Na próxima etapa vamos trabalhar em [...]."

**Anuncie a próxima Sprint.** A sugestão da equipe é **cadastro de usuários e perfis de
acesso** (US08), justamente porque é o que destrava o controle de medicamentos:

> "Para registrar quem administrou cada medicamento, o sistema precisa primeiro saber
> quem são as pessoas e o que cada uma pode fazer. Por isso a próxima etapa é o cadastro
> de usuários. Depende das respostas que vocês nos derem hoje sobre as funções."

**Combine a próxima reunião.**

---

## 6. Depois da reunião

- [ ] Passar o feedback da seção 4 para `docs/product-backlog.md`
- [ ] Atualizar `docs/requisitos.md` seção 6 com os pontos que foram esclarecidos
- [ ] Registrar o resultado da Review em `docs/sprints.md`
- [ ] Repriorizar o backlog conforme o que a instituição indicou
- [ ] Fazer o Sprint Planning da Sprint 2

---

## Anexo — se algo der errado na demonstração

| Problema | O que fazer |
|----------|-------------|
| A aplicação não sobe | Verifique se o SQL Server está em execução e se o servidor em `appsettings.json` está correto |
| Login não funciona | Confirme e-mail e senha em `appsettings.Development.json`, seção `Seed:Administrator` |
| Não aparecem os residentes de demonstração | Só são criados com o banco vazio. Apague o banco (seção 1) e execute novamente |
| Erro em alguma tela | Não tente consertar na hora. Anote, siga para o próximo passo e trate depois |

> Se algo falhar, seja direto: "isso aqui não está funcionando como esperado, vou anotar
> e corrigir". Transparência sobre um defeito custa menos que uma tentativa de disfarçar
> — e, em Scrum, o defeito encontrado vira item de backlog, o que é o funcionamento
> normal do processo.
