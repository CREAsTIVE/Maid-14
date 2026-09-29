using Robust.Shared.Serialization;

namespace Content.Shared._Maid.Economy;

/// <summary>
/// Withdraws the cash inserted into a vending machine.
/// </summary>
[Serializable, NetSerializable]
public sealed class VendingMachineWithdrawMessage : BoundUserInterfaceMessage;

/// <summary>
/// Raised on a vending machine when a user tries to buy an item from it.
/// Payment is resolved on the server only.
/// </summary>
[ByRefEvent]
public record struct VendingMachinePurchaseAttemptEvent(EntityUid User, int Price, bool Paid = false);
