using System;
using System.Collections.Generic;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;

namespace Content.Client._Maid.UserInterface.Chart;

/// <summary>
///     Control that manages and renders sub-renderers in local coordinate space.
/// </summary>
public sealed class ChartRenderer : Control
{
    private readonly List<IChartSubRenderer> _subRenderers = new();

    [ViewVariables(VVAccess.ReadWrite)]
    public float ViewportMinX { get; set; } = 0f;

    [ViewVariables(VVAccess.ReadWrite)]
    public float ViewportMaxX { get; set; } = 10f;

    [ViewVariables(VVAccess.ReadWrite)]
    public float ViewportMinY { get; set; } = 0f;

    [ViewVariables(VVAccess.ReadWrite)]
    public float ViewportMaxY { get; set; } = 10f;

    public IReadOnlyList<IChartSubRenderer> SubRenderers => _subRenderers;


    public ChartRenderer()
    {
        RectClipContent = true;
    }

    public void AddSubRenderer(IChartSubRenderer subRenderer)
    {
        _subRenderers.Add(subRenderer);
    }

    public void RemoveSubRenderer(IChartSubRenderer subRenderer)
    {
        _subRenderers.Remove(subRenderer);
    }

    public void ClearSubRenderers()
    {
        _subRenderers.Clear();
    }

    public void SetViewport(float minX, float maxX, float minY, float maxY)
    {
        ViewportMinX = minX;
        ViewportMaxX = maxX;
        ViewportMinY = minY;
        ViewportMaxY = maxY;
    }

    /// <summary>
    ///     Adjusts the viewport to fit all bounded charts added to this renderer with the smallest bounding box.
    /// </summary>
    public void AutoFit()
    {
        var minX = float.MaxValue;
        var maxX = float.MinValue;
        var minY = float.MaxValue;
        var maxY = float.MinValue;
        var hasData = false;

        foreach (var sub in _subRenderers)
        {
            if (sub is not IBoundedChartSubRenderer bounded)
                continue;

            var bounds = bounded.GetBounds();

            if (!bounds.HasValue)
                continue;

            minX = Math.Min(minX, bounds.Value.Left);
            maxX = Math.Max(maxX, bounds.Value.Right);
            minY = Math.Min(minY, bounds.Value.Bottom);
            maxY = Math.Max(maxY, bounds.Value.Top);
            hasData = true;
        }

        if (!hasData)
            return;

        // Pad collapsed ranges to avoid divide by zero / flatlines
        if (MathF.Abs(maxX - minX) < 1e-5f)
        {
            minX -= 1f;
            maxX += 1f;
        }

        if (MathF.Abs(maxY - minY) < 1e-5f)
        {
            minY -= 1f;
            maxY += 1f;
        }

        // Add 5% padding around the bounding box for a cleaner look
        var paddingX = (maxX - minX) * 0.05f;
        var paddingY = (maxY - minY) * 0.05f;

        SetViewport(minX - paddingX, maxX + paddingX, minY - paddingY, maxY + paddingY);
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var rangeX = ViewportMaxX - ViewportMinX;
        var rangeY = ViewportMaxY - ViewportMinY;

        if (MathF.Abs(rangeX) < 1e-6f)
            rangeX = 1f;
        if (MathF.Abs(rangeY) < 1e-6f)
            rangeY = 1f;
        var scaleX = PixelWidth / rangeX;
        var scaleY = -PixelHeight / rangeY; // Negative Y scale to flip Y-axis (Y-up)

        var posX = -ViewportMinX * scaleX;
        var posY = PixelHeight - ViewportMinY * scaleY;

        var position = new Vector2(posX, posY);
        var scale = new Vector2(scaleX, scaleY);
        var borders = new UIBox2(0, 0, PixelWidth, PixelHeight);
        var viewport = new Box2(ViewportMinX, ViewportMinY, ViewportMaxX, ViewportMaxY);
        var context = new DrawContext(this, borders, viewport, position, scale);

        foreach (var sub in _subRenderers)
        {
            sub.Draw(handle, context);
        }
    }

}
