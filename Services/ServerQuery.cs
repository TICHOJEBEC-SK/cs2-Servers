using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Servers.Config;

namespace Servers.Services;

public class ServerQuery
{
    private readonly int _timeoutMs;
    private readonly TimeSpan _ttl;
    private readonly ConcurrentDictionary<string, (DateTime ts, QueryResult result)> _cache = new();

    public ServerQuery(int timeoutMs, int cacheTtlSeconds)
    {
        _timeoutMs = Math.Max(200, timeoutMs);
        _ttl = TimeSpan.FromSeconds(Math.Max(0, cacheTtlSeconds));
    }

    public async Task<QueryResult> QueryAsync(ServerEndpoint ep, CancellationToken ct = default)
    {
        var key = $"{ep.Address}:{ep.Port}";
        if (_ttl > TimeSpan.Zero &&
            _cache.TryGetValue(key, out var entry) &&
            DateTime.UtcNow - entry.ts <= _ttl)
            return entry.result;

        var result = await QueryA2SInfo(ep, ct).ConfigureAwait(false);
        if (_ttl > TimeSpan.Zero)
            _cache[key] = (DateTime.UtcNow, result);
        return result;
    }

    public class QueryResult
    {
        public bool Ok { get; set; }
        public string Map { get; set; } = "unknown";
        public int Players { get; set; }
        public int MaxPlayers { get; set; }
        public int Bots { get; set; }
    }

    private static IPEndPoint BuildEndpoint(ServerEndpoint ep)
    {
        if (IPAddress.TryParse(ep.Address, out var ip))
            return new IPEndPoint(ip, ep.Port);

        var entry = Dns.GetHostEntry(ep.Address);
        var addr = entry.AddressList.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork)
                   ?? entry.AddressList.First();
        return new IPEndPoint(addr, ep.Port);
    }

    private async Task<QueryResult> QueryA2SInfo(ServerEndpoint ep, CancellationToken ct)
    {
        using var udp = new UdpClient();
        udp.Client.ReceiveTimeout = _timeoutMs;
        udp.Client.SendTimeout = _timeoutMs;

        var endpoint = BuildEndpoint(ep);

        static byte[] BuildInfoRequest(int? challenge = null)
        {
            var buf = new List<byte>(4 + 1 + 19 + 1 + (challenge.HasValue ? 4 : 0));
            buf.AddRange(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF });
            buf.Add(0x54);
            buf.AddRange(Encoding.ASCII.GetBytes("Source Engine Query"));
            buf.Add(0x00);
            if (challenge.HasValue)
                buf.AddRange(BitConverter.GetBytes(challenge.Value));
            return buf.ToArray();
        }

        async Task<byte[]?> ReceiveAsyncWithTimeout()
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(_timeoutMs);
            try
            {
                var resp = await udp.ReceiveAsync(cts.Token);
                return resp.Buffer;
            }
            catch
            {
                return null;
            }
        }

        try
        {
            var req = BuildInfoRequest();
            await udp.SendAsync(req, req.Length, endpoint);
            var data = await ReceiveAsyncWithTimeout();
            if (data is null || data.Length < 5) return new QueryResult { Ok = false };

            if (data[4] == 0x41)
            {
                if (data.Length < 9) return new QueryResult { Ok = false };
                var challenge = BitConverter.ToInt32(data, 5);

                var req2 = BuildInfoRequest(challenge);
                await udp.SendAsync(req2, req2.Length, endpoint);
                data = await ReceiveAsyncWithTimeout();
                if (data is null || data.Length < 5 || data[4] != 0x49)
                    return new QueryResult { Ok = false };
            }
            else if (data[4] != 0x49)
            {
                return new QueryResult { Ok = false };
            }

            var idx = 5;

            string ReadCString()
            {
                var start = idx;
                while (idx < data.Length && data[idx] != 0x00) idx++;
                var s = Encoding.UTF8.GetString(data, start, idx - start);
                if (idx < data.Length) idx++;
                return s;
            }

            if (idx >= data.Length) return new QueryResult { Ok = false };
            idx++;

            _ = ReadCString();
            var map = ReadCString();
            _ = ReadCString();
            _ = ReadCString();

            if (idx + 1 >= data.Length) return new QueryResult { Ok = false };
            idx += 2;

            if (idx + 2 >= data.Length) return new QueryResult { Ok = false };
            var players = data[idx++];
            var maxPlayers = data[idx++];
            var bots = 0;
            if (idx < data.Length) bots = data[idx++];

            return new QueryResult
            {
                Ok = true,
                Map = string.IsNullOrWhiteSpace(map) ? "unknown" : map,
                Players = players,
                MaxPlayers = maxPlayers,
                Bots = bots
            };
        }
        catch
        {
            return new QueryResult { Ok = false };
        }
    }
}