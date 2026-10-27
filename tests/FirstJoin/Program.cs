using System.Reflection;
using System.Runtime.CompilerServices;
using AddonsManager.Clients;
using AddonsManager.Config;
using AddonsManager.Hooks;
using AddonsManager.Utils;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Engine;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Memory;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.ProtobufDefinitions;
using SwiftlyS2.Shared.Services;

namespace FirstJoin;

internal static class Program
{
    private const ulong NewSteamId = 76561198000000001;
    private const ulong OldSteamId = 76561198000000002;
    private const string Map = "111";
    private const string Addon = "222";

    public static int Main()
    {
        (string Name, Action Test)[] tests =
        [
            ("first signon before PlayerManager and ReplyConnection", FirstSignon),
            ("slot reuse ignores a previous player's addon cache", ReusedSlot),
            ("map change works after managed player removal", MapChange),
            ("download reconnect requests the next addon then completes", DownloadReconnect),
            ("nested sends restore the outer client", NestedSend),
            ("exception restores the previous send context", ThrowingSend),
            ("separate network threads keep separate client contexts", ConcurrentSend),
            ("unload removes the context hook exactly once", Unload),
        ];
        var failures = 0;
        foreach (var (name, test) in tests)
        {
            try
            {
                test();
                Console.WriteLine($"PASS {name}");
            }
            catch (Exception exception)
            {
                failures++;
                Console.Error.WriteLine($"FAIL {name}: {exception.GetBaseException().Message}");
            }
        }
        Console.WriteLine($"{tests.Length - failures}/{tests.Length} checks passed.");
        return failures == 0 ? 0 : 1;
    }

    private static void FirstSignon()
    {
        var fixture = new Fixture();
        var message = NewMessage();
        fixture.WithClient(1, () => fixture.Hooks.SendNetMessage(message, 0));
        Equal(SignonState_t.SIGNONSTATE_CHANGELEVEL, message.SignonState);
        Equal(Map, message.Addons);
        Equal(Map, fixture.Clients.GetClientInfo(NewSteamId).CurrentPendingAddon);
    }

    private static void ReusedSlot()
    {
        var fixture = new Fixture();
        fixture.ManagedPlayer = NewPlayer(OldSteamId);
        fixture.Clients.GetClientInfo(OldSteamId).DownloadedAddons.AddRange([Map, Addon]);
        fixture.Clients.OnClientDisconnected(TestProxy.Create<IOnClientDisconnectedEvent>((_, _) => 0));
        var message = NewMessage();
        fixture.WithClient(1, () => fixture.Hooks.SendNetMessage(message, 0));
        Equal(SignonState_t.SIGNONSTATE_CHANGELEVEL, message.SignonState);
        Equal(Map, message.Addons);
        Equal("", fixture.Clients.GetClientInfo(OldSteamId).CurrentPendingAddon);
    }

    private static void MapChange()
    {
        var fixture = new Fixture();
        var message = NewMessage(SignonState_t.SIGNONSTATE_CHANGELEVEL, $"{Map},{Addon}");
        fixture.WithClient(1, () => fixture.Hooks.SendNetMessage(message, 0));
        Equal(Map, message.Addons);
        Equal(Map, fixture.Clients.GetClientInfo(NewSteamId).CurrentPendingAddon);
    }

    private static void DownloadReconnect()
    {
        var fixture = new Fixture();
        var info = fixture.Clients.GetClientInfo(NewSteamId);
        info.CurrentPendingAddon = Map;
        fixture.ManagedPlayer = NewPlayer(NewSteamId);
        var connected = TestProxy.Create<IOnClientConnectedEvent>((_, _) => 0);
        fixture.Clients.OnClientConnected(connected);
        var next = NewMessage();
        fixture.WithClient(1, () => fixture.Hooks.SendNetMessage(next, 0));
        Equal(Addon, next.Addons);
        Equal(SignonState_t.SIGNONSTATE_CHANGELEVEL, next.SignonState);
        fixture.Clients.OnClientConnected(connected);
        var final = NewMessage();
        fixture.WithClient(1, () => fixture.Hooks.SendNetMessage(final, 0));
        Equal(SignonState_t.SIGNONSTATE_CONNECTED, final.SignonState);
        Equal(2, info.DownloadedAddons.Count);
    }

