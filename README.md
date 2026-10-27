<div align="center">
  <img src="https://pan.samyyc.dev/s/VYmMXE" />
  <h2><strong>Addons Manager</strong></h2>
  <h3>No description.</h3>
</div>

<p align="center">
  <img src="https://img.shields.io/badge/build-passing-brightgreen" alt="Build Status">
  <img src="https://img.shields.io/github/downloads/SwiftlyS2-Plugins/AddonsManager/total" alt="Downloads">
  <img src="https://img.shields.io/github/stars/SwiftlyS2-Plugins/AddonsManager?style=flat&logo=github" alt="Stars">
  <img src="https://img.shields.io/github/license/SwiftlyS2-Plugins/AddonsManager" alt="License">
</p>

## Building

- Open the project in your preferred .NET IDE (e.g., Visual Studio, Rider, VS Code).
- Build the project. The output DLL and resources will be placed in the `build/` directory.
- The publish process will also create a zip file for easy distribution.

## First-join addon delivery

Version 2.1.1 resolves signon recipients from the network client while the message is being sent. The initial signon can occur before `ReplyConnection` and before SwiftlyS2 creates a managed player. Looking up only a managed player or remembering the last occupant of a slot could skip addon downloads for a newcomer or use another client's cache.

The plugin wraps the framework's current `CServerSideClient::SendNetMessage` vtable entry to expose the borrowed client pointer to the existing typed signon handler. The context is thread-local, restored after nested sends or exceptions, and its hook is removed on unload. It does not retain client pointers after the send or change the download/cache policy. The vtable index comes from SwiftlyS2's `CServerSideClient::SendNetMessage` gamedata; no numeric offset is copied into this plugin.

The ABI and callback ordering were checked against SwiftlyS2 `v1.5.1-beta.3`: [native SendNetMessage](https://github.com/swiftly-solution/swiftlys2/blob/v1.5.1-beta.3/src/network/netmessages/netmessages.cpp), [vtable trampoline](https://github.com/swiftly-solution/swiftlys2/blob/v1.5.1-beta.3/src/memory/hooks/vfunction.cpp), and [managed vtable lookup](https://github.com/swiftly-solution/swiftlys2/blob/v1.5.1-beta.3/managed/src/SwiftlyS2.Core/Modules/Memory/MemoryService.cs). The native function returns a one-byte `bool`; `NetChannelBufType_t` is the framework's signed-byte enum. This preserves the framework's protobuf handling and follows [MultiAddonManager's network-client identity route](https://github.com/Source2ZE/MultiAddonManager/blob/master/src/multiaddonmanager.cpp).

See [the managed regression checks](tests/FirstJoin/README.md) for validation and the remaining live-client checks.

## Publishing

- Use the `dotnet publish -c Release` command to build and package your plugin.
- Distribute the generated zip file or the contents of the `build/publish` directory.

## Commands

```
sw_downloadaddon <workshop_id> # Download a workshop addon via command
sw_searchpath # View all the VPK Search Paths
```

## Adding Addons

- To add an addon for players to download, modify `addons/swiftlys2/configs/plugins/AddonsManager/config.jsonc` at key `Main.Addons` with Workshop ID's:

```jsonc
{
  "Main": {
    "Addons": [
      "WORKSHOP_ID1",
      "WORKSHOP_ID2",
      // ...
    ],
    // ...
  }
}
```

## Config Options

| Option                          | Default | Description                                                                                                                                                                                      |
| ------------------------------- | ------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `Addons`                        | `[]`    | Workshop IDs mounted and sent to clients on connect.                                                                                                                                             |
| `BlockDisconnectMessages`       | `true`  | Suppress the loopback disconnect message clients get when the server mounts addons (reconnect). Set `false` to let it through.                                                                   |
| `CacheClientsWithAddons`        | `true`  | Remember which addons a client already downloaded across reconnects, skipping re-download. `false` clears the list on disconnect.                                                                |
| `CacheClientsDurationInSeconds` | `0.0`   | How long a client's download cache stays valid after going inactive, in seconds. `0` disables expiry (cache never clears from inactivity). Only applies when `CacheClientsWithAddons` is `true`. |
| `ExtraAddonsTimeoutInSeconds`   | `10.0`  | Seconds to wait for a client to finish downloading a pending addon after reconnect before giving up on crediting it as downloaded.                                                               |
| `RedownloadAddonOnMount`        | `false` | Force a re-download of an addon every time it's mounted, even if already installed.                                                                                                              |

## API for other plugins

Reference `AddonsManager.Contract.dll` (build output of `AddonsManager.Contract`), then:

```csharp
private IAddonsManagerApi? _addons;

public override void UseSharedInterface(IInterfaceManager interfaceManager)
{
    if (interfaceManager.TryGetSharedInterface<IAddonsManagerApi>(IAddonsManagerApi.Key, out var api))
        _addons = api;
}

public override void Load(bool hotReload) => _addons?.AddAddon("WORKSHOP_ID");
```

- `AddAddon(id)` / `RemoveAddon(id)`: in memory only, call on every load. Config addons cannot be removed.
- `GetAddons()` / `GetMountedAddons()`: read-only lists.

## Acknowledgements

This plugin is a port of Source2ZE's MultiAddonManager for SwiftlyS2. It is released under GPL with credits given to the original code writers.
