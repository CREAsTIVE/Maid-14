using Content.Server.Stack;
using Content.Shared._Maid.Economy;
using Content.Shared.Cargo.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Emag.Systems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;

namespace Content.Server._Maid.Economy;

public sealed class ATMSystem : SharedATMSystem
{
    [Dependency] private readonly BankAccountSystem _bankAccount = default!;
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StackSystem _stack = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ATMComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ATMComponent, EntInsertedIntoContainerMessage>(OnCardInserted);
        SubscribeLocalEvent<ATMComponent, EntRemovedFromContainerMessage>(OnCardRemoved);
        SubscribeLocalEvent<ATMComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<ATMComponent, ATMRequestWithdrawMessage>(OnWithdrawRequest);
        SubscribeLocalEvent<ATMComponent, GotEmaggedEvent>(OnEmagged);
    }

    private void OnStartup(Entity<ATMComponent> ent, ref ComponentStartup args)
    {
        UpdateUiState(ent);
    }

    private void OnCardInserted(Entity<ATMComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ATMComponent.CardSlotId)
            return;

        UpdateUiState(ent);
    }

    private void OnCardRemoved(Entity<ATMComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ATMComponent.CardSlotId)
            return;

        UpdateUiState(ent);
    }

    private void OnEmagged(Entity<ATMComponent> ent, ref GotEmaggedEvent args)
    {
        // An emagged ATM doesn't check the PIN.
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction) || _emag.CheckFlag(ent, EmagType.Interaction))
            return;

        args.Handled = true;
    }

    private void OnInteractUsing(Entity<ATMComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled
            || !HasComp<CashComponent>(args.Used)
            || !TryComp<StackComponent>(args.Used, out var stack)
            || stack.StackTypeId != ent.Comp.CreditStackPrototype.Id)
            return;

        args.Handled = true;

        if (!TryGetCardAccount(ent, out var accountId))
        {
            _popup.PopupEntity(Loc.GetString("atm-trying-insert-cash-error"), ent, args.User, PopupType.Medium);
            _audio.PlayPvs(ent.Comp.SoundDeny, ent);
            return;
        }

        _bankAccount.TryChangeBalance(accountId, stack.Count);
        QueueDel(args.Used);

        _audio.PlayPvs(ent.Comp.SoundInsertCurrency, ent);
        UpdateUiState(ent);
    }

    private void OnWithdrawRequest(Entity<ATMComponent> ent, ref ATMRequestWithdrawMessage args)
    {
        if (args.Amount <= 0 || !TryGetCardAccount(ent, out var accountId)
            || !_bankAccount.TryGetAccount(accountId, out var account))
            return;

        if (account.Pin != args.Pin && !_emag.CheckFlag(ent, EmagType.Interaction))
        {
            _popup.PopupEntity(Loc.GetString("atm-wrong-pin"), ent, args.Actor);
            _audio.PlayPvs(ent.Comp.SoundDeny, ent);
            return;
        }

        if (!_bankAccount.TryChangeBalance(accountId, -args.Amount))
        {
            _popup.PopupEntity(Loc.GetString("atm-not-enough-cash"), ent, args.Actor);
            _audio.PlayPvs(ent.Comp.SoundDeny, ent);
            return;
        }

        _stack.Spawn(args.Amount, ent.Comp.CreditStackPrototype, Transform(ent).Coordinates);
        _audio.PlayPvs(ent.Comp.SoundWithdrawCurrency, ent);
        UpdateUiState(ent);
    }

    private bool TryGetCardAccount(Entity<ATMComponent> ent, out int accountId)
    {
        accountId = default;

        if (_itemSlots.GetItemOrNull(ent, ATMComponent.CardSlotId) is not { } card
            || !TryComp<BankCardComponent>(card, out var bankCard)
            || bankCard.AccountId is not { } id)
            return false;

        accountId = id;
        return true;
    }

    private void UpdateUiState(Entity<ATMComponent> ent)
    {
        var hasCard = TryGetCardAccount(ent, out var accountId);
        var state = new ATMBuiState
        {
            HasCard = hasCard,
            AccountBalance = hasCard ? _bankAccount.GetBalance(accountId) : 0,
            InfoMessage = Loc.GetString(hasCard ? "atm-ui-select-withdraw-amount" : "atm-ui-insert-card"),
        };

        _ui.SetUiState(ent.Owner, ATMUiKey.Key, state);
    }
}