    private static void NestedSend()
    {
        var fixture = new Fixture();
        fixture.Clients.GetClientInfo(OldSteamId).DownloadedAddons.AddRange([Map, Addon]);
        fixture.WithClient(1, () =>
        {
            var inner = NewMessage();
            fixture.WithClient(2, () => fixture.Hooks.SendNetMessage(inner, 0));
            Equal(SignonState_t.SIGNONSTATE_CONNECTED, inner.SignonState);
            var outer = NewMessage();
            fixture.Hooks.SendNetMessage(outer, 0);
            Equal(SignonState_t.SIGNONSTATE_CHANGELEVEL, outer.SignonState);
        });
        Equal((nint)0, fixture.CurrentClient);
    }

    private static void ThrowingSend()
    {
        var fixture = new Fixture();
        fixture.WithClient(1, () =>
        {
            try
            {
                fixture.WithClient(2, () => throw new InvalidOperationException("sentinel"));
                throw new Exception("The continuation exception was swallowed.");
            }
            catch (Exception exception) when (exception.GetBaseException().Message == "sentinel")
            {
                Equal((nint)1, fixture.CurrentClient);
            }
        });
        Equal((nint)0, fixture.CurrentClient);
    }

    private static void ConcurrentSend()
    {
        var fixture = new Fixture();
        using var entered = new Barrier(2);
        var first = Task.Run(() => fixture.WithClient(1, () =>
        {
            Check(entered.SignalAndWait(TimeSpan.FromSeconds(5)), "Thread rendezvous timed out.");
            Equal((nint)1, fixture.CurrentClient);
            Check(entered.SignalAndWait(TimeSpan.FromSeconds(5)), "Thread rendezvous timed out.");
        }));
        var second = Task.Run(() => fixture.WithClient(2, () =>
        {
            Check(entered.SignalAndWait(TimeSpan.FromSeconds(5)), "Thread rendezvous timed out.");
            Equal((nint)2, fixture.CurrentClient);
            Check(entered.SignalAndWait(TimeSpan.FromSeconds(5)), "Thread rendezvous timed out.");
        }));
        Task.WaitAll(first, second);
    }

    private static IPlayer NewPlayer(ulong steamId) => TestProxy.Create<IPlayer>((method, _) =>
        method.Name == "get_UnauthorizedSteamID" ? steamId : throw new NotSupportedException(method.Name));

