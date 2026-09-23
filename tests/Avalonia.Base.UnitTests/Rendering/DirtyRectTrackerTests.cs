using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.Composition.Server;
using Moq;
using Xunit;

namespace Avalonia.Base.UnitTests.Rendering;

public class DirtyRectTrackerTests
{
    [Fact]
    public void Single_Tracker_Clears_Final_Pixel_Rect_Once()
    {
        var context = new Mock<IDrawingContextImpl>();
        var tracker = new SingleDirtyRectTracker();

        tracker.AddRect(new LtrbRect(10, 10, 30, 30));
        tracker.FinalizeFrame(new LtrbRect(0, 0, 100, 100));
        tracker.Clear(context.Object, Colors.Transparent);

        context.Verify(
            x => x.Clear(Colors.Transparent, new Rect(9, 9, 22, 22)),
            Times.Once);
    }

    [Fact]
    public void Multi_Tracker_Clears_Each_Disjoint_Region()
    {
        var (tracker, context) = CreateMultiTracker(maxOverhead: 0);
        var bounds = new LtrbRect(0, 0, 200, 200);

        tracker.Initialize(bounds);
        tracker.AddRect(new LtrbRect(10, 10, 30, 30));
        tracker.AddRect(new LtrbRect(100, 20, 120, 40));
        tracker.FinalizeFrame(bounds);
        tracker.Clear(context.Object, Colors.Transparent);

        context.Verify(
            x => x.Clear(Colors.Transparent, new Rect(9, 9, 22, 22)),
            Times.Once);
        context.Verify(
            x => x.Clear(Colors.Transparent, new Rect(99, 19, 22, 22)),
            Times.Once);
        context.Verify(
            x => x.Clear(Colors.Transparent, new Rect(9, 9, 112, 32)),
            Times.Never);
    }

    [Fact]
    public void Multi_Tracker_Clears_Overlapping_Rectangles_As_Distinct_Regions()
    {
        var (tracker, context) = CreateMultiTracker(maxOverhead: 1);
        var bounds = new LtrbRect(0, 0, 200, 200);

        tracker.Initialize(bounds);
        tracker.AddRect(new LtrbRect(10, 10, 30, 30));
        tracker.AddRect(new LtrbRect(20, 20, 40, 40));
        tracker.FinalizeFrame(bounds);
        tracker.Clear(context.Object, Colors.Transparent);

        context.Verify(
            x => x.Clear(Colors.Transparent, new Rect(9, 9, 22, 22)),
            Times.Once);
        context.Verify(
            x => x.Clear(Colors.Transparent, new Rect(19, 19, 22, 22)),
            Times.Once);
        context.Verify(
            x => x.Clear(Colors.Transparent, new Rect(9, 9, 32, 32)),
            Times.Never);
    }

    [Fact]
    public void Region_Tracker_Clears_Each_Clip_Rect()
    {
        var (tracker, context) = CreateRegionTracker();
        var bounds = new LtrbRect(0, 0, 200, 200);

        tracker.AddRect(new LtrbRect(10, 10, 30, 30));
        tracker.AddRect(new LtrbRect(100, 20, 120, 40));
        tracker.FinalizeFrame(bounds);
        tracker.Clear(context.Object, Colors.Transparent);

        context.Verify(
            x => x.Clear(Colors.Transparent, new Rect(10, 10, 20, 20)),
            Times.Once);
        context.Verify(
            x => x.Clear(Colors.Transparent, new Rect(100, 20, 20, 20)),
            Times.Once);
    }

    [Fact]
    public void Tracker_Initialize_Clears_Cached_Clear_Rects()
    {
        var (tracker, context) = CreateMultiTracker(maxOverhead: 0);
        var bounds = new LtrbRect(0, 0, 200, 200);

        tracker.Initialize(bounds);
        tracker.AddRect(new LtrbRect(10, 10, 30, 30));
        tracker.FinalizeFrame(bounds);
        tracker.Clear(context.Object, Colors.Transparent);
        context.Invocations.Clear();

        tracker.Initialize(bounds);
        tracker.FinalizeFrame(bounds);
        tracker.Clear(context.Object, Colors.Transparent);

        context.Verify(
            x => x.Clear(It.IsAny<Color>(), It.IsAny<Rect>()),
            Times.Never);
    }

    private static (MultiDirtyRectTracker Tracker, Mock<IDrawingContextImpl> Context)
        CreateMultiTracker(double maxOverhead)
    {
        var region = new Mock<IPlatformRenderInterfaceRegion>();
        var platform = new Mock<IPlatformRenderInterface>();
        platform.Setup(x => x.CreateRegion()).Returns(region.Object);

        return (
            new MultiDirtyRectTracker(platform.Object, 8, maxOverhead),
            new Mock<IDrawingContextImpl>());
    }

    private static (RegionDirtyRectTracker Tracker, Mock<IDrawingContextImpl> Context)
        CreateRegionTracker()
    {
        var region = new Mock<IPlatformRenderInterfaceRegion>();
        region.SetupGet(x => x.Bounds).Returns(default(LtrbPixelRect));
        var platform = new Mock<IPlatformRenderInterface>();
        platform.Setup(x => x.CreateRegion()).Returns(region.Object);

        return (
            new RegionDirtyRectTracker(platform.Object),
            new Mock<IDrawingContextImpl>());
    }
}
