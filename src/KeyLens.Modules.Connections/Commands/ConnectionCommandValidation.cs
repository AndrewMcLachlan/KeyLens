using System.Text.Json;
using Microsoft.AspNetCore.Http;
using KeyLens.Connections;

namespace KeyLens.Modules.Connections.Commands;

internal static class ConnectionCommandValidation
{
    /// <summary>
    /// Checks the supplied configuration against the shape its connection type expects and returns it
    /// as JSON for storage.
    /// </summary>
    public static string Validate(Entities.ConnectionType type, object rawConfig, string? secret)
    {
        if (ConnectionConfig.RequiresSecret(type) && String.IsNullOrWhiteSpace(secret))
        {
            throw new BadHttpRequestException($"A {type} connection requires a secret.");
        }

        var json = rawConfig is JsonElement element ? element.GetRawText() : ConnectionConfig.Serialise(rawConfig);

        try
        {
            ConnectionConfig.Deserialise(type, json);
        }
        catch (Exception ex)
        {
            throw new BadHttpRequestException($"Invalid configuration for a {type} connection: {ex.Message}");
        }

        return json;
    }
}
