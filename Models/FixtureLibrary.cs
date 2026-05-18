using System.Collections.Generic;
using System.Linq;

namespace MDMX_MaskCreator;


public class FixtureLibrary
{
    private readonly Dictionary<string, DmxFixture> _fixtures;
    public FixtureLibrary(IEnumerable<DmxFixture> fixtures)
    {
        _fixtures = fixtures.ToDictionary(f => f.Name);
    }
    
    public DmxFixture? Get(string name) => _fixtures.TryGetValue(name, out var fixture) ? fixture : null;
    
    public bool Contains(string name) => _fixtures.ContainsKey(name);
    
    public int Count => _fixtures.Count;
    
    public static FixtureLibrary LoadFromCsv(string path) => new(DmxFixture.ParseFixtures(path));
    
}