using Content.Server.CartridgeLoader;
using Content.Shared._Maid.Economy;
using Content.Shared.CartridgeLoader;
using Content.Shared.PDA;

namespace Content.Server._Maid.Economy;

/// <summary>
/// PDA program that shows the balance of a linked bank account.
/// </summary>
public sealed class BankCartridgeSystem : EntitySystem
{
    [Dependency] private readonly BankAccountSystem _bankAccount = default!;
    [Dependency] private readonly CartridgeLoaderSystem _cartridgeLoader = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BankCartridgeComponent, CartridgeMessageEvent>(OnUiMessage);
        SubscribeLocalEvent<BankCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
        SubscribeLocalEvent<BankCartridgeComponent, CartridgeAddedEvent>(OnInstall);
        SubscribeLocalEvent<BankCartridgeComponent, CartridgeRemovedEvent>(OnRemove);
    }

    public void UpdateUiState(EntityUid cartridge)
    {
        if (!TryComp<BankCartridgeComponent>(cartridge, out var comp) || comp.Loader == null)
            return;

        UpdateUiState((cartridge, comp), comp.Loader.Value);
    }

    private void OnInstall(Entity<BankCartridgeComponent> ent, ref CartridgeAddedEvent args)
    {
        ent.Comp.Loader = args.Loader;
    }

    private void OnRemove(Entity<BankCartridgeComponent> ent, ref CartridgeRemovedEvent args)
    {
        ent.Comp.Loader = null;
    }

    private void OnUiReady(Entity<BankCartridgeComponent> ent, ref CartridgeUiReadyEvent args)
    {
        UpdateUiState(ent, args.Loader);
    }

    private void OnUiMessage(Entity<BankCartridgeComponent> ent, ref CartridgeMessageEvent args)
    {
        if (args is BankAccountLinkMessage message)
            LinkAccount(ent, message);

        UpdateUiState(ent, GetEntity(args.LoaderUid));
    }

    private void LinkAccount(Entity<BankCartridgeComponent> ent, BankAccountLinkMessage args)
    {
        if (!_bankAccount.TryGetAccount(args.AccountId, out var account) || args.Pin != account.Pin)
        {
            ent.Comp.AccountLinkResult = Loc.GetString("bank-program-ui-link-error");
            return;
        }

        ent.Comp.AccountLinkResult = Loc.GetString("bank-program-ui-link-success");

        if (args.AccountId != ent.Comp.AccountId)
        {
            if (ent.Comp.AccountId is { } oldId
                && _bankAccount.TryGetAccount(oldId, out var oldAccount)
                && oldAccount.CartridgeUid == ent.Owner)
                oldAccount.CartridgeUid = null;

            if (TryComp<BankCartridgeComponent>(account.CartridgeUid, out var oldCartridge))
                oldCartridge.AccountId = null;

            account.CartridgeUid = ent;
            ent.Comp.AccountId = args.AccountId;
        }

        // Link the ID card inside the PDA as well, unless it already has an account.
        if (!TryComp<PdaComponent>(GetEntity(args.LoaderUid), out var pda)
            || pda.ContainedId is not { } id
            || HasComp<BankCardComponent>(id))
            return;

        var bankCard = AddComp<BankCardComponent>(id);
        bankCard.AccountId = account.AccountId;
        Dirty(id, bankCard);
    }

    private void UpdateUiState(Entity<BankCartridgeComponent> ent, EntityUid loader)
    {
        var linkMessage = Loc.GetString("bank-program-ui-link-program") + '\n';
        if (TryComp<PdaComponent>(loader, out var pda) && pda.ContainedId is { } id)
        {
            linkMessage += TryComp<BankCardComponent>(id, out var bankCard) && bankCard.AccountId != null
                ? Loc.GetString("bank-program-ui-link-id-card-linked", ("account", bankCard.AccountId.Value))
                : Loc.GetString("bank-program-ui-link-id-card");
        }
        else
        {
            linkMessage += Loc.GetString("bank-program-ui-link-no-id-card");
        }

        var state = new BankCartridgeUiState
        {
            AccountLinkResult = ent.Comp.AccountLinkResult,
            AccountLinkMessage = linkMessage,
        };

        if (ent.Comp.AccountId is { } accountId && _bankAccount.TryGetAccount(accountId, out var account))
        {
            state.Balance = account.Balance;
            state.AccountId = account.AccountId;
            state.OwnerName = account.Name;
        }

        _cartridgeLoader.UpdateCartridgeUiState(loader, state);
    }
}
