using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using MDMX_MaskCreator.Grid;

namespace MDMX_MaskCreator;
public record DmxAddress(int Universe, int Channel);

public record DmxRange(DmxAddress Start, DmxAddress End)
{
    public IEnumerable<DmxAddress> Expand()
    {
        var startSlot = GridLayout.ToAbsoluteSlot(Start.Universe, Start.Channel);
        var endSlot = GridLayout.ToAbsoluteSlot(End.Universe, End.Channel);

        for (int slot = startSlot; slot <= endSlot; slot++)
        {
            int universe = (slot / 512) + 1;
            int channel = (slot % 512) + 1;
            yield return new DmxAddress(universe, channel);
        }
    }
}

public static class DmxRangeParser
{
    private static readonly Regex CrossUniverseRange =
        new(@"U(\d+):C(\d+)-U(\d+):C(\d+)", RegexOptions.IgnoreCase);
    private static readonly Regex SingleUniverseRange =
        new(@"U(\d+):C(\d+)-C(\d+)", RegexOptions.IgnoreCase);
    private static readonly Regex SingleChannel =
        new(@"U(\d+):C(\d+)", RegexOptions.IgnoreCase);

    public static List<DmxRange> Parse(string input)
    {
        var ranges = new List<DmxRange>();
        if (string.IsNullOrWhiteSpace(input))
            return ranges;

        foreach (var part in input.Split(',', StringSplitOptions.TrimEntries))
        {
            var range = TryParse(part);
            if (range is not null)
                ranges.Add(range);
        }

        return ranges;
    }

    private static DmxRange? TryParse(string part)
    {
        var m = CrossUniverseRange.Match(part);
        if (m.Success)
        {
            return new DmxRange(
                new DmxAddress(int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value)),
                new DmxAddress(int.Parse(m.Groups[3].Value), int.Parse(m.Groups[4].Value)));
        }

        m = SingleUniverseRange.Match(part);
        if (m.Success)
        {
            int universe = int.Parse(m.Groups[1].Value);
            return new DmxRange(
                new DmxAddress(universe, int.Parse(m.Groups[2].Value)),
                new DmxAddress(universe, int.Parse(m.Groups[3].Value)));
        }

        m = SingleChannel.Match(part);
        if (m.Success)
        {
            var ch = new DmxAddress(int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value));
            return new DmxRange(ch, ch);
        }

        return null;
    }
}
