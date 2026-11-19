/*
Post-Deployment Script
----------------------
Seeds the lookup tables. MERGE keeps this re-runnable on every deployment.
*/

MERGE INTO [dbo].[ConnectionType] AS Target
USING (VALUES
    (1, N'Azure'),
    (2, N'Aws'),
    (3, N'HashiCorpVault'),
    (4, N'TlsEndpoint')
) AS Source ([Id], [Name])
ON Target.[Id] = Source.[Id]
WHEN MATCHED AND Target.[Name] <> Source.[Name] THEN
    UPDATE SET [Name] = Source.[Name]
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([Id], [Name]) VALUES (Source.[Id], Source.[Name]);
