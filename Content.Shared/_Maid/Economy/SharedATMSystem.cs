using Content.Shared.Containers.ItemSlots;

namespace Content.Shared._Maid.Economy;

public abstract class SharedATMSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ATMComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ATMComponent, ComponentRemove>(OnRemove);
    }

    private void OnInit(Entity<ATMComponent> ent, ref ComponentInit args)
    {
        _itemSlots.AddItemSlot(ent, ATMComponent.CardSlotId, ent.Comp.CardSlot);
    }

    private void OnRemove(Entity<ATMComponent> ent, ref ComponentRemove args)
    {
        _itemSlots.RemoveItemSlot(ent, ent.Comp.CardSlot);
    }
}
