using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client._Maid.Mood;

public sealed class SaturationScaleOverlay : Overlay
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private static readonly ProtoId<ShaderPrototype> Shader = "SaturationScale";
    private const float Saturation = 0.5f;

    public override bool RequestScreenTexture => true;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private readonly ShaderInstance _shader;

    public SaturationScaleOverlay()
    {
        IoCManager.InjectDependencies(this);
        _shader = _prototype.Index(Shader).InstanceUnique();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _shader.SetParameter("saturation", Saturation);

        var handle = args.WorldHandle;
        handle.UseShader(_shader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }
}
