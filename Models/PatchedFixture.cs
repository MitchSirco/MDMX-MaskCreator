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
    
}