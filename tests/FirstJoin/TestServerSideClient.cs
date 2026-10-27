using SwiftlyS2.Shared.Engine;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.ProtobufDefinitions;
using SwiftlyS2.Shared.SteamAPI;

namespace FirstJoin;

// A real API implementation is needed because DispatchProxy cannot return a ref CSteamID.
internal sealed class TestServerSideClient(int slot, ulong steamId) : IServerSideClient
{
    private CSteamID _steamId = new(steamId);
    public int ClientSlot { get; } = slot;
    public ref CSteamID SteamID => ref _steamId;
    public string UserIDString => throw new NotSupportedException();
    public string Name { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public uint EntityIndex => throw new NotSupportedException();
    public INetChannel NetChannel => throw new NotSupportedException();
    public byte ConnectionTypeFlags => throw new NotSupportedException();
    public byte AsyncDisconnectFlags => throw new NotSupportedException();
    public bool MarkedToKick { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public SignonState_t SignonState => throw new NotSupportedException();
    public bool FakePlayer => throw new NotSupportedException();
    public bool SendingSnapshots => throw new NotSupportedException();
    public int UserID => throw new NotSupportedException();
    public bool ReceivedPacket => throw new NotSupportedException();
    public ref CSteamID DisconnectedSteamID => throw new NotSupportedException();
    public ref CSteamID AuthTicketSteamID => throw new NotSupportedException();
    public ref CSteamID FriendsID => throw new NotSupportedException();
    public ref NSAddress IPAddress => throw new NotSupportedException();
    public unsafe KeyValues* Convars => throw new NotSupportedException();
    public bool ConvarsChanged => throw new NotSupportedException();
    public bool IsHLTV => throw new NotSupportedException();
    public uint SendtableCRC => throw new NotSupportedException();
    public uint ChallengeID => throw new NotSupportedException();
    public int SignonTick => throw new NotSupportedException();
    public int DeltaTick { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public int StringTableAckTick => throw new NotSupportedException();
    public nint Address => throw new NotSupportedException();
    public ref CUtlVector<nint> LoadedSpawnGroups => throw new NotSupportedException();
    public ref CBitVec16384 BaselinesSent => throw new NotSupportedException();
    public int BaselineUsed => throw new NotSupportedException();
    public int LoadingProgress => throw new NotSupportedException();
    public int ForceWaitForTick => throw new NotSupportedException();
    public bool LowViolence => throw new NotSupportedException();
    public bool FullyAuthenticated => throw new NotSupportedException();
    public float NextMessageTime => throw new NotSupportedException();
    public float AuthenticatedTime => throw new NotSupportedException();
    public float SnapshotInterval => throw new NotSupportedException();
    public int SpamCommandsCount => throw new NotSupportedException();
    public ref CNetworkStatTrace NetworkStatTrace => throw new NotSupportedException();
    public double LastExecutedCommand => throw new NotSupportedException();
    public bool ShouldSendMessages => throw new NotSupportedException();
    public void Reconnect() => throw new NotSupportedException();
    public void SetRate(int rate) => throw new NotSupportedException();
    public void SetUpdateRate(float updateRate) => throw new NotSupportedException();
    public int GetRate() => throw new NotSupportedException();
    public void ForceFullUpdate() => throw new NotSupportedException();
    public void UpdateSendState() => throw new NotSupportedException();
    public void UpdateUserSettings() => throw new NotSupportedException();
    public void ResetUserSettings() => throw new NotSupportedException();
    public void FreeBaselines() => throw new NotSupportedException();
    public void MarkToKick() => throw new NotSupportedException();
    public void UnmarkToKick() => throw new NotSupportedException();
}
