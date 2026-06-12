using Content.Goobstation.Client.IoC;
using Content.Goobstation.Client.JoinQueue;
using Robust.Shared.ContentPack;

namespace Content.Goobstation.Client.Entry;

public sealed class EntryPoint : GameClient
{
    [Dependency] private readonly JoinQueueManager _joinQueue = default!;

    public override void Init()
    {
        ContentGoobClientIoC.Register();

        IoCManager.BuildGraph();
        IoCManager.InjectDependencies(this);
    }

    public override void PostInit()
    {
        base.PostInit();

        _joinQueue.Initialize();
    }
}
