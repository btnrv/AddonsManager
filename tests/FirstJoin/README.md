# First-join regression checks

Run from the repository root:

```text
dotnet run --project tests/FirstJoin/FirstJoin.csproj -c Release
```

This console harness calls the production `AddonsHooks.SendNetMessage`, `AddonsClients` connection handlers, and native-context continuation. It uses the real SwiftlyS2 API interfaces, managed doubles for players/messages, and no CS2 native calls. The hook constructor is bypassed because installing a real detour requires the game process. The two explicit doubles cover APIs that `DispatchProxy` cannot implement (reference-returning SteamID and the protobuf interface's internal static factory).

The original handlers failed first signon without PlayerManager, slot reuse, and map-change checks. The corrected handlers also exercise addon-download reconnects, nested sends, exception restoration, concurrent threads, and idempotent hook removal. The harness allows the pre-fix handler revision to execute without a context wrapper to reproduce the earlier failures.

These checks do not validate native detour installation or client mounting. On a running server, join from a client without the extra addon downloaded, verify the download/reconnect sequence and final `S2C_CONNECTION` addon list, then check rendering, map change, reconnect, and plugin reload.
