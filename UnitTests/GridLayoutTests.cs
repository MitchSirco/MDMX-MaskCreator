using MDMX_MaskCreator.Grid;

namespace MDMX_MaskCreator.UnitTests;

using Xunit;

public class GridLayoutTests
{
    [Fact]
    public void ToAbsoluteSlot_Universe1_Channel1_IsZero()
    {
        Assert.Equal(0, GridLayout.ToAbsoluteSlot(1, 1));
    }

    [Fact]
    public void ToAbsoluteSlot_Universe1_Channel512_IsLastOfUniverse1()
    {
        Assert.Equal(511, GridLayout.ToAbsoluteSlot(1, 512));
    }

    [Fact]
    public void ToAbsoluteSlot_Universe2_StartsAt512()
    {
        Assert.Equal(512, GridLayout.ToAbsoluteSlot(2, 1));
    }

    [Fact]
    public void ToAbsoluteSlot_Universe6_Channel3_Is2562()
    {
        Assert.Equal(2562, GridLayout.ToAbsoluteSlot(6, 3));
    }

    // --- ToColumnSlot ---

    [Fact]
    public void ToColumnSlot_Slot0_IsColumn0Slot0()
    {
        Assert.Equal((0, 0), GridLayout.ToColumnSlot(0));
    }

    [Fact]
    public void ToColumnSlot_Slot5_IsColumn0Slot5()
    {
        Assert.Equal((0, 5), GridLayout.ToColumnSlot(5));
    }

    [Fact]
    public void ToColumnSlot_Slot6_IsColumn1Slot0()
    {
        Assert.Equal((1, 0), GridLayout.ToColumnSlot(6));
    }

    [Fact]
    public void ToColumnSlot_Universe2Start_IsColumn85Slot2()
    {
        // universe 2 starts at absoluteSlot 512
        // 512 / 6 = 85 remainder 2
        Assert.Equal((85, 2), GridLayout.ToColumnSlot(512));
    }

    [Fact]
    public void ToColumnSlot_Universe3Start_IsColumn170Slot4()
    {
        // universe 3 starts at absoluteSlot 1024
        // 1024 / 6 = 170 remainder 4
        Assert.Equal((170, 4), GridLayout.ToColumnSlot(1024));
    }

    [Fact]
    public void ToColumnSlot_Universe4Start_IsColumn256Slot0()
    {
        // universe 4 starts at absoluteSlot 1536
        // 1536 / 6 = 256 remainder 0
        Assert.Equal((256, 0), GridLayout.ToColumnSlot(1536));
    }

    // --- ToPixelRect ---

    [Fact]
    public void ToPixelRect_Column0_Slot0_IsOrigin()
    {
        var rect = GridLayout.ToPixelRect(0, 0);
        Assert.Equal(0, rect.Left);
        Assert.Equal(0, rect.Top);
        Assert.Equal(4, rect.Width);
        Assert.Equal(32, rect.Height);
    }

    [Fact]
    public void ToPixelRect_Column1_Slot0_IsX4Y0()
    {
        var rect = GridLayout.ToPixelRect(1, 0);
        Assert.Equal(4, rect.Left);
        Assert.Equal(0, rect.Top);
    }

    [Fact]
    public void ToPixelRect_Column0_Slot1_IsX0Y32()
    {
        var rect = GridLayout.ToPixelRect(0, 1);
        Assert.Equal(0, rect.Left);
        Assert.Equal(32, rect.Top);
    }

    [Fact]
    public void ToPixelRect_Column3_Slot5_IsCorrect()
    {
        var rect = GridLayout.ToPixelRect(3, 5);
        Assert.Equal(12, rect.Left); // 3 * 4
        Assert.Equal(160, rect.Top); // 5 * 32
    }

    // --- ResolveSlots ---

    [Fact]
    public void ResolveSlots_LightRobot1_StartsAtColumn427Slot0()
    {
        var fixture = MakePatchedFixture(universe: 6, channel: 3, alignment: 0, channelCount: 9);
        var slots = GridLayout.ResolveSlots(fixture);

        Assert.Equal(9, slots.Count);
        Assert.Equal((427, 0), slots[0]);
    }

    [Fact]
    public void ResolveSlots_LightRobot1_EndsAtColumn428Slot2()
    {
        var fixture = MakePatchedFixture(universe: 6, channel: 3, alignment: 0, channelCount: 9);
        var slots = GridLayout.ResolveSlots(fixture);

        Assert.Equal((428, 2), slots[8]);
    }

    [Fact]
    public void ResolveSlots_LightRobot2_StartsAtColumn428Slot3()
    {
        // packed immediately after LightRobot1 in the same column
        var fixture = MakePatchedFixture(universe: 6, channel: 12, alignment: 3, channelCount: 9);
        var slots = GridLayout.ResolveSlots(fixture);

        Assert.Equal((428, 3), slots[0]);
    }

    [Fact]
    public void ResolveSlots_LightRobot2_EndsAtColumn429Slot5()
    {
        var fixture = MakePatchedFixture(universe: 6, channel: 12, alignment: 3, channelCount: 9);
        var slots = GridLayout.ResolveSlots(fixture);

        Assert.Equal((429, 5), slots[8]);
    }

    [Fact]
    public void ResolveSlots_WrapsAcrossColumns()
    {
        // a 7-channel fixture starting at slot 4 should wrap into the next column
        var fixture = MakePatchedFixture(universe: 1, channel: 5, alignment: 4, channelCount: 7);
        var slots = GridLayout.ResolveSlots(fixture);

        Assert.Equal((0, 4), slots[0]);
        Assert.Equal((0, 5), slots[1]);
        Assert.Equal((1, 0), slots[2]); // wraps here
        Assert.Equal((1, 4), slots[6]);
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
}