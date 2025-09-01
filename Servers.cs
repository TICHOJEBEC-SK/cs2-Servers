﻿using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Extensions;
using CounterStrikeSharp.API.Modules.Timers;
using Servers.Config;
using Servers.Services;

using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace Servers;

public class Servers : BasePlugin, IPluginConfig<PluginConfig>
{
    public override string ModuleName => "Servers";
    public override string ModuleVersion => "1.2";
    public override string ModuleAuthor => "TICHOJEBEC";

    public PluginConfig Config { get; set; } = new();

    private Localization _l = null!;
    private ServerQuery _query = null!;
    private readonly HashSet<string> _registered = new(StringComparer.OrdinalIgnoreCase);

    private Timer? _advertTimer;

    public void OnConfigParsed(PluginConfig config)
    {
        if (config.QueryTimeoutMs < 200) config.QueryTimeoutMs = 200;
        if (config.QueryTimeoutMs > 5000) config.QueryTimeoutMs = 5000;
        if (config.CacheTtlSeconds < 0) config.CacheTtlSeconds = 0;
        if (config.CacheTtlSeconds > 30) config.CacheTtlSeconds = 30;

        if (config.AdvertIntervalSeconds < 0) config.AdvertIntervalSeconds = 0;
        if (config.AdvertIntervalSeconds > 3600) config.AdvertIntervalSeconds = 3600;

        if (string.IsNullOrWhiteSpace(config.ChatPrefix)) config.ChatPrefix = " {green}[Servers]{default}";
        if (config.CommandNames.Length == 0) config.CommandNames = new[] { "servers" };
        if (string.IsNullOrWhiteSpace(config.Language)) config.Language = "en";
        
        config.Servers ??= new List<ServerEndpoint>();

        foreach (var ep in config.Servers)
        {
            if (string.IsNullOrWhiteSpace(ep.Name)) ep.Name = "Server";
            if (string.IsNullOrWhiteSpace(ep.Address)) throw new Exception($"Server '{ep.Name}' has empty Address.");
            if (ep.Port is < 1 or > 65535) throw new Exception($"Server '{ep.Name}' has invalid Port: {ep.Port}");
        }

        Config = config;
    }

    public override void Load(bool hotReload)
    {
        var langDir = Path.Combine(ModuleDirectory, "lang");
        _l = new Localization(langDir, Config.Language);
        _query = new ServerQuery(Config.QueryTimeoutMs, Config.CacheTtlSeconds);

        foreach (var name in Config.CommandNames)
            RegisterCommandOnce(name, "Shows the list of servers", OnCmdServers);

        StartAdvertTimer();
    }

    private void StartAdvertTimer()
    {
        _advertTimer?.Kill();

        if (Config.AdvertIntervalSeconds > 0)
        {
            _advertTimer = AddTimer((float)Config.AdvertIntervalSeconds, OnAdvertTick, TimerFlags.REPEAT);
        }
    }

    private void RegisterCommandOnce(string name, string help, CommandInfo.CommandCallback callback)
    {
        if (string.IsNullOrWhiteSpace(name)) return;
        if (_registered.Contains(name)) return;
        AddCommand(name, help, callback);
        _registered.Add(name);
    }

    private string Pref(string s) => $"{Config.ChatPrefix} {s}";

    private void OnCmdServers(CCSPlayerController? caller, CommandInfo info)
    {
        if (!Chat.ValidateCaller(caller)) return;
        var player = caller!;

        var eps = Config.Servers.ToArray();

        _ = Task.Run(async () =>
        {
            var tasks = eps.Select(async (ep, i) =>
            {
                var qr = await _query.QueryAsync(ep).ConfigureAwait(false);
                return new { Index = i + 1, Ep = ep, Q = qr };
            });
            var results = await Task.WhenAll(tasks).ConfigureAwait(false);

            Server.NextFrame(() =>
            {
                if (player is not { IsValid: true }) return;

                Chat.ToPlayer(player, Pref(_l["Servers.Header"]));
                foreach (var r in results)
                {
                    if (r.Q.Ok)
                    {
                        var shownPlayers = Config.CountBots
                            ? r.Q.Players
                            : Math.Max(0, r.Q.Players - r.Q.Bots);

                        Chat.ToPlayer(player, Pref(_l["Servers.Line.Online"]),
                            r.Index, r.Ep.Name, r.Q.Map, shownPlayers, r.Q.MaxPlayers, r.Ep.Address, r.Ep.Port);
                    }
                    else
                    {
                        Chat.ToPlayer(player, Pref(_l["Servers.Line.Offline"]),
                            r.Index, r.Ep.Name, r.Ep.Address, r.Ep.Port);
                    }
                }
            });
        });
    }

    private void OnAdvertTick()
    {
        var eps = Config.Servers.ToArray();

        _ = Task.Run(async () =>
        {
            var tasks = eps.Select(async ep =>
            {
                var qr = await _query.QueryAsync(ep).ConfigureAwait(false);
                return new { Ep = ep, Q = qr };
            });

            var results = await Task.WhenAll(tasks).ConfigureAwait(false);
            var online = results.Where(r => r.Q.Ok).ToArray();
            if (online.Length == 0) return;

            var pick = online[Random.Shared.Next(online.Length)];
            var shownPlayers = Config.CountBots
                ? pick.Q.Players
                : Math.Max(0, pick.Q.Players - pick.Q.Bots);

            Server.NextFrame(() =>
            {
                Chat.ToAllFmt(Pref(_l["Servers.Line.Online"]),
                    0, pick.Ep.Name, pick.Q.Map, shownPlayers, pick.Q.MaxPlayers,
                    pick.Ep.Address, pick.Ep.Port);
            });
        });
    }

    [ConsoleCommand("servers_reload_config", "Reloads the Servers plugin config")]
    [RequiresPermissions("@css/root")]
    public void OnReloadConfig(CCSPlayerController? player, CommandInfo cmd)
    {
        Config.Reload();
        StartAdvertTimer();
        cmd.ReplyToCommand(_l["Servers.Reload.Done"]);
    }

    [ConsoleCommand("servers_reset_config", "Resets the Servers plugin config to defaults (in-memory)")]
    [RequiresPermissions("@css/root")]
    public void OnResetConfig(CCSPlayerController? player, CommandInfo cmd)
    {
        Config.Update();
        StartAdvertTimer();
        cmd.ReplyToCommand(_l["Servers.Reset.Done"]);
    }
}