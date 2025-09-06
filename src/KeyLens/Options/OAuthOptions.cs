using Asm.OAuth;

namespace KeyLens.Options;

public record OAuthOptions : AzureOAuthOptions
{
    public required string ClientSecret { get; init; }

    public string? Scope { get; init; }
}
