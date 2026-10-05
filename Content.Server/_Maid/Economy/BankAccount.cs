namespace Content.Server._Maid.Economy;

/// <summary>
/// A bank account. Accounts only live for the duration of a round.
/// </summary>
public sealed class BankAccount(int accountId, int pin, int balance)
{
    public readonly int AccountId = accountId;
    public readonly int Pin = pin;
    public int Balance = balance;

    /// <summary>
    /// Owner's name shown in the bank program and on payment terminals.
    /// </summary>
    public string Name = string.Empty;

    /// <summary>
    /// Mind of the owner. Salaries are only paid to accounts with an owner.
    /// </summary>
    public EntityUid? Mind;

    /// <summary>
    /// Bank program linked to this account, updated whenever the balance changes.
    /// </summary>
    public EntityUid? CartridgeUid;
}
