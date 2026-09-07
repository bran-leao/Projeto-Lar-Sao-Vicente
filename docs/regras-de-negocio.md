# Regras de negócio

Regras efetivamente implementadas. Cada uma indica onde está aplicada no código.

## 1. Residente

### RN01 — Nome completo obrigatório
O nome é obrigatório e limitado a 150 caracteres. Espaços nas extremidades são removidos
automaticamente, evitando cadastros duplicados por diferença invisível de digitação.

`Resident.SetData` · `ResidentFormViewModel.FullName`

### RN02 — Data de nascimento não pode estar no futuro
A data de hoje é aceita; qualquer data posterior é recusada.

`Resident.SetData` · `ResidentsController.ValidarDatas`

### RN03 — Data de entrada não pode estar no futuro
Mesma regra da RN02, aplicada à data de entrada na instituição.

### RN04 — Data de entrada não pode anteceder o nascimento
Regra de consistência lógica: ninguém entra na instituição antes de nascer.

Não foi solicitada explicitamente pela instituição; foi acrescentada por decorrer
diretamente das duas anteriores. Deve ser confirmada na Sprint Review.

### RN05 — Quarto obrigatório
Obrigatório, até 20 caracteres.

> Assume-se que todo residente possui quarto atribuído no momento do cadastro.
> Ponto a confirmar com a instituição (ver `requisitos.md`, seção 6).

### RN06 — Grau de dependência obrigatório
Deve ser Grau I, Grau II ou Grau III, conforme a classificação usada pelas ILPI:

| Grau | Definição |
|------|-----------|
| Grau I | Idosos independentes, mesmo que requeiram equipamentos de autoajuda |
| Grau II | Dependência em até três atividades de autocuidado |
| Grau III | Dependência que requer assistência em todas as atividades de autocuidado |

### RN07 — Situação inicial Ativo
Todo residente é criado com situação Ativo.

### RN08 — Idade sempre calculada
A idade é derivada da data de nascimento no momento da consulta e **nunca armazenada**.
Um valor gravado ficaria incorreto com a passagem do tempo.

`Resident.GetAgeOn`

### RN09 — Observações limitadas a 1000 caracteres
Campo opcional. A tela orienta a registrar apenas informações necessárias ao cuidado.

## 2. Inativação

### RN10 — Não há exclusão física de residentes
Nenhuma operação do sistema apaga um cadastro de residente. A saída é registrada como
mudança de situação para Inativo.

O histórico precisa ser preservado para consultas futuras e para os módulos de
rastreabilidade de medicamentos previstos no backlog.

`Resident.Deactivate` · `ResidentsController.Inativar`

### RN11 — Residentes inativos permanecem visíveis
Continuam aparecendo na listagem, identificados pela situação. Não são ocultados.

### RN12 — Inativação é idempotente
Inativar um residente já inativo não produz efeito nem gera erro.

### RN13 — Inativação pode ser desfeita
Um residente inativo pode ser reativado.

Implementada como contrapartida da inativação: sem ela, uma inativação feita por engano
não teria como ser corrigida pela interface. A pertinência deve ser confirmada com a
instituição.

## 3. Autenticação e acesso

### RN14 — Não há autocadastro
As contas são criadas pela administração da instituição. Não existe tela de registro
nem recuperação de senha por e-mail.

### RN15 — Senhas nunca em texto puro
Armazenadas como hash PBKDF2 com salt, pelo ASP.NET Core Identity. A senha em texto
puro não é gravada nem devolvida para a interface em nenhuma situação.

### RN16 — Exigência mínima de senha
No mínimo 8 caracteres, com ao menos uma letra maiúscula, uma minúscula e um dígito.

### RN17 — Bloqueio temporário por tentativas
Após 5 tentativas seguidas com senha incorreta, a conta é bloqueada por 15 minutos.

### RN18 — Mensagem de erro não revela existência de conta
Credenciais inválidas produzem sempre a mesma mensagem, independentemente de o e-mail
existir ou não. Diferenciar permitiria descobrir quais e-mails possuem conta.

`AccountController.MensagemCredenciaisInvalidas`

### RN19 — Toda página exige autenticação por padrão
O acesso anônimo precisa ser liberado explicitamente. Aplica-se a tela de login, à
página de erro e aos arquivos estáticos.

### RN20 — Logout apenas por POST
Exposto somente por POST e com token antifalsificação. Como link comum, bastaria um site
externo carregar a URL para desconectar o funcionário sem que ele solicitasse.

## 4. Auditoria

### RN21 — Data e responsável registrados automaticamente
Toda entidade que herda de `AuditableEntity` tem `CreatedAt`, `CreatedByUserId`,
`UpdatedAt` e `UpdatedByUserId` preenchidos pelo contexto de dados durante a gravação.

### RN22 — Informação de criação é imutável
`CreatedAt` e `CreatedByUserId` não são alterados em edições posteriores.

`AppDbContext.ApplyAuditInformation`

### RN23 — Datas de auditoria gravadas em UTC
A conversão para o horário de Brasília ocorre apenas na exibição.

## 5. Proteção de dados pessoais (LGPD)

### RN24 — Minimização de dados
A Sprint 1 coleta apenas o necessário ao atendimento: nome, datas de nascimento e de
entrada, quarto, grau de dependência, situação e observações.

**Não são coletados** CPF, RG, endereço, telefone, filiação ou dados de saúde
detalhados. Cada campo novo deve ser justificado por uma necessidade concreta da
instituição, e não acrescentado por conveniência.

### RN25 — Nenhum dado real em desenvolvimento
Dados reais de residentes do Lar São Vicente de Paulo não são utilizados em
desenvolvimento, testes ou demonstrações.

### RN26 — Dados de demonstração identificados
Os registros criados para demonstração trazem, nas observações, a marcação
"Registro fictício, criado apenas para demonstração do sistema".

### RN27 — Demonstração desligada por padrão
A carga de dados fictícios só ocorre quando `Seed:DemoData` está habilitado e o banco
está vazio. Em produção permanece desligada, para que registros fictícios nunca se
misturem aos reais nem distorçam os indicadores.

### RN28 — Identificadores não sequenciais
Residentes são identificados por GUID, de modo que as URLs não permitam descobrir quantos
residentes existem nem percorrer os cadastros por tentativa e erro.

## 6. Interface

### RN29 — Idioma e formatos brasileiros
Interface em português do Brasil, datas exibidas em dd/MM/yyyy e fuso de referência
America/Sao_Paulo.

O formato de exibição é definido pelo servidor e não varia conforme o idioma do
navegador do funcionário.

### RN30 — Indicadores refletem apenas dados reais
O Dashboard apresenta somente contagens obtidas do banco. Nenhum número é estimado,
projetado ou preenchido artificialmente.
