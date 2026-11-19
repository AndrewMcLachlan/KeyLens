CREATE TABLE [dbo].[Connection]
(
    [Id] UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Connection_Id DEFAULT NEWID(),
    [OwnerId] UNIQUEIDENTIFIER NOT NULL,
    [Name] NVARCHAR(100) NOT NULL,
    [ConnectionTypeId] INT NOT NULL,
    [Enabled] BIT NOT NULL CONSTRAINT DF_Connection_Enabled DEFAULT 1,
    -- Provider-specific settings as JSON. Opaque to the database: never queried or indexed.
    [Config] NVARCHAR(4000) NOT NULL,
    [Created] DATETIMEOFFSET NOT NULL CONSTRAINT DF_Connection_Created DEFAULT SYSUTCDATETIME(),
    [LastScan] DATETIMEOFFSET NULL,
    [LastScanError] NVARCHAR(1000) NULL,
    -- Names this connection's secret in the key vault. The secret itself is never stored here.
    [SecretName] NVARCHAR(127) NULL,
    [SecretUpdated] DATETIMEOFFSET NULL,
    CONSTRAINT [PK_Connection] PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT [FK_Connection_ConnectionType] FOREIGN KEY (ConnectionTypeId) REFERENCES [ConnectionType](Id),
)

GO

CREATE INDEX [IX_Connection_OwnerId] ON [dbo].[Connection] ([OwnerId])

GO

CREATE UNIQUE INDEX [IX_Connection_OwnerId_Name] ON [dbo].[Connection] ([OwnerId], [Name])
