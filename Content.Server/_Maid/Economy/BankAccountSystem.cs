using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Access.Systems;
using Content.Server.CartridgeLoader;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Server.Roles;
using Content.Server.Roles.Jobs;
using Content.Shared._Maid.CVars;
using Content.Shared._Maid.Economy;
using Content.Shared.GameTicking;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.Roles.Jobs;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Maid.Economy;

/// <summary>
/// Handles personal bank accounts, bank cards and salaries.
/// </summary>
public sealed class BankAccountSystem : EntitySystem
{
    [Dependency] private readonly BankCartridgeSystem _bankCartridge = default!;
    [Dependency] private readonly CartridgeLoaderSystem _cartridgeLoader = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IdCardSystem _idCard = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly JobSystem _job = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    private static readonly ProtoId<SalaryPrototype> Salaries = "Salaries";

    private readonly Dictionary<int, BankAccount> _accounts = new();
    private TimeSpan _nextSalary;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BankCardComponent, ComponentStartup>(OnCardStartup);
        SubscribeLocalEvent<JobRoleComponent, GetBriefingEvent>(OnGetBriefing);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawned);
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarted);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_gameTicker.RunLevel != GameRunLevel.InRound || _timing.CurTime < _nextSalary)
            return;

        _nextSalary = _timing.CurTime + GetSalaryInterval();
        PaySalaries();
    }

    #region Public API

    public BankAccount CreateAccount(int? accountId = null, int startingBalance = 0)
    {
        if (accountId != null && TryGetAccount(accountId.Value, out var existing))
            return existing;

        int id;
        do
        {
            id = accountId ?? _random.Next(100000, 1000000);
            accountId = null;
        } while (_accounts.ContainsKey(id));

        var account = new BankAccount(id, _random.Next(1000, 10000), startingBalance);
        _accounts.Add(id, account);
        return account;
    }

    public bool TryGetAccount(int accountId, [NotNullWhen(true)] out BankAccount? account)
    {
        return _accounts.TryGetValue(accountId, out account);
    }

    public int GetBalance(int accountId)
    {
        return TryGetAccount(accountId, out var account) ? account.Balance : 0;
    }

    /// <summary>
    /// Adds money to or takes money from an account. Fails if the account doesn't have enough money.
    /// </summary>
    public bool TryChangeBalance(int accountId, int amount)
    {
        if (!TryGetAccount(accountId, out var account) || account.Balance + amount < 0)
            return false;

        account.Balance += amount;

        if (account.CartridgeUid != null)
            _bankCartridge.UpdateUiState(account.CartridgeUid.Value);

        return true;
    }

    /// <summary>
    /// Moves money between two accounts.
    /// </summary>
    public bool TryTransfer(int fromAccountId, int toAccountId, int amount)
    {
        if (amount <= 0 || fromAccountId == toAccountId || !_accounts.ContainsKey(toAccountId))
            return false;

        if (!TryChangeBalance(fromAccountId, -amount))
            return false;

        TryChangeBalance(toAccountId, amount);
        return true;
    }

    #endregion

    private void OnCardStartup(Entity<BankCardComponent> ent, ref ComponentStartup args)
    {
        var account = CreateAccount(ent.Comp.AccountId, ent.Comp.StartingBalance);
        ent.Comp.AccountId = account.AccountId;
        Dirty(ent);
    }

    private void OnPlayerSpawned(PlayerSpawnCompleteEvent ev)
    {
        if (!_idCard.TryFindIdCard(ev.Mob, out var idCard) || !_mind.TryGetMind(ev.Mob, out var mindId, out _))
            return;

        var bankCard = EnsureComp<BankCardComponent>(idCard);
        if (bankCard.AccountId == null || !TryGetAccount(bankCard.AccountId.Value, out var account))
            return;

        account.Balance = GetSalary(mindId) + _cfg.GetCVar(MaidCVars.EconomyStartingBalance);
        account.Mind = mindId;
        account.Name = Name(ev.Mob);

        if (!_inventory.TryGetSlotEntity(ev.Mob, "id", out var pda))
            return;

        foreach (var program in _cartridgeLoader.GetInstalled(pda.Value))
        {
            if (!TryComp<BankCartridgeComponent>(program, out var cartridge))
                continue;

            cartridge.AccountId = account.AccountId;
            account.CartridgeUid = program;
            break;
        }
    }

    private void OnGetBriefing(Entity<JobRoleComponent> ent, ref GetBriefingEvent args)
    {
        var mind = args.Mind.Owner;
        var account = _accounts.Values.FirstOrDefault(account => account.Mind == mind);
        if (account == null)
            return;

        args.Append(Loc.GetString("bank-account-briefing",
            ("account", account.AccountId),
            ("pin", account.Pin)));
    }

    private void OnRoundStarted(RoundStartingEvent ev)
    {
        _nextSalary = _timing.CurTime + GetSalaryInterval();
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _accounts.Clear();
    }

    private void PaySalaries()
    {
        foreach (var account in _accounts.Values)
        {
            if (account.Mind is not { } mindId
                || !TryComp<MindComponent>(mindId, out var mind)
                || mind.UserId is not { } userId
                || !_player.TryGetSessionById(userId, out _)
                || mind.CurrentEntity is not { } body
                || _mobState.IsDead(body))
                continue;

            TryChangeBalance(account.AccountId, GetSalary(mindId));
        }

        _chat.DispatchGlobalAnnouncement(Loc.GetString("salary-pay-announcement"),
            colorOverride: Color.FromHex("#18abf5"));
    }

    private int GetSalary(EntityUid mindId)
    {
        if (!_job.MindTryGetJob(mindId, out var job)
            || !_prototype.TryIndex(Salaries, out var salaries)
            || !salaries.Salaries.TryGetValue(job.ID, out var salary))
            return 0;

        return salary;
    }

    private TimeSpan GetSalaryInterval()
    {
        return TimeSpan.FromSeconds(_cfg.GetCVar(MaidCVars.EconomySalaryInterval));
    }
}
