using Content.Server._Maid.Triggers.Components;
using Content.Server.Explosion.EntitySystems;
using Content.Shared.Implants.Components;

namespace Content.Server._Maid.Triggers.EntitySystems;

public sealed class AddComponentsOnTriggerSystem : EntitySystem
{
    [Dependency] private IEntityManager _entityManager = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AddComponentsOnTriggerComponent, TriggerEvent>(OnAddComponentsTrigger);
    }

    private void OnAddComponentsTrigger(EntityUid uid, AddComponentsOnTriggerComponent component, ref TriggerEvent args)
    {
        if (!TryComp(uid, out SubdermalImplantComponent? implant) || implant.ImplantedEntity is null)
            return;

        _entityManager.AddComponents(implant.ImplantedEntity.Value, component.Components, component.RemoveExisting);
        args.Handled = true;
    }
}
