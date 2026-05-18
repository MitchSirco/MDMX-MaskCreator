using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace MDMX_MaskCreator.ViewModels;

public class PresetService
{
    private record PresetEntry(int Universe, int Channel);

    public void Save(IEnumerable<PatchedFixtureViewModel> selected, string path)
    {
        var entries = selected
            .Where(f => f.IsSelected)
            .Select(f => new PresetEntry(f.Universe, f.Channel))
            .ToList();

        var json = JsonSerializer.Serialize(entries, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        
        File.WriteAllText(path, json);
    }

    public HashSet<(int universe, int channel)> Load(string path)
    {
        var json = File.ReadAllText(path);
        var entries = JsonSerializer.Deserialize<List<PresetEntry>>(json) ?? [];

        return entries
            .Select(e => (e.Universe, e.Channel))
            .ToHashSet();
    }
}