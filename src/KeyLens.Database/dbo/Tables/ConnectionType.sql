CREATE TABLE [dbo].[ConnectionType]
(
    [Id] INT NOT NULL,
    [Name] NVARCHAR(50) NOT NULL,
    CONSTRAINT [PK_ConnectionType] PRIMARY KEY CLUSTERED (Id),
)

GO

CREATE UNIQUE INDEX [IX_ConnectionType_Name] ON [dbo].[ConnectionType] ([Name])
