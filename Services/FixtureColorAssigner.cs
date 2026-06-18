using System;
using System.Collections.Generic;
using System.Linq;
using SkiaSharp;

namespace MDMX_MaskCreator.Services;

public static class FixtureColorAssigner
{
    
    // a curated set of visually distinct colors
    private static readonly SKColor[] Palette =
    {
        new SKColor(230, 25, 75),   // red
        new SKColor(60, 180, 75),   // green
        new SKColor(255, 165, 0),   // orange
        new SKColor(0, 130, 200),   // blue
        new SKColor(245, 130, 48),  // orange-brown
        new SKColor(145, 30, 180),  // purple
        new SKColor(70, 240, 240),  // cyan
        new SKColor(240, 50, 230),  // magenta
        new SKColor(210, 245, 60),  // lime
        new SKColor(250, 190, 190), // pink
        new SKColor(0, 128, 128),   // teal
        new SKColor(230, 190, 255), // lavender
        new SKColor(170, 110, 40),  // brown
        new SKColor(255, 250, 200), // cream
        new SKColor(128, 0, 0),     // maroon
        new SKColor(170, 255, 195), // mint
        new SKColor(128, 128, 0),   // olive
        new SKColor(255, 215, 180), // peach
        new SKColor(0, 0, 128),     // navy
    };
    
    public static Dictionary<string, SKColor> AssignColors(
        IEnumerable<string> fixtureTypeNames, int seed = 0)
    {
        var distinctNames = fixtureTypeNames.Distinct().OrderBy(n => n).ToList();
        var colors = new Dictionary<string, SKColor>();
        
        for (int i = 0; i < distinctNames.Count; i++)
        {
            // combine seed + index for deterministic but spread-out hues
            // deterministic shuffle order based on seed, but always picks from the fixed palette
            var index = (HashString(distinctNames[i] + seed) + i) % Palette.Length;
            colors[distinctNames[i]] = Palette[index];
        }

        return colors;

    }
    
    private static int HashString(string input)
    {
        unchecked
        {
            int hash = 17;
            foreach (var c in input)
                hash = hash * 31 + c;
            return Math.Abs(hash);
        }
    }
}