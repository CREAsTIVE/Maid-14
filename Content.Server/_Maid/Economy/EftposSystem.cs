using Content.Shared._Maid.Economy;
using Content.Shared.Access.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;

namespace Content.Server._Maid.Economy;

public sealed class EftposSystem : EntitySystem
{
    [Dependency] private readonly BankAccountSystem _bankAccount = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EftposComponent, EftposLockMessage>(OnLock);
        SubscribeLocalEvent<EftposComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(Entity<EftposComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled
            || ent.Comp.BankAccountId is not { } receiver
            || ent.Comp.Amount <= 0
            || !TryComp<BankCardComponent>(args.Used, out var bankCard)
            || bankCard.AccountId is not { } payer
            || payer == receiver)
            return;

        args.Handled = true;

        if (_bankAccount.TryTransfer(payer, receiver, ent.Comp.Amount))
        {
            _popup.PopupEntity(Loc.GetString("eftpos-transaction-success"), ent);
            _audio.PlayPvs(ent.Comp.SoundApply, ent);
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("eftpos-transaction-error"), ent);
            _audio.PlayPvs(ent.Comp.SoundDeny, ent);
        }
    }

    private void OnLock(Entity<EftposComponent> ent, ref EftposLockMessage args)
    {
        if (!_hands.TryGetActiveItem(args.Actor, out var card)
            || !TryComp<BankCardComponent>(card, out var bankCard)
            || bankCard.AccountId is not { } accountId)
            return;

        if (ent.Comp.BankAccountId == null)
        {
            ent.Comp.BankAccountId = accountId;
            ent.Comp.Amount = Math.Max(args.Amount, 0);
        }
        else if (ent.Comp.BankAccountId == accountId)
        {
            ent.Comp.BankAccountId = null;
            ent.Comp.Amount = 0;
        }

        var state = new EftposBuiState
        {
            Locked = ent.Comp.BankAccountId != null,
            Amount = ent.Comp.Amount,
            Owner = GetOwnerName(card.Value, ent.Comp.BankAccountId),
        };

        _ui.SetUiState(ent.Owner, EftposUiKey.Key, state);
    }

    private string GetOwnerName(EntityUid card, int? accountId)
    {
        if (accountId == null || !_bankAccount.TryGetAccount(accountId.Value, out var account))
            return string.Empty;

        if (TryComp<IdCardComponent>(card, out var idCard) && idCard.FullName != null)
            return idCard.FullName;

        return account.Name == string.Empty ? account.AccountId.ToString() : account.Name;
    }
}
