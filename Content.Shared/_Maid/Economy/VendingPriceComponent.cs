namespace Content.Server._Maid.Economy;

[RegisterComponent]
public sealed partial class VendingPriceComponent : Component
{
    [DataField]
    public int Price;
}
