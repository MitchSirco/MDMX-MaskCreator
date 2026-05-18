using System;
using System.Collections.Generic;

namespace MDMX_MaskCreator;

public class PatchedFixture
{
    public PatchEntry Patch { get; }
    public DmxFixture Definition { get; }

    public PatchedFixture(PatchEntry patch, DmxFixture definition)
    {
        Patch = patch;
        Definition = definition;
    }
    
    // convenience passthroughs so callers don't need to dig into Patch/Definition
    public string Name => Definition.Name;
    public int Universe => Patch.Universe;
    public int StartChannel => Patch.Channel;
    public int Alignment => Patch.Alignment;
    public int ChannelCount => Definition.Channels;
    public string Location => Patch.Location;
    
    public static List<PatchedFixture> Resolve(
        IEnumerable<PatchEntry> entries,
        FixtureLibrary library)
    {
        var result = new List<PatchedFixture>();

        foreach (var entry in entries)
        {
            var definition = library.Get(entry.Fixture);
            if (definition is null)
            {
                Console.WriteLine($"Warning: '{entry.Fixture}' not found in library");
                continue;
            }
            result.Add(new PatchedFixture(entry, definition));
        }

        return result;
    }
    
}