-- Additive SQL Server upgrade for an EXISTING, verified legacy database.
-- Review and back up first. Not executed by application startup or onboarding.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID(N'dbo.Clients', N'U') IS NULL OR OBJECT_ID(N'dbo.Walkers', N'U') IS NULL
    THROW 51000, 'Verify the legacy schema before applying the M0 account upgrade.', 1;
IF OBJECT_ID(N'dbo.Accounts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Accounts (
        Id uniqueidentifier NOT NULL PRIMARY KEY,
        CreatedUtc datetime2 NOT NULL,
        UpdatedUtc datetime2 NOT NULL,
        Issuer nvarchar(256) COLLATE Latin1_General_100_BIN2 NOT NULL,
        Subject nvarchar(256) COLLATE Latin1_General_100_BIN2 NOT NULL,
        ClientId uniqueidentifier NULL,
        WalkerId uniqueidentifier NULL,
        IsAdministrator bit NOT NULL,
        IsDisabled bit NOT NULL,
        CONSTRAINT FK_Accounts_Clients_ClientId FOREIGN KEY (ClientId) REFERENCES dbo.Clients(Id),
        CONSTRAINT FK_Accounts_Walkers_WalkerId FOREIGN KEY (WalkerId) REFERENCES dbo.Walkers(Id)
    );
    CREATE UNIQUE INDEX IX_Accounts_Issuer_Subject ON dbo.Accounts(Issuer, Subject);
    CREATE UNIQUE INDEX IX_Accounts_ClientId ON dbo.Accounts(ClientId) WHERE ClientId IS NOT NULL;
    CREATE UNIQUE INDEX IX_Accounts_WalkerId ON dbo.Accounts(WalkerId) WHERE WalkerId IS NOT NULL;
END;
COMMIT;
-- No automatic email-based claims, administrator seeds, production database creation,
-- legacy constraint changes or destructive rollback are included.
