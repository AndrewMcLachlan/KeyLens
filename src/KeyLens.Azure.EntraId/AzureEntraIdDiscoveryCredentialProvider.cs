using System.Runtime.CompilerServices;
using Azure.Core;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Tenant = (System.Guid TenantId, string DisplayName);

namespace KeyLens.Azure.EntraId;

public class AzureEntraIdDiscoveryCredentialProvider(TokenCredential tokenCredential) : ICredentialProvider
{
    public string Name => "Azure.EntraId.Discovery";

    public IEnumerable<string> RequiredPermissions() =>
    [
        "Application.Read.All",
        "Directory.Read.All",
    ];

    public async IAsyncEnumerable<CredentialRecord> EnumerateAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Create a Graph client
        var graphClient = new GraphServiceClient(tokenCredential, ["https://graph.microsoft.com/.default"]);

        var tenants = await GetGraphAccessibleTenantsAsync(graphClient, cancellationToken);

        foreach (var tenant in tenants)
        // Enumerate app registrations from the tenant
        await foreach (var credential in EnumerateAppRegistrationsAsync(graphClient, tenant, cancellationToken))
        {
            yield return credential;
        }
    }

    private static async IAsyncEnumerable<CredentialRecord> EnumerateAppRegistrationsAsync(
        GraphServiceClient graphClient,
        Tenant tenant,
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
                    if (app.PasswordCredentials != null)
                    {
                        foreach (var passwordCred in app.PasswordCredentials)
                        {
                            results.Add(new CredentialRecord(
                                Provider: "Azure.EntraId",
                                Container: app.DisplayName ?? "Unknown App",
                                ContainerId: app.Id ?? String.Empty,
                                CredentialId: passwordCred.KeyId?.ToString() ?? String.Empty,
                                Kind: CredentialKind.Password,
                                Name: passwordCred.DisplayName ?? "Password Credential",
                                NotBefore: passwordCred.StartDateTime,
                                ExpiresOn: passwordCred.EndDateTime,
                                Enabled: true, // Password credentials don't have an enabled property
                                CredentialUri: new Uri($"https://portal.azure.com/#view/Microsoft_AAD_RegisteredApps/ApplicationMenuBlade/~/Credentials/appId/{app.AppId}/isMSAApp/"),
                                Metadata: new
                                {
                                    app.AppId,
                                    tenant.TenantId,
                                    TenantDisplayName = tenant.DisplayName,
                                }
                            ));
                        }
                    }

                    if (app.KeyCredentials != null)
                    {
                        foreach (var keyCred in app.KeyCredentials)
                        {
                            results.Add(new CredentialRecord(
                                Provider: "Azure.EntraId",
                                Container: app.DisplayName ?? "Unknown App",
                                ContainerId: app.Id ?? String.Empty,
                                CredentialId: keyCred.KeyId?.ToString() ?? String.Empty,
                                Kind: CredentialKind.Certificate,
                                Name: keyCred.DisplayName ?? "Certificate Credential",
                                NotBefore: keyCred.StartDateTime,
                                ExpiresOn: keyCred.EndDateTime,
                                Enabled: true, // Key credentials don't have an enabled property
                                CredentialUri: new Uri($"https://portal.azure.com/#view/Microsoft_AAD_RegisteredApps/ApplicationMenuBlade/~/Credentials/appId/{app.AppId}/isMSAApp/"),
                                Metadata: new
                                {
                                    app.AppId,
                                    tenant.TenantId,
                                    TenantDisplayName = tenant.DisplayName,
                                }
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
                                    ContainerId: app.Id ?? String.Empty,
                                    CredentialId: passwordCred.KeyId?.ToString() ?? String.Empty,
                                    Kind: CredentialKind.Password,
                                    Name: passwordCred.DisplayName ?? "Password Credential",
                                    NotBefore: passwordCred.StartDateTime,
                                    ExpiresOn: passwordCred.EndDateTime,
                                    Enabled: true,
                                    CredentialUri: new Uri($"https://portal.azure.com/#view/Microsoft_AAD_RegisteredApps/ApplicationMenuBlade/~/Credentials/appId/{app.AppId}/isMSAApp/"),
                                    Metadata: new
                                    {
                                        app.AppId,
                                        tenant.TenantId,
                                        TenantDisplayName = tenant.DisplayName,
                                    }
                                ));
                            }
                        }

                        if (app.KeyCredentials != null)
                        {
                            foreach (var keyCred in app.KeyCredentials)
                            {
                                results.Add(new CredentialRecord(
                                    Provider: "Azure.EntraId",
                                    Container: app.DisplayName ?? "Unknown App",
                                    ContainerId: app.Id ?? String.Empty,
                                    CredentialId: keyCred.KeyId?.ToString() ?? String.Empty,
                                    Kind: CredentialKind.Certificate,
                                    Name: keyCred.DisplayName ?? "Certificate Credential",
                                    NotBefore: keyCred.StartDateTime,
                                    ExpiresOn: keyCred.EndDateTime,
                                    Enabled: true,
                                    CredentialUri: new Uri($"https://portal.azure.com/#view/Microsoft_AAD_RegisteredApps/ApplicationMenuBlade/~/Credentials/appId/{app.AppId}/isMSAApp/"),
                                    Metadata: new
                                    {
                                        app.AppId,
                                        tenant.TenantId,
                                        TenantDisplayName = tenant.DisplayName,
                                    }
                                ));
                            }
                        }

                        return true;
                    });

                await pageIterator.IterateAsync(cancellationToken);
            }
        }
        catch (Exception)
        {
            // TODO: better error handling/logging
        }

        foreach (var record in results.OrderBy(r => r.Container).ThenBy(r => r.CredentialId))
        {
            yield return record;
        }
    }


    private static async Task<List<Tenant>> GetGraphAccessibleTenantsAsync(GraphServiceClient graphClient, CancellationToken cancellationToken)
    {
        List<Tenant> tenants = [];

        try
        {
            // Get organization info for current tenant
            var organization = await graphClient.Organization.GetAsync(cancellationToken: cancellationToken);

            if (organization?.Value?.Count > 0 == true)
            {
                var org = organization.Value.First();
                tenants.Add((
                    Guid.Parse(org.Id!),
                    org.DisplayName!
                ));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error accessing tenant via Graph: {ex.Message}");
        }

        return tenants;
    }
}
