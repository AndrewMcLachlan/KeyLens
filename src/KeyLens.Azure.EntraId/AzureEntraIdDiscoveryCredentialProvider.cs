using System.Runtime.CompilerServices;
using Azure.Core;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace KeyLens.Azure.EntraId;

public class AzureEntraIdDiscoveryCredentialProvider(TokenCredential tokenCredential, string? tenantId = null) : ICredentialProvider
{
    public string Name => "Azure.EntraId.Discovery";

    public async IAsyncEnumerable<CredentialRecord> EnumerateAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Create a Graph client
        var graphClient = new GraphServiceClient(tokenCredential, ["https://graph.microsoft.com/.default"]);

        // Enumerate app registrations from the tenant
        await foreach (var credential in EnumerateAppRegistrationsAsync(graphClient, tenantId ?? "Current Tenant", cancellationToken))
        {
            yield return credential;
        }
    }

    private static async IAsyncEnumerable<CredentialRecord> EnumerateAppRegistrationsAsync(
        GraphServiceClient graphClient,
        string tenantDisplayName,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var results = new List<CredentialRecord>();

        try
        {
            var applications = await graphClient.Applications.GetAsync(requestConfiguration =>
            {
                requestConfiguration.QueryParameters.Select = ["id", "displayName", "appId", "passwordCredentials", "keyCredentials"];
            }, cancellationToken);

            if (applications?.Value != null)
            {
                foreach (var app in applications.Value)
                {
                    // Process password credentials (secrets)
                    if (app.PasswordCredentials != null)
                    {
                        foreach (var passwordCred in app.PasswordCredentials)
                        {
                            results.Add(new CredentialRecord(
                                Provider: "Azure.EntraId",
                                Container: app.DisplayName ?? "Unknown App",
                                ContainerId: app.Id ?? string.Empty,
                                CredentialId: passwordCred.KeyId?.ToString() ?? string.Empty,
                                Kind: CredentialKind.Password,
                                Name: passwordCred.DisplayName ?? "Password Credential",
                                NotBefore: passwordCred.StartDateTime,
                                ExpiresOn: passwordCred.EndDateTime,
                                Enabled: true, // Password credentials don't have an enabled property
                                Metadata: new { AppId = app.AppId, TenantId = tenantDisplayName }
                            ));
                        }
                    }

                    // Process key credentials (certificates)
                    if (app.KeyCredentials != null)
                    {
                        foreach (var keyCred in app.KeyCredentials)
                        {
                            results.Add(new CredentialRecord(
                                Provider: "Azure.EntraId",
                                Container: app.DisplayName ?? "Unknown App",
                                ContainerId: app.Id ?? string.Empty,
                                CredentialId: keyCred.KeyId?.ToString() ?? string.Empty,
                                Kind: CredentialKind.Certificate,
                                Name: keyCred.DisplayName ?? "Certificate Credential",
                                NotBefore: keyCred.StartDateTime,
                                ExpiresOn: keyCred.EndDateTime,
                                Enabled: true, // Key credentials don't have an enabled property
                                Metadata: new { AppId = app.AppId, TenantId = tenantDisplayName }
                            ));
                        }
                    }
                }

                // Handle pagination if there are more results
                var pageIterator = PageIterator<Application, ApplicationCollectionResponse>
                    .CreatePageIterator(graphClient, applications, (app) =>
                    {
                        // Process password credentials (secrets)
                        if (app.PasswordCredentials != null)
                        {
                            foreach (var passwordCred in app.PasswordCredentials)
                            {
                                results.Add(new CredentialRecord(
                                    Provider: "Azure.EntraId",
                                    Container: app.DisplayName ?? "Unknown App",
                                    ContainerId: app.Id ?? string.Empty,
                                    CredentialId: passwordCred.KeyId?.ToString() ?? string.Empty,
                                    Kind: CredentialKind.Password,
                                    Name: passwordCred.DisplayName ?? "Password Credential",
                                    NotBefore: passwordCred.StartDateTime,
                                    ExpiresOn: passwordCred.EndDateTime,
                                    Enabled: true,
                                    Metadata: new { AppId = app.AppId, TenantId = tenantDisplayName }
                                ));
                            }
                        }

                        // Process key credentials (certificates)
                        if (app.KeyCredentials != null)
                        {
                            foreach (var keyCred in app.KeyCredentials)
                            {
                                results.Add(new CredentialRecord(
                                    Provider: "Azure.EntraId",
                                    Container: app.DisplayName ?? "Unknown App",
                                    ContainerId: app.Id ?? string.Empty,
                                    CredentialId: keyCred.KeyId?.ToString() ?? string.Empty,
                                    Kind: CredentialKind.Certificate,
                                    Name: keyCred.DisplayName ?? "Certificate Credential",
                                    NotBefore: keyCred.StartDateTime,
                                    ExpiresOn: keyCred.EndDateTime,
                                    Enabled: true,
                                    Metadata: new { AppId = app.AppId, TenantId = tenantDisplayName }
                                ));
                            }
                        }

                        return true; // Continue iteration
                    });

                await pageIterator.IterateAsync(cancellationToken);
            }
        }
        catch (Exception)
        {
            // Skip if we can't access applications in this tenant
        }

        foreach (var record in results.OrderBy(r => r.Container).ThenBy(r => r.CredentialId))
        {
            yield return record;
        }
    }

    public IEnumerable<string> RequiredPermissions() =>
    [
        "Application.Read.All",
        "Directory.Read.All"
    ];
}
