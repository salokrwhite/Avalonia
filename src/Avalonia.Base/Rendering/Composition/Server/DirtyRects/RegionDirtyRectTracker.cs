using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform;
using Avalonia.Reactive;

namespace Avalonia.Rendering.Composition.Server;

internal class RegionDirtyRectTracker : IDirtyRectTracker
{
    private readonly IPlatformRenderInterfaceRegion _region;
    private readonly List<LtrbRect> _rects = new();
    private readonly List<Rect> _clearRects = new();
    private Random _random = new();

    public RegionDirtyRectTracker(IPlatformRenderInterface platformRender)
    {
        _region = platformRender.CreateRegion();
    }
    
    public void AddRect(LtrbRect rect) => _rects.Add(rect);

    private LtrbPixelRect GetInflatedPixelRect(LtrbRect rc)
    {
        var inflated = rc.Inflate(new Thickness(1)).IntersectOrEmpty(rc);
        var pixelRect = LtrbPixelRect.FromRectUnscaled(inflated);
        return pixelRect;
    }
    
    public void FinalizeFrame(LtrbRect bounds)
    {
        _region.Reset();
        _clearRects.Clear();
        foreach (var rc in _rects)
        {
            var pixelRect = GetInflatedPixelRect(rc);
            _region.AddRect(pixelRect);
            _clearRects.Add(pixelRect.ToRectUnscaled());
        }
        CombinedRect = _region.Bounds.ToLtrbRectUnscaled();
    }

    public IDisposable BeginDraw(IDrawingContextImpl ctx)
    {
        ctx.PushClip(_region);
        return Disposable.Create(ctx.PopClip);
    }

    public void Clear(IDrawingContextImpl context, Color color)
    {
        foreach (var rect in _clearRects)
            context.Clear(color, rect);
    }

    public bool IsEmpty => _rects.Count == 0;

    public bool Intersects(LtrbRect rect) => _region.Intersects(rect);

    public void Initialize(LtrbRect bounds)
    {
        _rects.Clear();
        _clearRects.Clear();
        _region.Reset();
    }

    public void Visualize(IDrawingContextImpl context)
    {
        context.DrawRegion(
            new ImmutableSolidColorBrush(
                new Color(150, (byte)_random.Next(255), (byte)_random.Next(255), (byte)_random.Next(255))),
            null, _region);
    }

    public LtrbRect CombinedRect { get; private set; }
    
}
