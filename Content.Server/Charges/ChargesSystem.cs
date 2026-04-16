using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.Power;

namespace Content.Server.Charges;

public sealed class ChargesSystem : SharedChargesSystem
{
    // MAID PR 21 START
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AutoRechargeComponent, PowerChangedEvent>(OnPowerChanged);
    }

    private void OnPowerChanged(Entity<AutoRechargeComponent> ent, ref PowerChangedEvent args)
    {
        if (!ent.Comp.RequirePower) return;
        if (!TryComp<LimitedChargesComponent>(ent, out var limitedChargesComp)) return;

        // Both gonna be updated anyway
        Dirty(ent, ent.Comp);
        Dirty(ent, limitedChargesComp);

        if (args.Powered)
        {
            // Reset timer so chargin is happening ONLY when powered
            limitedChargesComp.LastUpdate = _timing.CurTime;

            ent.Comp.Enabled = true;
            return;
        }
        // Store current charge and reset timer
        limitedChargesComp.LastCharges = GetCurrentCharges(new(ent, limitedChargesComp, ent.Comp));
        limitedChargesComp.LastUpdate = _timing.CurTime;

        ent.Comp.Enabled = false;
    }
    // MAID PR 21 END
}
