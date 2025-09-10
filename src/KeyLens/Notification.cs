
namespace KeyLens.Api.Services;

public class Notification(CredentialRecord credential)
{
    public CredentialRecord Credential => credential;

    public string Message
    {
        get
        {
            if (!credential.ExpiresOn.HasValue) return $"Credential '{credential.Name}' in '{credential.Container}' has no expiration date.";

            return credential.ExpiresOn.Value.UtcDateTime > DateTime.UtcNow
                ? $"Credential '{credential.Name}' in '{credential.Container}' expires in {((credential.ExpiresOn.Value.UtcDateTime - DateTime.UtcNow).Days)} days on {credential.ExpiresOn.Value.UtcDateTime:yyyy-MM-dd}."
                : $"Credential '{credential.Name}' in '{credential.Container}' expired {(DateTime.UtcNow - credential.ExpiresOn.Value.UtcDateTime).Days} days ago on {credential.ExpiresOn.Value.UtcDateTime:yyyy-MM-dd}.";
        }
    }
}
