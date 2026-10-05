using Content.Server._Maid.Economy;
using Content.Shared._Maid.Economy;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._Maid;

[TestFixture]
public sealed class EconomyTest
{
    [Test]
    public async Task BankCardsAndTransfers()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.ResolveDependency<IEntityManager>();
        var bank = entMan.System<BankAccountSystem>();

        var map = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var card = entMan.SpawnEntity("PassengerIDCard", map.GridCoords);
            var bankCard = entMan.EnsureComponent<BankCardComponent>(card);
            Assert.That(bankCard.AccountId, Is.Not.Null);
            Assert.That(bank.TryGetAccount(bankCard.AccountId!.Value, out _));

            var from = bank.CreateAccount(startingBalance: 100);
            var to = bank.CreateAccount();

            Assert.That(bank.TryTransfer(from.AccountId, to.AccountId, 60));
            Assert.That(from.Balance, Is.EqualTo(40));
            Assert.That(to.Balance, Is.EqualTo(60));

            // Not enough money.
            Assert.That(bank.TryTransfer(from.AccountId, to.AccountId, 60), Is.False);
            Assert.That(from.Balance, Is.EqualTo(40));

            entMan.DeleteEntity(card);
        });

        await pair.CleanReturnAsync();
    }
}
