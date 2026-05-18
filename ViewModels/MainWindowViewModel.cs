using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using MDMX_MaskCreator.Grid;
using ReactiveUI;
using SkiaSharp;

namespace MDMX_MaskCreator.ViewModels;

public class MainWindowViewModel: ReactiveObject
{
    private readonly MaskRenderer _renderer = new();
    private readonly PresetService _presetService = new();

    private Bitmap? _previewBitmap;
    private string _statusText = "No patch loaded";
    private bool _canExport;
    
    public ObservableCollection<UniverseGroupViewModel> Universes { get; } = new();

    public Bitmap? PreviewBitmap
    {
        get => _previewBitmap;
        private set => this.RaiseAndSetIfChanged(ref _previewBitmap, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => this.RaiseAndSetIfChanged(ref _statusText, value);
    }

    public bool CanExport
    {
        get => _canExport;
        private set => this.RaiseAndSetIfChanged(ref _canExport, value);
    }

    // commands — wired up in constructor
    public ReactiveCommand<Unit, Unit> LoadFixturesCommand { get; }
    public ReactiveCommand<Unit, Unit> LoadPatchCommand { get; }
    public ReactiveCommand<Unit, Unit> ExportMaskCommand { get; }
    public ReactiveCommand<Unit, Unit> SavePresetCommand { get; }
    public ReactiveCommand<Unit, Unit> LoadPresetCommand { get; }
    
    // internal state
    private FixtureLibrary? _library;
    public FixtureLibrary? Library
    {
        get => _library;
        private set => this.RaiseAndSetIfChanged(ref _library, value);
    }

    private List<PatchedFixture> _patch = new();
    
    public MainWindowViewModel()
    {
        var canLoadPatch = this.WhenAnyValue(x => x._library)
            .Select(lib => lib is not null);

        var canExport = this.WhenAnyValue(x => x.CanExport);

        LoadFixturesCommand = ReactiveCommand.CreateFromTask(LoadFixturesAsync);
        LoadPatchCommand = ReactiveCommand.CreateFromTask(LoadPatchAsync, canLoadPatch);
        ExportMaskCommand = ReactiveCommand.CreateFromTask(ExportMaskAsync, canExport);
        SavePresetCommand = ReactiveCommand.CreateFromTask(SavePresetAsync, canExport);
        LoadPresetCommand = ReactiveCommand.CreateFromTask(LoadPresetAsync, canExport);
    }
    
    private void RebuildFixtureList(List<PatchedFixture> patched)
    {
        Universes.Clear();

        var groups = patched
            .GroupBy(f => f.Universe)
            .OrderBy(g => g.Key);

        foreach (var group in groups)
        {
            var universeVm = new UniverseGroupViewModel(group.Key);

            foreach (var fixture in group.OrderBy(f => f.StartChannel))
            {
                var fixtureVm = new PatchedFixtureViewModel(fixture);

                // subscribe to selection changes — redraw preview whenever any checkbox changes
                fixtureVm
                    .WhenAnyValue(f => f.IsSelected)
                    .Subscribe(_ => RefreshPreview());

                universeVm.Fixtures.Add(fixtureVm);
            }

            Universes.Add(universeVm);
        }

        CanExport = Universes.Count > 0;
        UpdateStatus();
        RefreshPreview();
    }
    
    private void RefreshPreview()
    {
        var selected = AllFixtures().Where(f => f.IsSelected).Select(f => f.Fixture);
        var skBitmap = _renderer.Render(selected);
        PreviewBitmap = ConvertToAvaloniaBitmap(skBitmap);
    }

    private void UpdateStatus()
    {
        var fixtureCount = AllFixtures().Count();
        var universeCount = Universes.Count;
        var selectedCount = AllFixtures().Count(f => f.IsSelected);
        StatusText = $"Patch loaded · {fixtureCount} fixtures · {universeCount} universes · {selectedCount} masked";
    }
    
    private IEnumerable<PatchedFixtureViewModel> AllFixtures()
        => Universes.SelectMany(u => u.Fixtures);

    // converts SkiaSharp bitmap to Avalonia bitmap for display
    private static Bitmap ConvertToAvaloniaBitmap(SKBitmap skBitmap)
    {
        using var image = SKImage.FromBitmap(skBitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = new MemoryStream(data.ToArray());
        return new Bitmap(stream);
    }

    // --- file operations ---

    private async Task LoadFixturesAsync()
    {
        var path = await PickFileAsync("Load fixture library", "csv");
        if (path is null) return;

        Library = FixtureLibrary.LoadFromCsv(path);
        StatusText = $"Fixture library loaded · {Library.Count} definitions";
    }

    private async Task LoadPatchAsync()
    {
        if (_library is null) return;

        var path = await PickFileAsync("Load patch", "csv");
        if (path is null) return;

        var entries = PatchEntry.ParsePatch(path);
        _patch = PatchedFixture.Resolve(entries, _library);
        RebuildFixtureList(_patch);
    }

    private async Task ExportMaskAsync()
    {
        var path = await SaveFileAsync("Export mask PNG", "png");
        if (path is null) return;

        var selected = AllFixtures().Where(f => f.IsSelected).Select(f => f.Fixture);
        _renderer.RenderAndSave(selected, path);
    }

    private async Task SavePresetAsync()
    {
        var path = await SaveFileAsync("Save preset", "json");
        if (path is null) return;

        _presetService.Save(AllFixtures(), path);
    }

    private async Task LoadPresetAsync()
    {
        var path = await PickFileAsync("Load preset", "json");
        if (path is null) return;

        var selected = _presetService.Load(path);

        foreach (var fixture in AllFixtures())
            fixture.IsSelected = selected.Contains((fixture.Universe, fixture.Channel));

        UpdateStatus();
    }

    // --- file picker helpers ---
    // these need a reference to the Avalonia StorageProvider
    // we'll wire this up from the View via a service locator or constructor injection

    private Func<string, string, Task<string?>>? _pickFile;
    private Func<string, string, Task<string?>>? _saveFile;

    public void RegisterFilePickers(
        Func<string, string, Task<string?>> pickFile,
        Func<string, string, Task<string?>> saveFile)
    {
        _pickFile = pickFile;
        _saveFile = saveFile;
    }

    private Task<string?> PickFileAsync(string title, string extension)
        => _pickFile?.Invoke(title, extension) ?? Task.FromResult<string?>(null);

    private Task<string?> SaveFileAsync(string title, string extension)
        => _saveFile?.Invoke(title, extension) ?? Task.FromResult<string?>(null);
    
    
}