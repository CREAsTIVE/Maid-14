using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Maid.Economy;

/// <summary>
/// A payment terminal. Its owner locks it with a card and an amount, then customers swipe their cards to pay.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class EftposComponent : Component
{
    [ViewVariables]
    public int? BankAccountId;

    [ViewVariables]
    public int Amount;

    [DataField]
    public SoundSpecifier SoundApply = new SoundPathSpecifier("/Audio/Machines/chime.ogg");

    [DataField]
    public SoundSpecifier SoundDeny = new SoundPathSpecifier("/Audio/Machines/buzz-sigh.ogg");
}

[Serializable, NetSerializable]
public enum EftposUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class EftposBuiState : BoundUserInterfaceState
{
    public bool Locked;
    public int Amount;
    public string Owner = string.Empty;
}

[Serializable, NetSerializable]
public sealed class EftposLockMessage(int amount) : BoundUserInterfaceMessage
{
    public readonly int Amount = amount;
}
