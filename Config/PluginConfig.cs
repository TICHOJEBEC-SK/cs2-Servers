using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

namespace Servers.Config;

public class PluginConfig : BasePluginConfig
{
    [JsonPropertyName("Language")] public string Language { get; set; } = "en";
    [JsonPropertyName("ChatPrefix")] public string ChatPrefix { get; set; } = " {green}[Servers]{default}";
    [JsonPropertyName("CommandNames")] public string[] CommandNames { get; set; } = new[] { "servers" };
    [JsonPropertyName("QueryTimeoutMs")] public int QueryTimeoutMs { get; set; } = 900;
    [JsonPropertyName("CacheTtlSeconds")] public int CacheTtlSeconds { get; set; } = 5;
    [JsonPropertyName("AdvertIntervalSeconds")] public int AdvertIntervalSeconds { get; set; } = 120;
    [JsonPropertyName("Servers")] public List<ServerEndpoint> Servers { get; set; } = new()
    {
        new ServerEndpoint { Name = "Public #1", Address = "127.0.0.1", Port = 27015 },
        new ServerEndpoint { Name = "Public #2", Address = "127.0.0.1", Port = 27016 }
    };
}

public class ServerEndpoint
{
    public string Name { get; set; } = "Server";
    public string Address { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 27015;
}