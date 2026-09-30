/* ============================================================================
   Sistema de Gestão do Lar São Vicente de Paulo
   Consultas para demonstrar o banco de dados

   Abra este arquivo com o banco LarSaoVicente selecionado, selecione UMA
   consulta por vez e pressione Ctrl+Shift+E (ou F5, se nada estiver
   selecionado, roda o arquivo inteiro).

   Rode o sistema pelo menos uma vez antes (F5 no Visual Studio), para que o
   banco exista e os dados de demonstração sejam criados.

   Os dados são fictícios. Nenhum dado real de residente do Lar São Vicente
   de Paulo é utilizado em nenhuma circunstância.
   ============================================================================ */


/* ----------------------------------------------------------------------------
   1. AS TABELAS QUE EXISTEM NESTE BANCO

   Prova que a estrutura está criada. Devem aparecer 11 linhas: as 3 tabelas
   projetadas pela equipe, as 7 do ASP.NET Core Identity e a de controle das
   migrations.
   ---------------------------------------------------------------------------- */

SELECT
    TABLE_NAME                                    AS Tabela,
    (SELECT COUNT(*)
       FROM INFORMATION_SCHEMA.COLUMNS c
      WHERE c.TABLE_NAME = t.TABLE_NAME)          AS QtdColunas
FROM INFORMATION_SCHEMA.TABLES t
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY Tabela;


/* ----------------------------------------------------------------------------
   2. QUAIS MIGRATIONS JÁ FORAM APLICADAS

   O banco não foi criado à mão: foi montado a partir das migrations
   versionadas no repositório. Esta tabela é o registro disso.
   ---------------------------------------------------------------------------- */

SELECT MigrationId AS Migration, ProductVersion AS VersaoDoEFCore
FROM __EFMigrationsHistory
ORDER BY MigrationId;


/* ----------------------------------------------------------------------------
   3. O CATÁLOGO DE MEDICAMENTOS

   Repare em duas coisas:
   - Macrodantina tem o princípio ativo Nitrofurantoína. Nas planilhas da
     instituição esses dois nomes aparecem soltos, sem nada relacionando um ao
     outro. Aqui é o mesmo registro.
   - A luva de procedimento é categoria Insumo e não tem princípio ativo. Está
     correto: luva não é fármaco.
   ---------------------------------------------------------------------------- */

SELECT
    CommercialName                    AS Medicamento,
    ISNULL(Strength, '—')             AS Concentracao,
    ISNULL(ActiveIngredient, '—')     AS PrincipioAtivo,
    CASE Category WHEN 1 THEN 'Medicamento' WHEN 2 THEN 'Insumo' END AS Categoria,
    CASE PackageUnit
        WHEN 1 THEN 'Caixa'   WHEN 2 THEN 'Frasco'  WHEN 3 THEN 'Tubo'
        WHEN 4 THEN 'Sachê'   WHEN 5 THEN 'Fardo'   WHEN 6 THEN 'Caixa master'
        WHEN 7 THEN 'Unidade' WHEN 8 THEN 'Envelope' WHEN 9 THEN 'Ampola'
    END                               AS Embalagem,
    ISNULL(Barcode, 'sem código')     AS CodigoDeBarras,
    CASE Status WHEN 1 THEN 'Ativo' WHEN 2 THEN 'Inativo' END AS Situacao
FROM Medicamentos
ORDER BY Status, CommercialName;


/* ----------------------------------------------------------------------------
   4. O RELACIONAMENTO: CADA ENTRADA APONTA PARA UM MEDICAMENTO

   É a única chave estrangeira declarada no banco inteiro. O JOIN abaixo é o
   que ela permite.

   Repare na linha da Dipirona: quantidade 11 em UM registro. Foi pedido pela
   enfermagem — se chegam onze envelopes, ninguém preenche o formulário onze
   vezes. A quantidade pertence à entrada, nunca ao catálogo.
   ---------------------------------------------------------------------------- */

SELECT
    m.CommercialName                  AS Medicamento,
    e.Quantity                        AS Quantidade,
    ISNULL(e.LotNumber, '—')          AS Lote,
    CASE
        WHEN e.ExpiryNotIdentified = 1 THEN 'NÃO IDENTIFICADA'
        WHEN e.ExpiryDate < CAST(GETDATE() AS date) THEN 'VENCIDO em ' + FORMAT(e.ExpiryDate, 'dd/MM/yyyy')
        ELSE FORMAT(e.ExpiryDate, 'dd/MM/yyyy')
    END                               AS Validade,
    CASE e.Origin
        WHEN 1 THEN 'Distribuidora'  WHEN 2 THEN 'Farmácia Popular'
        WHEN 3 THEN 'Posto de saúde' WHEN 4 THEN 'Família do residente'
        WHEN 5 THEN 'Doação'
    END                               AS Origem,
    CASE e.Status
        WHEN 1 THEN 'Aguardando conferência'
        WHEN 2 THEN 'Conferida'
        WHEN 3 THEN 'Recusada'
    END                               AS Situacao,
    CASE e.IdentifiedByScan WHEN 1 THEN 'Leitura do código' ELSE 'Digitada' END AS Como
