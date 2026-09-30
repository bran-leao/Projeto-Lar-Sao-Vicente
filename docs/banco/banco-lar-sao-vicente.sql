/* ============================================================================
   Sistema de Gestão do Lar São Vicente de Paulo
   Instituição de Longa Permanência para Idosos

   Script de criação do banco de dados — gerado a partir das migrations do
   Entity Framework Core 10, em 30/09/2026.

   ----------------------------------------------------------------------------
   O QUE ESTE ARQUIVO É

   Este script NÃO foi escrito à mão. Ele é gerado a partir das duas migrations
   versionadas no repositório:

       20260907141652_CriacaoInicial
       20260915230122_CatalogoDeMedicamentos

   As migrations são a fonte da verdade da estrutura do banco. Em qualquer
   máquina onde o sistema rodar, ele lê essas migrations e monta a estrutura
   idêntica, sozinho, na inicialização. Este arquivo existe para consulta, para
   documentação e para o caso de alguém preferir criar o banco manualmente.

   O script é IDEMPOTENTE: pode ser executado mais de uma vez sem erro. Cada
   bloco verifica antes, na tabela __EFMigrationsHistory, se aquela migration
   já foi aplicada.

   ----------------------------------------------------------------------------
   COMO EXECUTAR

   No Visual Studio: Exibir > Pesquisador de Objetos do SQL Server, clique com o
   botão direito no banco, Nova Consulta, cole este arquivo e execute.

   Pela linha de comando:
       sqlcmd -S "(localdb)\MSSQLLocalDB" -d LarSaoVicente -i banco-lar-sao-vicente.sql

   ----------------------------------------------------------------------------
   AS 10 TABELAS

   Projetadas pela equipe:
       Residentes ................ residentes atendidos pela instituição
       Medicamentos .............. catálogo de medicamentos e insumos
       EntradasDeMedicamento ..... remessas recebidas, com lote e validade

   Do ASP.NET Core Identity (usuários, senhas e perfis de acesso):
       AspNetUsers, AspNetRoles, AspNetUserRoles, AspNetUserClaims,
       AspNetRoleClaims, AspNetUserLogins, AspNetUserTokens

   Mais a tabela de controle __EFMigrationsHistory, que registra quais
   migrations já foram aplicadas neste banco.

   ----------------------------------------------------------------------------
   DETALHES QUE VALE CONHECER

   * Chaves primárias em uniqueidentifier (GUID), e não em inteiro sequencial,
     para que as URLs do sistema não permitam descobrir quantos residentes
     existem nem percorrer os cadastros por tentativa e erro.

   * IX_Medicamentos_Barcode é um índice ÚNICO COM FILTRO. No SQL Server um
     índice único comum trata todos os NULL como iguais e aceitaria apenas um
     item sem código de barras — e a maior parte das cartelas de doação não tem
     código algum. O filtro restringe a unicidade às linhas que de fato o têm.

   * A chave estrangeira de EntradasDeMedicamento usa ON DELETE NO ACTION.
     Apagar um item do catálogo não pode levar junto o histórico de tudo que
     entrou por ele: o item é inativado, nunca excluído.

   * Os campos CreatedByUserId e UpdatedByUserId guardam o identificador do
     usuário SEM chave estrangeira declarada, de propósito. Assim o histórico
     permanece legível mesmo que uma conta seja removida no futuro.

   * Datas de auditoria (datetime2) são gravadas em UTC. A conversão para o
     fuso America/Sao_Paulo acontece apenas na exibição.

   ============================================================================ */

IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907141652_CriacaoInicial'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907141652_CriacaoInicial'
)
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] nvarchar(450) NOT NULL,
        [FullName] nvarchar(max) NOT NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907141652_CriacaoInicial'
)
BEGIN
    CREATE TABLE [Residentes] (
        [Id] uniqueidentifier NOT NULL,
        [FullName] nvarchar(150) NOT NULL,
        [BirthDate] date NOT NULL,
        [AdmissionDate] date NOT NULL,
        [Room] nvarchar(20) NOT NULL,
        [DependencyLevel] int NOT NULL,
        [Status] int NOT NULL,
        [Notes] nvarchar(1000) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByUserId] nvarchar(450) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedByUserId] nvarchar(450) NULL,
        CONSTRAINT [PK_Residentes] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907141652_CriacaoInicial'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907141652_CriacaoInicial'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907141652_CriacaoInicial'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907141652_CriacaoInicial'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907141652_CriacaoInicial'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] nvarchar(450) NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907141652_CriacaoInicial'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907141652_CriacaoInicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907141652_CriacaoInicial'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907141652_CriacaoInicial'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907141652_CriacaoInicial'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907141652_CriacaoInicial'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907141652_CriacaoInicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907141652_CriacaoInicial'
)
BEGIN
    CREATE INDEX [IX_Residentes_FullName] ON [Residentes] ([FullName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907141652_CriacaoInicial'
)
BEGIN
    CREATE INDEX [IX_Residentes_Status] ON [Residentes] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907141652_CriacaoInicial'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260907141652_CriacaoInicial', N'10.0.11');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915230122_CatalogoDeMedicamentos'
)
BEGIN
    CREATE TABLE [Medicamentos] (
        [Id] uniqueidentifier NOT NULL,
        [CommercialName] nvarchar(150) NOT NULL,
        [ActiveIngredient] nvarchar(150) NULL,
        [Strength] nvarchar(60) NULL,
        [Form] int NULL,
        [UnitsPerPackage] int NULL,
        [PackageUnit] int NOT NULL,
        [Barcode] nvarchar(14) NULL,
        [Category] int NOT NULL,
        [Status] int NOT NULL,
        [Notes] nvarchar(1000) NULL,
        [NormalizedSearchKey] nvarchar(300) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByUserId] nvarchar(450) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedByUserId] nvarchar(450) NULL,
        CONSTRAINT [PK_Medicamentos] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915230122_CatalogoDeMedicamentos'
)
BEGIN
    CREATE TABLE [EntradasDeMedicamento] (
        [Id] uniqueidentifier NOT NULL,
        [MedicationId] uniqueidentifier NOT NULL,
        [Quantity] int NOT NULL,
        [LotNumber] nvarchar(20) NULL,
        [ExpiryDate] date NULL,
        [ExpiryNotIdentified] bit NOT NULL,
        [Origin] int NOT NULL,
        [ReceivedOn] date NOT NULL,
        [IdentifiedByScan] bit NOT NULL,
        [SerialNumber] nvarchar(20) NULL,
        [Status] int NOT NULL,
        [RejectionReason] nvarchar(500) NULL,
        [ReviewedAt] datetime2 NULL,
        [ReviewedByUserId] nvarchar(450) NULL,
        [Notes] nvarchar(1000) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByUserId] nvarchar(450) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedByUserId] nvarchar(450) NULL,
        CONSTRAINT [PK_EntradasDeMedicamento] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EntradasDeMedicamento_Medicamentos_MedicationId] FOREIGN KEY ([MedicationId]) REFERENCES [Medicamentos] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915230122_CatalogoDeMedicamentos'
)
BEGIN
    CREATE INDEX [IX_EntradasDeMedicamento_ExpiryDate] ON [EntradasDeMedicamento] ([ExpiryDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915230122_CatalogoDeMedicamentos'
)
BEGIN
    CREATE INDEX [IX_EntradasDeMedicamento_MedicationId_Status] ON [EntradasDeMedicamento] ([MedicationId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915230122_CatalogoDeMedicamentos'
)
BEGIN
    CREATE INDEX [IX_EntradasDeMedicamento_Status] ON [EntradasDeMedicamento] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915230122_CatalogoDeMedicamentos'
)
BEGIN
    CREATE INDEX [IX_Medicamentos_ActiveIngredient] ON [Medicamentos] ([ActiveIngredient]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915230122_CatalogoDeMedicamentos'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Medicamentos_Barcode] ON [Medicamentos] ([Barcode]) WHERE [Barcode] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915230122_CatalogoDeMedicamentos'
)
BEGIN
    CREATE INDEX [IX_Medicamentos_NormalizedSearchKey] ON [Medicamentos] ([NormalizedSearchKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915230122_CatalogoDeMedicamentos'
)
BEGIN
    CREATE INDEX [IX_Medicamentos_Status] ON [Medicamentos] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915230122_CatalogoDeMedicamentos'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260915230122_CatalogoDeMedicamentos', N'10.0.11');
END;

COMMIT;
GO

