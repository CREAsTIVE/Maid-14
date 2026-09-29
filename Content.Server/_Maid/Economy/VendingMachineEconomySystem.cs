using Content.Server.Cargo.Systems;
using Content.Server.Stack;
using Content.Shared._Maid.CVars;
using Content.Shared._Maid.Economy;
using Content.Shared.Access.Systems;
using Content.Shared.Cargo.Components;
using Content.Shared.Interaction;
using Content.Shared.PDA;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Stacks;
using Content.Shared.Tag;
using Content.Shared.VendingMachines;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Server._Maid.Economy;

/// <summary>
/// Makes vending machines sell their items for cash or money from the buyer's bank account.
/// </summary>
public sealed class VendingMachineEconomySystem : EntitySystem
{
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly BankAccountSystem _bankAccount = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly PricingSystem _pricing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _power = default!;
    [Dependency] private readonly StackSystem _stack = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    private static readonly ProtoId<TagPrototype> IgnoreBalanceChecksTag = "IgnoreBalanceChecks";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VendingMachineComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<VendingMachineComponent, VendingMachinePurchaseAttemptEvent>(OnPurchaseAttempt);
        SubscribeLocalEvent<VendingMachineComponent, VendingMachineWithdrawMessage>(OnWithdraw);
    }

    /// <summary>
    /// Base price of an item sold by vending machines.
    /// </summary>
    public int GetItemPrice(EntityPrototype prototype)
    {
        var price = (int) _pricing.GetEstimatedPrice(prototype);
        if (price <= 0)
            price = _cfg.GetCVar(MaidCVars.EconomyVendingDefaultPrice);

        return (int) (price * _cfg.GetCVar(MaidCVars.EconomyVendingPriceMultiplier));
    }

    private void OnInteractUsing(Entity<VendingMachineComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled
            || ent.Comp.Broken
            || !_power.IsPowered(ent.Owner)
            || !HasComp<CashComponent>(args.Used)
            || !TryComp<StackComponent>(args.Used, out var stack)
            || stack.StackTypeId != ent.Comp.CreditStackPrototype.Id)
            return;

        args.Handled = true;

        ent.Comp.Credits += stack.Count;
        Dirty(ent);
        QueueDel(args.Used);

        _audio.PlayPvs(ent.Comp.SoundInsertCurrency, ent);
    }

    private void OnWithdraw(Entity<VendingMachineComponent> ent, ref VendingMachineWithdrawMessage args)
    {
        if (ent.Comp.Credits <= 0)
            return;

        _stack.Spawn(ent.Comp.Credits, ent.Comp.CreditStackPrototype, Transform(ent).Coordinates);
        ent.Comp.Credits = 0;
        Dirty(ent);

        _audio.PlayPvs(ent.Comp.SoundWithdrawCurrency, ent);
    }

    private void OnPurchaseAttempt(Entity<VendingMachineComponent> ent, ref VendingMachinePurchaseAttemptEvent args)
    {
        if (args.Paid)
            return;

        if (_tag.HasTag(args.User, IgnoreBalanceChecksTag))
        {
            args.Paid = true;
            return;
        }

        // Cash inserted into the machine is spent first.
        if (ent.Comp.Credits >= args.Price)
        {
            ent.Comp.Credits -= args.Price;
            Dirty(ent);
            args.Paid = true;
            return;
        }

        foreach (var item in _accessReader.FindPotentialAccessItems(args.User))
        {
            var card = TryComp<PdaComponent>(item, out var pda) && pda.ContainedId is { } id ? id : item;

            if (!TryComp<BankCardComponent>(card, out var bankCard)
                || bankCard.AccountId is not { } accountId
                || !_bankAccount.TryChangeBalance(accountId, -args.Price))
                continue;

            args.Paid = true;
            return;
        }
    }
}
