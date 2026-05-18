using System.Collections.ObjectModel;
using System.Reactive;
using ReactiveUI;

namespace MDMX_MaskCreator.ViewModels;

public class UniverseGroupViewModel : ReactiveObject
{
    private bool _isExpanded = true;
    public int FixtureCount => Fixtures.Count;
    public ReactiveCommand<Unit, bool> ToggleExpandedCommand { get; }

    public int Universe { get; }
    public string Label => $"Universe {Universe}";
    public ObservableCollection<PatchedFixtureViewModel> Fixtures { get; } = new();
    
    public bool IsExpanded
    {
        get => _isExpanded;
        set => this.RaiseAndSetIfChanged(ref _isExpanded, value);
    }
    
    // select/deselect all fixtures in this universe at once
    public void SelectAll()
    {
        foreach (var f in Fixtures)
            f.IsSelected = true;
    }

    public void DeselectAll()
    {
        foreach (var f in Fixtures)
            f.IsSelected = false;
    }

    public UniverseGroupViewModel(int universe)
    {
        Universe = universe;
        ToggleExpandedCommand = ReactiveCommand.Create(() =>
            IsExpanded = !IsExpanded);
    }
}