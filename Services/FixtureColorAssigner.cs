using System;
using System.Collections.Generic;
using System.Linq;
using MDMX_MaskCreator.Grid;
using SkiaSharp;

namespace MDMX_MaskCreator.Services; 


public record FixtureRun(string FixtureName, int StartColumn, int EndColumn);

public static class FixtureColorAssigner
{
   
    // a curated set of visually distinct colors
    private static readonly SKColor[] Palette =
    {
        new SKColor(230, 25,  75),   // 0  red
        new SKColor(0,   130, 200),  // 1  blue
        new SKColor(60,  180, 75),   // 2  green
        new SKColor(255, 165, 0),    // 3  orange
        new SKColor(145, 30,  180),  // 4  purple
        new SKColor(70,  240, 240),  // 5  cyan
        new SKColor(240, 50,  230),  // 6  magenta
        new SKColor(210, 245, 60),   // 7  lime
        new SKColor(0,   128, 128),  // 8  teal
        new SKColor(128, 0,   0),    // 9  maroon
        new SKColor(0,   0,   128),  // 10 navy
        new SKColor(255, 215, 180),  // 11 peach
        new SKColor(170, 110, 40),   // 12 brown
        new SKColor(230, 190, 255),  // 13 lavender
        new SKColor(128, 128, 0),    // 14 olive
        new SKColor(170, 255, 195),  // 15 mint
        new SKColor(250, 190, 190),  // 16 pink
        new SKColor(0,   60,  48),   // 17 dark green
        new SKColor(255, 250, 200),  // 18 cream
        new SKColor(128, 128, 128),  // 19 gray
    };
    
    public static Dictionary<string, SKColor> AssignColors(
        IEnumerable<string> fixtureTypeNames, int seed = 0)
    {
        var distinctNames = fixtureTypeNames
            .Distinct()
            .ToList(); // remove .OrderBy(n => n)
        var colors = new Dictionary<string, SKColor>();

        for (int i = 0; i < distinctNames.Count; i++)
        {
            if (i < Palette.Length)
            {
                // use palette directly by index — no hash, guaranteed unique
                colors[distinctNames[i]] = Palette[i];
            }
            else
            {
                // overflow — vary lightness of existing palette colors
                var baseColor = Palette[i % Palette.Length];
                float factor = 0.6f + (i / Palette.Length) * 0.15f;
                colors[distinctNames[i]] = new SKColor(
                    (byte)Math.Min(255, baseColor.Red * factor),
                    (byte)Math.Min(255, baseColor.Green * factor),
                    (byte)Math.Min(255, baseColor.Blue * factor));
            }
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
    
    public static List<FixtureRun> FindContiguousRuns(List<PatchedFixture> fixtures)
    {
        // build a column -> fixture name map
        var columnOwner = new Dictionary<int, string>();

        foreach (var fixture in fixtures)
        {
            var slots = GridLayout.ResolveSlots(fixture);
            foreach (var (column, _) in slots)
                columnOwner[column] = fixture.Name;
        }

        if (columnOwner.Count == 0)
            return [];

        var sortedColumns = columnOwner.Keys.OrderBy(c => c).ToList();
        var runs = new List<FixtureRun>();

        int runStart = sortedColumns[0];
        string runName = columnOwner[runStart];
        int prevColumn = runStart;

        for (int i = 1; i < sortedColumns.Count; i++)
        {
            int col = sortedColumns[i];
            string name = columnOwner[col];

            bool isContiguous = col == prevColumn + 1;
            bool sameFixture = name == runName;

            if (!isContiguous || !sameFixture)
            {
                runs.Add(new FixtureRun(runName, runStart, prevColumn));
                runStart = col;
                runName = name;
            }

            prevColumn = col;
        }

        runs.Add(new FixtureRun(runName, runStart, prevColumn));
        return runs;
    }

    
}