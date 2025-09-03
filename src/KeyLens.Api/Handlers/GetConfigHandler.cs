using KeyLens.Api.Models;
using KeyLens.Api.Options;
using Microsoft.Extensions.Options;

namespace KeyLens.Api.Handlers;

public static class GetConfigHandler
{
    public static Config Handle(IOptions<OAuthOptions> options) => new(
        options.Value.Audience ?? String.Empty,
        options.Value.Scope ?? String.Empty
    );
}
