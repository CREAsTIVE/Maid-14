
using Content.Server._Maid.Terminator.Components;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Temperature.Systems;
using Robust.Shared.Timing;

namespace Content.Server._Maid.Terminator.EntitySystems;

public sealed class HeatOverTimeSystem : EntitySystem
{
    [Dependency] private readonly FlammableSystem _flammable = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly TemperatureSystem _temperature = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HeatOverTimeComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(EntityUid uid, HeatOverTimeComponent component, ref ComponentStartup args)
    {
        if (component.NextTickTime == TimeSpan.Zero)
            component.NextTickTime = _timing.CurTime;
    }

    public override void Update(float frameTime)
    {
        var currentTime = _timing.CurTime;
        var query = EntityQueryEnumerator<HeatOverTimeComponent>();

        while (query.MoveNext(out var uid, out var component))
        {
            if (component.Interval <= TimeSpan.Zero)
                continue;

            while (currentTime >= component.NextTickTime)
            {
                _temperature.ChangeHeat(uid, component.Heat * component.Multiplier, component.IgnoreHeatResistance);
                if (component.FireStacks > 0f)
                {
                    _flammable.AdjustFireStacks(uid,
                        component.FireStacks * component.Multiplier,
                        null,
                        true,
                        component.FireProtectionPenetration);
                }

                component.Multiplier += component.MultiplierIncrease;
                component.NextTickTime += component.Interval;
            }
        }
    }
}
