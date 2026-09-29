using Content.Server._Maid.Mood;
using Content.Shared._Maid.Mood;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._Maid;

[TestFixture]
public sealed class MoodTest
{
    [Test]
    public async Task MoodEffectsChangeMood()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.ResolveDependency<IEntityManager>();
        var mood = entMan.System<MoodSystem>();

        var map = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var mob = entMan.SpawnEntity("MobHuman", map.GridCoords);
            var comp = entMan.GetComponent<MoodComponent>(mob);
            var initial = comp.CurrentMoodLevel;

            mood.ApplyEffect((mob, comp), "BeingHugged");
            Assert.That(comp.CurrentMoodLevel, Is.GreaterThan(initial));

            mood.RemoveEffect((mob, comp), "BeingHugged");
            Assert.That(comp.CurrentMoodLevel, Is.EqualTo(initial));

            // Effects of the same category replace each other.
            mood.ApplyEffect((mob, comp), "HungerStarving");
            var starving = comp.CurrentMoodLevel;
            mood.ApplyEffect((mob, comp), "HungerOverfed");
            Assert.That(comp.CurrentMoodLevel, Is.GreaterThan(starving));
            Assert.That(comp.CategorisedEffects["Hunger"].Id, Is.EqualTo("HungerOverfed"));

            entMan.DeleteEntity(mob);
        });

        await pair.CleanReturnAsync();
    }
}
