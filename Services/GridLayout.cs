using System;
using System.Collections.Generic;

using System.Linq;
using SkiaSharp;

namespace MDMX_MaskCreator.Grid;

public static class GridLayout
{
    public const int SlotsPerColumn = 6;
    public const int BitsPerSlot = 8;
    public const int ParityBits = 4;
    public const int BitSize = 4; // pixels width x height
    public const int SlotHeight = BitsPerSlot * BitSize; // 32px
    public const int ColumnHeight = (SlotsPerColumn * BitsPerSlot + ParityBits) * BitSize; // 208px
    public const int ColumnWidth = BitSize; // 4px
    public const int ChannelsPerUniverse = 512;
    
    // universes in 0-7, channels in 0-511
    public static int ToAbsoluteSlot(int universe, int channel) => (universe - 1) * ChannelsPerUniverse + (channel - 1);

    public static (int column, int slot) ToColumnSlot(int absoluteSlot) =>
        (absoluteSlot / SlotsPerColumn, absoluteSlot % SlotsPerColumn);

    public static SKRect ToPixelRect(int column, int slot) => new SKRect(
        left: column * ColumnWidth,
        top: slot * SlotHeight,
        right: (column * ColumnWidth) + ColumnWidth,
        bottom: (slot * SlotHeight) + SlotHeight
    );
    
    //haha davinci resolve
    public static List<(int column, int slot)> ResolveSlots(PatchedFixture fixture)
    {
        // subtract alignment
        var baseAbsoluteUnit = ToAbsoluteSlot(fixture.Universe, fixture.StartChannel) - fixture.Alignment;

        return Enumerable.Range(0, fixture.ChannelCount)
            .Select(i => ToColumnSlot(baseAbsoluteUnit + fixture.Alignment + i))
            .ToList();
    }


}