FROM EntradasDeMedicamento e
INNER JOIN Medicamentos m ON m.Id = e.MedicationId
ORDER BY e.ReceivedOn DESC;


/* ----------------------------------------------------------------------------
   5. O ESTOQUE — A CONSULTA MAIS IMPORTANTE DA DEMONSTRAÇÃO

   Não existe coluna "estoque" em lugar nenhum do banco. O estoque É esta
   consulta: a soma das entradas cuja situação é Conferida.

   Por que assim? A alternativa seria mover o item de uma área de triagem para
   o estoque depois da revisão. Uma transferência pode falhar pela metade,
   duplica o registro e pode ser esquecida. Aqui nada se move: a entrada
   pendente simplesmente não é somada.
   ---------------------------------------------------------------------------- */

SELECT
    m.CommercialName                                              AS Medicamento,
    ISNULL(m.Strength, '')                                        AS Concentracao,
    SUM(CASE WHEN e.Status = 2 THEN e.Quantity ELSE 0 END)        AS EmEstoque,
    SUM(CASE WHEN e.Status = 1 THEN e.Quantity ELSE 0 END)        AS AguardandoConferencia,
    SUM(CASE WHEN e.Status = 3 THEN e.Quantity ELSE 0 END)        AS Recusado
FROM Medicamentos m
LEFT JOIN EntradasDeMedicamento e ON e.MedicationId = m.Id
WHERE m.Status = 1
GROUP BY m.CommercialName, m.Strength
ORDER BY EmEstoque DESC, m.CommercialName;


/* ----------------------------------------------------------------------------
   6. O QUE ESTÁ AGUARDANDO CONFERÊNCIA, POR ORDEM DE RISCO

   Validade desconhecida e lote vencido aparecem primeiro. É o principal risco
   em medicamento recebido por doação, e hoje a instituição não tem como
   descobrir isso sem abrir caixa por caixa.
   ---------------------------------------------------------------------------- */

SELECT
    m.CommercialName                  AS Medicamento,
    e.Quantity                        AS Quantidade,
    CASE
        WHEN e.ExpiryNotIdentified = 1 THEN 'Validade não identificada'
        WHEN e.ExpiryDate < CAST(GETDATE() AS date) THEN 'Lote vencido'
        ELSE 'Dentro da validade'
    END                               AS Risco,
    ISNULL(e.Notes, '')               AS Observacoes
FROM EntradasDeMedicamento e
INNER JOIN Medicamentos m ON m.Id = e.MedicationId
WHERE e.Status = 1
ORDER BY
    CASE
        WHEN e.ExpiryNotIdentified = 1 THEN 1
        WHEN e.ExpiryDate < CAST(GETDATE() AS date) THEN 2
        ELSE 3
    END,
    e.ExpiryDate;


/* ----------------------------------------------------------------------------
   7. OS RESIDENTES

   Note o que NÃO existe aqui: CPF, RG, endereço e telefone. A ausência é
   deliberada — princípio da minimização de dados da LGPD. Só se coleta o que
   a instituição pediu.

   A idade também não é uma coluna: é calculada a partir da data de
   nascimento, para nunca existir um valor que fique errado com o tempo.
   ---------------------------------------------------------------------------- */

SELECT
    FullName                                                   AS Residente,
    FORMAT(BirthDate, 'dd/MM/yyyy')                            AS Nascimento,
    DATEDIFF(YEAR, BirthDate, GETDATE())
        - CASE WHEN DATEADD(YEAR, DATEDIFF(YEAR, BirthDate, GETDATE()), BirthDate)
                    > CAST(GETDATE() AS date)
               THEN 1 ELSE 0 END                               AS Idade,
    Room                                                       AS Quarto,
    CASE DependencyLevel
        WHEN 1 THEN 'Grau I' WHEN 2 THEN 'Grau II' WHEN 3 THEN 'Grau III'
    END                                                        AS GrauDeDependencia,
    CASE Status WHEN 1 THEN 'Ativo' WHEN 2 THEN 'Inativo' END  AS Situacao
FROM Residentes
ORDER BY Status, FullName;


/* ----------------------------------------------------------------------------
   8. A AUDITORIA SE PREENCHE SOZINHA

   Nenhum controller escreve nestas colunas. Quem preenche é o SaveChanges do
   contexto de dados, percorrendo tudo que foi alterado. Assim nenhuma
   gravação escapa da auditoria, mesmo que um programador futuro esqueça.

   As datas são gravadas em UTC; a conversão para o horário de Brasília
   acontece só na exibição.
   ---------------------------------------------------------------------------- */

