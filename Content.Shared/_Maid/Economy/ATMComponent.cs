using Content.Shared.Containers.ItemSlots;
using Content.Shared.Stacks;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Maid.Economy;

[RegisterComponent, NetworkedComponent]
public sealed partial class ATMComponent : Component
{
    public const string CardSlotId = "card-slot";

    [DataField]
    public ItemSlot CardSlot = new();

    [DataField]
    public ProtoId<StackPrototype> CreditStackPrototype = "Credit";

    [DataField]
    public SoundSpecifier SoundInsertCurrency = new SoundPathSpecifier("/Audio/_Maid/Machines/polaroid2.ogg");

    [DataField]
    public SoundSpecifier SoundWithdrawCurrency = new SoundPathSpecifier("/Audio/_Maid/Machines/polaroid1.ogg");

    [DataField]
    public SoundSpecifier SoundDeny = new SoundPathSpecifier("/Audio/Machines/buzz-sigh.ogg");
}

[Serializable, NetSerializable]
public enum ATMUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class ATMRequestWithdrawMessage(int amount, int pin) : BoundUserInterfaceMessage
{
    public readonly int Amount = amount;
    public readonly int Pin = pin;
}

[Serializable, NetSerializable]
public sealed class ATMBuiState : BoundUserInterfaceState
{
    public bool HasCard;
    public string InfoMessage = string.Empty;
    public int AccountBalance;
}
