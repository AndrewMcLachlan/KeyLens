using Asm.OAuth;

namespace KeyLens.Options;

public record OAuthOptions : AzureOAuthOptions
{
    public required string ClientSecret { get; init; }

    public required string Scope { get; init; }
}
