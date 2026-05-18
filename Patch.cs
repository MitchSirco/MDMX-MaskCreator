using System.Collections;
using System.Collections.Generic;

namespace MDMX_MaskCreator;

/// <summary>
/// Class file that contains
/// </summary>
public class PatchHeader
{
    string name;
    string description;
    string version;
    List<Patch> patches = null;
    
}

public class Patch
{
    string safe_to_patch = "Yes - Locked";
    int universe = 1;
    int channel = 1;
    Fixture fixture;
    string location = "World";
    int alignment = 0;
    int fixture_id = 1;
    
    public Patch(Fixture fixture)
    {
        this.fixture = fixture;
    }
    
}