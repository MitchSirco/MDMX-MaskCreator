using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
    private string _filter = string.Empty;
    
    
    public ReactiveCommand<Unit, Unit> SelectAllCommand { get; }
    public ReactiveCommand<Unit, Unit> DeselectAllCommand { get; }
    
    public UniverseGroupViewModel(int universe)
    {
        Universe = universe;
        ToggleExpandedCommand = ReactiveCommand.Create(() => IsExpanded = !IsExpanded);
        SelectAllCommand = ReactiveCommand.Create(SelectAll);
        DeselectAllCommand = ReactiveCommand.Create(DeselectAll);
    }
    
    public UniverseGroupViewModel(int universe, IEnumerable<PatchedFixtureViewModel> fixtures)
    {
        Universe = universe;
        ToggleExpandedCommand = ReactiveCommand.Create(() => IsExpanded = !IsExpanded);
        foreach (var f in fixtures)
            Fixtures.Add(f);
    }

    
    public bool IsExpanded
    {
        get => _isExpanded;
        set => this.RaiseAndSetIfChanged(ref _isExpanded, value);
    }
    
    // select/deselect all fixtures in this universe at once
    public void SelectAll()
    {
        foreach (var f in FilteredFixtures)
            f.IsSelected = true;
    }

    public void DeselectAll()
    {
        foreach (var f in FilteredFixtures)
            f.IsSelected = false;
    }
    
    public void SetFilter(string filter)
    {
        _filter = filter;
        this.RaisePropertyChanged(nameof(FilteredFixtures));
    }

    public IEnumerable<PatchedFixtureViewModel> FilteredFixtures =>
        string.IsNullOrWhiteSpace(_filter)
            ? Fixtures
            : Fixtures.Where(f => f.Name.ToLower().Contains(_filter));



}