using Content.Shared.Eye;
using Content.Shared.Interaction;
using Robust.Shared.Containers;
using Robust.Shared.Map.Components;

namespace Content.Shared._Maid.Extraction;

/// <summary>
/// Zone visibility and range for the extractee, so they can use the marker from anywhere inside the zone.
/// </summary>
public sealed class ExtractionZoneSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExtractableComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ExtractableComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ExtractableComponent, GetVisMaskEvent>(OnGetVisMask);
        SubscribeLocalEvent<ExtractableComponent, InRangeOverrideEvent>(OnInRangeOverride);
    }

    private void OnStartup(Entity<ExtractableComponent> ent, ref ComponentStartup args)
    {
        _eye.RefreshVisibilityMask(ent.Owner);
    }

    private void OnShutdown(Entity<ExtractableComponent> ent, ref ComponentShutdown args)
    {
        _eye.RefreshVisibilityMask(ent.Owner);
    }

    private void OnGetVisMask(Entity<ExtractableComponent> ent, ref GetVisMaskEvent args)
    {
        if (ent.Comp.LifeStage <= ComponentLifeStage.Running)
            args.VisibilityMask |= (int) VisibilityFlags.MaidExtraction;
    }

    private void OnInRangeOverride(Entity<ExtractableComponent> ent, ref InRangeOverrideEvent args)
    {
        if (!TryComp<ExtractionZoneComponent>(args.Target, out var zone) || zone.Extractee != ent.Owner)
            return;

        args.Handled = true;
        args.InRange = IsInZone((args.Target, zone), ent.Owner);
    }

    /// <summary>
    /// Whether the user stands inside the square zone around the marker, on the same grid and not inside a container.
    /// </summary>
    public bool IsInZone(Entity<ExtractionZoneComponent> zone, EntityUid user)
    {
        if (_container.IsEntityOrParentInContainer(user))
            return false;

        var zoneXform = Transform(zone);
        if (zoneXform.GridUid is not { } grid
            || Transform(user).GridUid != grid
            || !TryComp<MapGridComponent>(grid, out var gridComp))
            return false;

        var zoneTile = _map.TileIndicesFor(grid, gridComp, _transform.GetMapCoordinates(zone, zoneXform));
        var userTile = _map.TileIndicesFor(grid, gridComp, _transform.GetMapCoordinates(user));
        var delta = userTile - zoneTile;

        return Math.Abs(delta.X) <= zone.Comp.Range && Math.Abs(delta.Y) <= zone.Comp.Range;
    }
}
