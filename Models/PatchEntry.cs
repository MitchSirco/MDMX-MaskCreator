using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;

namespace MDMX_MaskCreator;

/// <summary>
/// Class file that contains
/// </summary>
public class PatchEntry
{
    public string SafeToPatch { get; set; }     // "Yes - Locked", "wip"
    public int Universe { get; set; }
    public int Channel { get; set; }
    public string Fixture { get; set; }         // matches DmxFixture.Name
    public string Location { get; set; }
    public int Alignment { get; set; }
    public int? FixtureNumber { get; set; }     // nullable, often empty
    public string Notes { get; set; }
    
    public static List<PatchEntry> ParsePatch(string filePath)
    {
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        
        csv.Read();
        csv.ReadHeader();
        
        var entries = new List<PatchEntry>();

        int lastUniverse = 0;
        
        while (csv.Read())
        {
            if (csv.Parser.Record.All(string.IsNullOrWhiteSpace))
                continue;

            var channelRaw = csv.GetField("Channel");

            if (!int.TryParse(channelRaw, out var channel))
                continue;

            var fixtureNumRaw = csv.GetField("Fixture #");

            int universe;
            if (csv.TryGetField<int>("Universe", out var parsedUniverse))
            {
                universe = parsedUniverse;
                lastUniverse = universe;
            }
            else
                universe = lastUniverse;
            
            entries.Add(new PatchEntry
            {
                SafeToPatch = csv.GetField("Safe-to-patch") ?? string.Empty,
                Universe = universe,
                Channel = channel,
                Fixture = csv.GetField("Fixture") ?? string.Empty,
                Location = csv.GetField("Location") ?? string.Empty,
                // empty = 0
                Alignment = csv.TryGetField<int>("Alignment", out var alignment) ? alignment : 0,
                FixtureNumber = int.TryParse(fixtureNumRaw, out var fn) ? fn : null,
                Notes = csv.GetField("Notes") ?? string.Empty
            });
        }
        return entries;
    }
    
}