<h1 align="center">
  CS2 Servers
</h1>

<p align="center">
<i>Loved the tool? Please consider <a href="https://paypal.com/paypalme/playpointsk">donating</a> 💸 to help it improve!</i>
</p>

<p align="center">
<a href="https://www.paypal.com/paypalme/playpointsk"><img src="https://img.shields.io/badge/support-PayPal-blue?logo=PayPal&style=flat-square&label=Donate"/>
</a>
</p>

---

## 📜 About the Plugin

A **Counter-Strike 2 plugin** for **CounterStrikeSharp** that shows the status of your server network (map, players, slots, online/offline).  
Useful for communities with multiple servers.

<p align="center">
  <img src="https://i.ibb.co/Z654ycY9/servers.png" alt="Servers Plugin Preview"/>
</p>

The plugin supports:
- **Configurable command names** (`servers`, etc.)
- **Configurable chat prefix and language**
- **Configurable query timeout & cache TTL**
- **Language files** (`en.json`, `sk.json`) for easy localization
- **Safe multithreading** – queries are offloaded, chat runs on main thread
- **Automatic broadcast** – every X seconds (configurable), a random online server is advertised in chat

---

## 🔹 Commands

1. **`servers`** – *Show servers list*
    - Displays all configured servers with name, map, player count, and status.
    - If a server is unreachable, shows it as offline.

---

## 🛠 Installation

**Requirements**
- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp)

**Steps**
1. Build the plugin (`dotnet build -c Release`) or download prebuilt.
2. Copy the DLL and `lang/` folder to:
   ```
   /game/csgo/addons/counterstrikesharp/plugins/Servers/
   ```
3. Start or restart the server.

---

## ⚙️ Configuration

Config is generated on first run:
```
{
  "ChatPrefix": "[SERVERS]",
  "CommandNames": [ "servers" ],
  "QueryTimeoutMs": 900,
  "CacheTtlSeconds": 5,
  "AdvertIntervalSeconds": 120,
  "Language": "en",
  "Servers": [
    { "Name": "Public #1", "Address": "127.0.0.1", "Port": 27015 },
    { "Name": "Public #2", "Address": "127.0.0.1", "Port": 27016 }
  ]
}
```

- **ChatPrefix** – text prefix with colors (`{green}`, `{default}`, …).
- **CommandNames** – command(s) registered for players.
- **QueryTimeoutMs** – UDP query timeout in ms (200–5000).
- **CacheTtlSeconds** – how long results are cached (0–30s).
- **AdvertIntervalSeconds** – how often (in seconds) a random online server is advertised to all players (0 = disabled).
- **Language** – `en` or `sk`.
- **Servers** – list of endpoints (IP/host + port).

---

## 🎨 Colors in translations

You can use color tags in `lang/en.json` or `lang/sk.json`:

Example:
```
"{green}{0}. {white}{1} {grey}| map {lightblue}{2} {grey}| players {white}{3}/{4}"
```
The `{color}` tags will be replaced by `ChatColors` codes automatically.

---

## 📩 Contact
- **Discord:** `tichotm`