    private static void Unload()
    {
        var fixture = new Fixture();
        var function = typeof(AddonsHooks).GetField("_sendNetMessage", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var hook = typeof(AddonsHooks).GetField("_sendNetMessageHook", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var hookId = Guid.NewGuid();
        var removed = 0;
        Action<Guid> remove = id =>
        {
            Equal(hookId, id);
            removed++;
        };
        function.SetValue(fixture.Hooks, Activator.CreateInstance(
            typeof(TestFunction<>).MakeGenericType(function.FieldType.GenericTypeArguments[0]), remove));
        hook.SetValue(fixture.Hooks, hookId);
        ((IDisposable)fixture.Hooks).Dispose();
        ((IDisposable)fixture.Hooks).Dispose();
        Equal(1, removed);
    }

    private static CNETMsg_SignonState NewMessage(SignonState_t state = SignonState_t.SIGNONSTATE_CONNECTED, string addons = "")
    {
        return new TestSignonState { SignonState = state, Addons = addons };
    }

    private static void Equal<T>(T expected, T actual) =>
        Check(EqualityComparer<T>.Default.Equals(expected, actual), $"Expected {expected}, got {actual}.");

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private sealed class Fixture
    {
        public AddonsClients Clients { get; }
        public AddonsHooks Hooks { get; }
        public IPlayer? ManagedPlayer { get; set; }
        public nint CurrentClient => (nint?)typeof(AddonsHooks)
            .GetField("_sendingClient", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null) ?? 0;

        public Fixture()
        {
            var players = TestProxy.Create<IPlayerManagerService>((method, _) =>
                method.Name == "GetPlayer" ? ManagedPlayer : throw new NotSupportedException(method.Name));
            var registrator = TestProxy.Create<IRegistratorService>((_, _) => null);
            var memory = TestProxy.Create<IMemoryService>((method, args) =>
                method.Name == "ToServerSideClient"
                    ? new TestServerSideClient(0, (nint)args[0]! == 1 ? NewSteamId : OldSteamId)
                    : throw new NotSupportedException(method.Name));
            var core = TestProxy.Create<ISwiftlyCore>((method, _) => method.Name switch
            {
                "get_PlayerManager" => players,
                "get_Registrator" => registrator,
                "get_Memory" => memory,
                "get_Logger" => NullLogger.Instance,
                _ => throw new NotSupportedException(method.Name)
            });
            var config = TestProxy.Create<IOptionsMonitor<AddonsConfig>>((_, _) => new AddonsConfig());
            var utilities = new AddonsUtilities(core);
            utilities.SetCurrentWorkshopMap(Map);
            utilities.GetMountedAddons().Add(Addon);
            Clients = new AddonsClients(core, utilities, config);
            // Skip native hook installation; exercise the actual managed handlers and context wrapper.
            Hooks = (AddonsHooks)RuntimeHelpers.GetUninitializedObject(typeof(AddonsHooks));
            SetField("Core", core);
            SetField("Clients", Clients);
            SetField("Utilities", utilities);
        }

        private void SetField(string name, object value) =>
            typeof(AddonsHooks).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(Hooks, value);

        public void WithClient(nint client, Action continuation)
        {
            var wrapper = typeof(AddonsHooks).GetMethod("SendWithClientContext", BindingFlags.Static | BindingFlags.NonPublic);
            if (wrapper is null)
            {
                // Also permits running this regression harness against the pre-fix revision.
                continuation();
                return;
            }
            var nextType = wrapper.GetParameters()[3].ParameterType;
            var delegateType = nextType.GenericTypeArguments[0];
            var adapter = new Continuation(continuation);
            var next = Delegate.CreateDelegate(delegateType, adapter, nameof(Continuation.Send));
            var factory = typeof(Fixture).GetMethod(nameof(NextFactory), BindingFlags.Static | BindingFlags.NonPublic)!
                .MakeGenericMethod(delegateType).Invoke(null, [next]);
            Equal(false, (bool)wrapper.Invoke(null, [client, (nint)123, NetChannelBufType_t.BUF_RELIABLE, factory])!);
        }

        private static Func<T> NextFactory<T>(T next) => () => next;
    }

    private sealed class Continuation(Action action)
    {
        public bool Send(nint client, nint message, NetChannelBufType_t bufferType)
        {
            Equal((nint)123, message);
            Equal(NetChannelBufType_t.BUF_RELIABLE, bufferType);
            action();
            return false;
        }
    }

    private sealed class TestFunction<TDelegate>(Action<Guid> remove) : IUnmanagedFunction<TDelegate> where TDelegate : Delegate
    {
        public nint Address => throw new NotSupportedException();
        public TDelegate CallOriginal => throw new NotSupportedException();
        public TDelegate Call => throw new NotSupportedException();
        public Guid AddHook(Func<Func<TDelegate>, TDelegate> callbackBuilder) => throw new NotSupportedException();
        public void RemoveHook(Guid id) => remove(id);
    }
}
