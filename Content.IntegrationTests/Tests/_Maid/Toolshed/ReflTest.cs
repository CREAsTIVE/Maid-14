using System.Linq;
using Content.IntegrationTests.Tests.Toolshed;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.TypeParsers.Math;

namespace Content.IntegrationTests.Tests._Maid.Toolshed;

[TestFixture]
public sealed class ReflTest : ToolshedTest
{
    [Test]
    public async Task TestValidReadAndWrite()
    {
        await Server.WaitAssertion(() =>
        {
            var entMan = Server.ResolveDependency<IEntityManager>();
            var testEnt = entMan.SpawnEntity(null, MapCoordinates.Nullspace);
            var xform = entMan.GetComponent<TransformComponent>(testEnt);

            // 1. Verify reading a component field
            var success = Toolshed.InvokeCommand(this, "refl:read Transform/GridUid", testEnt, out var result);
            Assert.Multiple(() =>
            {
                Assert.That(success, Is.True);
                Assert.That(HasErrors, Is.False);
                Assert.That(result, Is.EqualTo(xform.GridUid));
            });

            // 2. Verify writing a ReadWrite field
            var targetAngle = Angle.FromDegrees(45);
            success = Toolshed.InvokeCommand(this, "refl:write Transform/LocalRotation 45deg", testEnt, out result);
            Assert.Multiple(() =>
            {
                Assert.That(success, Is.True);
                Assert.That(HasErrors, Is.False);
                // Verify the returned value is the entity itself
                Assert.That(result, Is.EqualTo(testEnt));
                // Verify the rotation was updated in the component
                Assert.That(xform.LocalRotation.EqualsApprox(targetAngle), Is.True);
            });

            // 3. Verify reading back the written rotation
            success = Toolshed.InvokeCommand(this, "refl:read Transform/LocalRotation", testEnt, out result);
            Assert.Multiple(() =>
            {
                Assert.That(success, Is.True);
                Assert.That(HasErrors, Is.False);
                Assert.That(result, Is.AssignableTo<Angle>());
                Assert.That(((Angle)result!).EqualsApprox(targetAngle), Is.True);
            });
        });
    }

    // Verify that an invalid path fails parsing and does not crash the server,
    // cause it did few times while developing
    [Test]
    public async Task TestInvalidMemberPath()
    {
        await Server.WaitAssertion(() =>
        {
            var entMan = Server.ResolveDependency<IEntityManager>();
            var testEnt = entMan.SpawnEntity(null, MapCoordinates.Nullspace);

            ClearErrors();
            ExpectError<IConError>();
            ParseCommand("refl:read NonExistentComponent/Field", typeof(EntityUid));
            Assert.That(HasErrors, Is.True);

            ClearErrors();
            ExpectError<IConError>();
            ParseCommand("refl:read Transform/NonExistentField", typeof(EntityUid));
            Assert.That(HasErrors, Is.True);

            ClearErrors();
            ExpectError<IConError>();
            ParseCommand("refl:read /", typeof(EntityUid));
            Assert.That(HasErrors, Is.True);

            ClearErrors();
            ExpectError<IConError>();
            ParseCommand("refl:read Som/", typeof(EntityUid));
            Assert.That(HasErrors, Is.True);

            ClearErrors();
            ExpectError<IConError>();
            ParseCommand("refl:read /Transform/", typeof(EntityUid));
            Assert.That(HasErrors, Is.True);

            ClearErrors();
            ExpectError<IConError>();
            ParseCommand("refl:read Transform/", typeof(EntityUid));
            Assert.That(HasErrors, Is.True);
        });
    }

    [Test]
    public async Task TestInvalidTypeWrite()
    {
        await Server.WaitAssertion(() =>
        {
            var entMan = Server.ResolveDependency<IEntityManager>();
            var testEnt = entMan.SpawnEntity(null, MapCoordinates.Nullspace);

            ClearErrors();
            ExpectError<IConError>();
            ParseCommand("refl:write Transform/LocalRotation \"not_an_angle\"", typeof(EntityUid));

            // Should report error
            Assert.Multiple(() =>
            {
                Assert.That(HasErrors, Is.True);
                Assert.That(GetErrors().Any(e => e is InvalidAngle || e is ArgumentParseError), Is.True);
            });
        });
    }

    [Test]
    public async Task TestInvalidInputs()
    {
        await Server.WaitAssertion(() =>
        {
            ClearErrors();
            var success = Toolshed.InvokeCommand(this, "refl:read Transform/GridUid", EntityUid.Invalid, out var result);

            Assert.Multiple(() =>
            {
                Assert.That(success, Is.True); // For null it just should ignore
                Assert.That(result, Is.Null);
            });
        });
    }
}
