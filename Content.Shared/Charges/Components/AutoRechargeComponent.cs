using Content.Shared.Charges.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Charges.Components;

/// <summary>
/// Something with limited charges that can be recharged automatically.
/// Requires LimitedChargesComponent to function.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedChargesSystem))]
public sealed partial class AutoRechargeComponent : Component
{
    /// <summary>
    /// The time it takes to regain a single charge
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan RechargeDuration = TimeSpan.FromSeconds(90);

    // MAID PR 21
    [DataField("requirePower"), AutoNetworkedField]
    public bool RequirePower = false;

    [DataField, AutoNetworkedField]
    public bool Enabled = true;
    // MAID PR 21 END
}
