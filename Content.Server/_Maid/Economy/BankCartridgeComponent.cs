namespace Content.Server._Maid.Economy;

[RegisterComponent, Access(typeof(BankCartridgeSystem), typeof(BankAccountSystem))]
public sealed partial class BankCartridgeComponent : Component
{
    [ViewVariables]
    public int? AccountId;

    [ViewVariables]
    public EntityUid? Loader;

    [ViewVariables]
    public string AccountLinkResult = string.Empty;
}
