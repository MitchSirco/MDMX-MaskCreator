namespace MDMX_MaskCreator.ViewModels;
using ReactiveUI;

public class PatchedFixtureViewModel : ReactiveObject
{
    private bool _isSelected;
    public PatchedFixture Fixture { get; }
    
    public string Name => $"{Fixture.Definition.Name} - {Location}";
    public string Location => Fixture.Patch.Location;
    public int Universe => Fixture.Patch.Universe;
    public int Channel => Fixture.Patch.Channel;
    public int ChannelCount => Fixture.Definition.Channels;
    public int FixtureId => Fixture.Patch.FixtureNumber != null ? (int)Fixture.Patch.FixtureNumber : 0;
    
 
    // display string for the sidebar: "ch 19 · LR 1"
    public string Meta => $"ch {Channel} - {ChannelCount}ch - Fixture #: {FixtureId}";
    
    public bool IsSelected
    {
        get => _isSelected;
        set => this.RaiseAndSetIfChanged(ref _isSelected, value);
    }
    
    public PatchedFixtureViewModel(PatchedFixture fixture)
    {
        Fixture = fixture;
    }
}