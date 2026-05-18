using MDMX_MaskCreator.Grid;
using SkiaSharp;

namespace MDMX_MaskCreator.UnitTests;

using Xunit;

public class MaskRendererTests
{
    private readonly MaskRenderer _renderer = new();

    [Fact]
    public void Render_EmptyFixtures_ProducesAllWhiteImage()
    {
        using var image = _renderer.Render([]);

        Assert.Equal(MaskRenderer.GridWidth, image.Width);
        Assert.Equal(MaskRenderer.GridHeight, image.Height);
        Assert.True(IsAllWhite(image));
    }

    [Fact]
    public void Render_SingleChannel_BlackensCorrectSlot()
    {
        // universe 1, channel 1, alignment 0, 1 channel
        // → column 0, slot 0 → x:0 y:0 w:4 h:32
        var fixture = MakePatchedFixture(universe: 1, channel: 1, alignment: 0, channelCount: 1);
        using var image = _renderer.Render([fixture]);

        // slot region should be black
        AssertRegionColor(image, x: 0, y: 0, w: 4, h: 32, expected: 0);

        // pixel just below slot should still be white
        AssertPixelColor(image, x: 0, y: 32, expected: 255);
    }

    [Fact]
    public void Render_SingleChannel_DoesNotBlackenAdjacentColumn()
    {
        var fixture = MakePatchedFixture(universe: 1, channel: 1, alignment: 0, channelCount: 1);
        using var image = _renderer.Render([fixture]);

        // column 1 should be untouched
        AssertPixelColor(image, x: 4, y: 0, expected: 255);
    }

    [Fact]
    public void Render_MultiChannelFixture_BlackensAllSlots()
    {
        // 6-channel fixture fills exactly one full column
        var fixture = MakePatchedFixture(universe: 1, channel: 1, alignment: 0, channelCount: 6);
        using var image = _renderer.Render([fixture]);

        // entire column 0 data region (192px) should be black
        AssertRegionColor(image, x: 0, y: 0, w: 4, h: 192, expected: 0);

        // parity region (y:192–207) should still be white — renderer doesn't touch it
        AssertPixelColor(image, x: 0, y: 192, expected: 255);
    }

    [Fact]
    public void Render_FixtureSpanningTwoColumns_BlackensBothColumns()
    {
        // 9-channel fixture spans into second column
        var fixture = MakePatchedFixture(universe: 1, channel: 1, alignment: 0, channelCount: 9);
        using var image = _renderer.Render([fixture]);

        // slots 0-5 of column 0 → fully black
        AssertRegionColor(image, x: 0, y: 0, w: 4, h: 192, expected: 0);

        // slots 0-2 of column 1 → black (3 channels × 32px = 96px)
        AssertRegionColor(image, x: 4, y: 0, w: 4, h: 96, expected: 0);

        // slot 3 of column 1 → white (not part of fixture)
        AssertPixelColor(image, x: 4, y: 96, expected: 255);
    }

    [Fact]
    public void Render_SlotOutsideGridBounds_DoesNotThrow()
    {
        // put a fixture way out of bounds
        var fixture = MakePatchedFixture(universe: 99, channel: 512, alignment: 0, channelCount: 1);
        var ex = Record.Exception(() => _renderer.Render([fixture]));
        Assert.Null(ex);
    }

    [Fact]
    public void Render_TwoFixturesSharingColumn_BlackensCorrectSlots()
    {
        // slot 0–2 of column 0
        var f1 = MakePatchedFixture(universe: 1, channel: 1, alignment: 0, channelCount: 3);
        // slot 3–5 of column 0
        var f2 = MakePatchedFixture(universe: 1, channel: 4, alignment: 3, channelCount: 3);

        using var image = _renderer.Render([f1]);

        // only f1 masked — slots 3-5 should still be white
        AssertPixelColor(image, x: 0, y: 96, expected: 255);

        using var imageBoth = _renderer.Render([f1, f2]);

        // both masked — full column black
        AssertRegionColor(imageBoth, x: 0, y: 0, w: 4, h: 192, expected: 0);
    }

    // --- helpers ---

    private static PatchedFixture MakePatchedFixture(
        int universe, int channel, int alignment, int channelCount)
    {
        var patch = new PatchEntry
        {
            Universe = universe,
            Channel = channel,
            Alignment = alignment,
            Fixture = "Test"
        };
        var definition = new DmxFixture
        {
            Name = "Test",
            Channels = channelCount
        };
        return new PatchedFixture(patch, definition);
    }

    private static bool IsAllWhite(SKBitmap bitmap)
    {
        for (int y = 0; y < bitmap.Height; y++)
            for (int x = 0; x < bitmap.Width; x++)
                if (bitmap.GetPixel(x, y).Red != 255)
                    return false;
        return true;
    }

    private static void AssertRegionColor(
        SKBitmap bitmap, int x, int y, int w, int h, byte expected)
    {
        for (int py = y; py < y + h; py++)
            for (int px = x; px < x + w; px++)
                Assert.Equal(expected, bitmap.GetPixel(px, py).Red);
    }

    private static void AssertPixelColor(SKBitmap bitmap, int x, int y, byte expected)
    {
        var pixel = bitmap.GetPixel(x, y);
        Assert.Equal(expected, pixel.Red); // gray8 — R=G=B
    }
}