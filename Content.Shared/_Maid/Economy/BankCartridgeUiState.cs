using Content.Shared.CartridgeLoader;
using Robust.Shared.Serialization;

namespace Content.Shared._Maid.Economy;

[Serializable, NetSerializable]
public sealed class BankCartridgeUiState : BoundUserInterfaceState
{
    public int Balance;
    public int? AccountId;
    public string OwnerName = string.Empty;
    public string AccountLinkMessage = string.Empty;
    public string AccountLinkResult = string.Empty;
}

[Serializable, NetSerializable]
public sealed class BankAccountLinkMessage(int accountId, int pin) : CartridgeMessageEvent
{
    public readonly int AccountId = accountId;
    public readonly int Pin = pin;
}
