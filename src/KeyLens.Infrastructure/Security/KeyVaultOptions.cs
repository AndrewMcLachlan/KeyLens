namespace KeyLens.Infrastructure.Security;

public class KeyVaultOptions
{
    public const string SectionName = "KeyVault";

    /// <summary>
    /// Vault that holds connection secrets.
    /// </summary>
    public Uri? VaultUri { get; set; }

    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromMinutes(5);
}
