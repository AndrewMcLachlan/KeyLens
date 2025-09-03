using Asm.OAuth;

namespace KeyLens.Api.Options;

public record OAuthOptions : AzureOAuthOptions
{
    public string? Scope { get; init; }
}
