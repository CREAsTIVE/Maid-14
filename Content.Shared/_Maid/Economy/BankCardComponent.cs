using Robust.Shared.GameStates;

namespace Content.Shared._Maid.Economy;

/// <summary>
/// An item linked to a bank account, e.g. an ID card.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BankCardComponent : Component
{
    /// <summary>
    /// Linked account. If not set, a new account is created on startup.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int? AccountId;

    [DataField]
    public int StartingBalance;
}
