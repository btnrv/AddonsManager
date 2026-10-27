using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.NetMessages;
using SwiftlyS2.Shared.ProtobufDefinitions;

namespace FirstJoin;

internal sealed class TestSignonState : CNETMsg_SignonState
{
    public SignonState_t SignonState { get; set; }
    public string Addons { get; set; } = "";
    public uint SpawnCount { get; set; }
    public uint NumServerPlayers { get; set; }
    public string MapName { get; set; } = "";
    public IProtobufRepeatedFieldValueType<string> PlayersNetworkids => throw new NotSupportedException();
    public IProtobufAccessor Accessor => throw new NotSupportedException();
    public ref CRecipientFilter Recipients => throw new NotSupportedException();
    public bool IsValid => true;
    public nint Address => throw new NotSupportedException();
    public void DangerouslySetAddress(nint address) => throw new NotSupportedException();
    public void Send() => throw new NotSupportedException();
    public void SendToAllPlayers() => throw new NotSupportedException();
    public void SendToPlayer(int playerId) => throw new NotSupportedException();
    public void Dispose() { }
}