SELECT
    m.CommercialName                                  AS Medicamento,
    FORMAT(m.CreatedAt, 'dd/MM/yyyy HH:mm')           AS CriadoEmUTC,
    ISNULL(u.Email, '(sistema)')                      AS CriadoPor,
    ISNULL(FORMAT(m.UpdatedAt, 'dd/MM/yyyy HH:mm'), '—') AS AlteradoEmUTC
FROM Medicamentos m
LEFT JOIN AspNetUsers u ON u.Id = m.CreatedByUserId
ORDER BY m.CreatedAt;


/* ----------------------------------------------------------------------------
   9. A SENHA NUNCA É GUARDADA EM TEXTO PURO

   O que existe é o hash PBKDF2 com salt, produzido pelo ASP.NET Core
   Identity. Dele não se volta à senha original. Se o banco inteiro vazasse,
   as senhas continuariam protegidas.
   ---------------------------------------------------------------------------- */

SELECT
    Email                                       AS Usuario,
    LEFT(PasswordHash, 45) + '...'              AS HashDaSenha,
    LEN(PasswordHash)                           AS TamanhoDoHash,
    AccessFailedCount                           AS TentativasIncorretas,
    LockoutEnabled                              AS BloqueioHabilitado
FROM AspNetUsers;


/* ----------------------------------------------------------------------------
   10. OS ÍNDICES, E O DETALHE DO CÓDIGO DE BARRAS

   Repare na coluna Filtro, na linha de IX_Medicamentos_Barcode.

   No SQL Server, um índice único comum trata TODOS os nulos como iguais e
   aceitaria apenas um item sem código de barras. Como a maior parte das
   cartelas recebidas por doação não tem código algum, isso inviabilizaria o
   cadastro. O filtro restringe a unicidade às linhas que de fato têm código.
   ---------------------------------------------------------------------------- */

SELECT
    t.name                                        AS Tabela,
    i.name                                        AS Indice,
    CASE i.is_unique WHEN 1 THEN 'Sim' ELSE 'Não' END AS Unico,
    ISNULL(i.filter_definition, '—')              AS Filtro
FROM sys.indexes i
INNER JOIN sys.tables t ON t.object_id = i.object_id
WHERE t.name IN ('Medicamentos', 'EntradasDeMedicamento', 'Residentes')
  AND i.name IS NOT NULL
ORDER BY t.name, i.name;


/* ----------------------------------------------------------------------------
   11. A CHAVE ESTRANGEIRA E A REGRA DE EXCLUSÃO

   ON DELETE NO_ACTION, e não CASCADE. Apagar um item do catálogo não pode
   levar junto o histórico de tudo que entrou por ele — seria destruir
   exatamente o que o sistema existe para manter. O item é inativado, nunca
   excluído.
   ---------------------------------------------------------------------------- */

SELECT
    fk.name                                       AS ChaveEstrangeira,
    OBJECT_NAME(fk.parent_object_id)              AS TabelaFilha,
    OBJECT_NAME(fk.referenced_object_id)          AS TabelaPai,
    fk.delete_referential_action_desc             AS AoExcluirOPai
FROM sys.foreign_keys fk
WHERE OBJECT_NAME(fk.parent_object_id) = 'EntradasDeMedicamento';


/* ----------------------------------------------------------------------------
   12. PROVA DE QUE O ÍNDICE FILTRADO FUNCIONA  (opcional — altera dados)

   Descomente e execute para demonstrar ao vivo. A primeira parte grava dois
   itens SEM código de barras e os dois são aceitos. A segunda tenta gravar um
   código repetido e o banco recusa.

   O ROLLBACK ao final desfaz tudo: nada fica gravado.
   ---------------------------------------------------------------------------- */

/*
BEGIN TRANSACTION;

    INSERT INTO Medicamentos
        (Id, CommercialName, ActiveIngredient, PackageUnit, Category, Status,
         NormalizedSearchKey, CreatedAt)
    VALUES
        (NEWID(), 'Teste sem código A', 'Princípio de teste', 1, 1, 1, 'TESTESEMCODIGOA', GETUTCDATE()),
        (NEWID(), 'Teste sem código B', 'Princípio de teste', 1, 1, 1, 'TESTESEMCODIGOB', GETUTCDATE());

    -- Os dois foram aceitos, mesmo com Barcode nulo nos dois.
    SELECT CommercialName, ISNULL(Barcode, 'NULO') AS CodigoDeBarras
    FROM Medicamentos
    WHERE CommercialName LIKE 'Teste sem código%';

    -- Agora um código repetido: o banco recusa com violação de índice único.
    INSERT INTO Medicamentos
        (Id, CommercialName, ActiveIngredient, Barcode, PackageUnit, Category, Status,
         NormalizedSearchKey, CreatedAt)
    VALUES
        (NEWID(), 'Duplicado', 'Princípio de teste', '07891000315507', 1, 1, 1, 'DUPLICADO', GETUTCDATE());

ROLLBACK TRANSACTION;
*/
