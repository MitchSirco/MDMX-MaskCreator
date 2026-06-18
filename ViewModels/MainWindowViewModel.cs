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
using MDMX_MaskCreator.Services;
using ReactiveUI;
using SkiaSharp;

namespace MDMX_MaskCreator.ViewModels;

public class MainWindowViewModel: ReactiveObject
{
    private MaskRenderer _renderer;
    private readonly PresetService _presetService = new();
    private readonly SettingsService _settingsService = new();
    private AppSettings _settings;

    public string TogglePreviewModeLabel => CurrentPreviewMode == PreviewMode.Mask
        ? "Show patch layout"
        : "Show mask preview";
    
    private Bitmap? _previewBitmap;
    private string _statusText = "No patch loaded";
    private bool _canExport;
    private string _presetText = "";
    
    public enum PreviewMode { Mask, PatchLayout }
    private PreviewMode _previewMode = PreviewMode.Mask;


    public bool CanOpenExportFolder =>
        _settings.LastExportPath is not null &&
        Directory.Exists(Path.GetDirectoryName(_settings.LastExportPath));
    
    private string _dmxRangeInput = string.Empty;
    private List<DmxRange> _parsedRanges = new();

    private string _fixtureFilter = string.Empty;

    public string FixtureFilter
    {
        get => _fixtureFilter;
        set
        {
            this.RaiseAndSetIfChanged(ref _fixtureFilter, value ?? string.Empty);
            this.RaisePropertyChanged(nameof(FilteredUniverses));
        }
    }
    public Func<string, string, Task>? ShowErrorDialog { get; set; }

    private Task ShowError(string title, string message)
        => ShowErrorDialog?.Invoke(title, message) ?? Task.CompletedTask;

    public IEnumerable<UniverseGroupViewModel> FilteredUniverses
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_fixtureFilter))
            {
                foreach (var universe in Universes)
                {
                    universe.SetFilter(string.Empty);
                }

                return Universes;
            }

            var filter = _fixtureFilter.ToLower();

            foreach (var universe in Universes)
            {
                universe.SetFilter(filter);
            }

            return Universes.Where(u => u.FilteredFixtures.Any());
        }
    }

    
    public string DmxRangeInput
    {
        get => _dmxRangeInput;
        set
        {
            this.RaiseAndSetIfChanged(ref _dmxRangeInput, value);
            _parsedRanges = DmxRangeParser.Parse(value);
            RefreshPreview();
        }
    }
    
    public string DmxRangeError => _dmxRangeInput.Length > 0 && _parsedRanges.Count == 0
        ? "No valid ranges found"
        : string.Empty;
    
    public string MaskedSummary => 
        $"{AllFixtures().Count(f => f.IsSelected)} fixtures masked";
    
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
    
    public string PresetText
    {
        get => _presetText;
        private set => this.RaiseAndSetIfChanged(ref _presetText, value);
    }

    public bool CanExport
    {
        get => _canExport;
        private set => this.RaiseAndSetIfChanged(ref _canExport, value);
    }
    public PreviewMode CurrentPreviewMode
    {
        get => _previewMode;
        set
        {
            this.RaiseAndSetIfChanged(ref _previewMode, value);
            RefreshPreview();
        }
    }
    // commands — wired up in constructor
    public ReactiveCommand<Unit, Unit> LoadFixturesCommand { get; }
    public ReactiveCommand<Unit, Unit> LoadPatchCommand { get; }
    public ReactiveCommand<Unit, Unit> ExportMaskCommand { get; }
    public ReactiveCommand<Unit, Unit> SavePresetCommand { get; }
    public ReactiveCommand<Unit, Unit> LoadPresetCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenSettingsCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenExportFolderCommand { get; }
    public ReactiveCommand<Unit, Unit> TogglePreviewModeCommand { get; }
    
    public Func<AppSettings, Task<AppSettings?>>? ShowSettingsDialog { get; set; }

    
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
        var canLoadPatch = this.WhenAnyValue(x => x.Library)
            .Select(lib => lib is not null);

        var canExport = this.WhenAnyValue(x => x.CanExport);

        _settings = _settingsService.Load();
        _renderer = new MaskRenderer(_settings.GridWidth);

        var canOpenFolder = this.WhenAnyValue(x => x.CanOpenExportFolder);

        LoadFixturesCommand = ReactiveCommand.CreateFromTask(LoadFixturesAsync);
        LoadPatchCommand = ReactiveCommand.CreateFromTask(LoadPatchAsync, canLoadPatch);
        ExportMaskCommand = ReactiveCommand.CreateFromTask(ExportMaskAsync, canExport);
        SavePresetCommand = ReactiveCommand.CreateFromTask(SavePresetAsync, canExport);
        LoadPresetCommand = ReactiveCommand.CreateFromTask(LoadPresetAsync, canExport);
        OpenSettingsCommand = ReactiveCommand.CreateFromTask(OpenSettingsAsync);
        OpenExportFolderCommand = ReactiveCommand.Create(OpenExportFolder, canOpenFolder);
        TogglePreviewModeCommand = ReactiveCommand.Create(() =>
        {
            CurrentPreviewMode = CurrentPreviewMode == PreviewMode.Mask ? PreviewMode.PatchLayout : PreviewMode.Mask;
        });
        
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
    
    private async void RefreshPreview()
    {
        
        SKBitmap skBitmap;

        if (CurrentPreviewMode == PreviewMode.PatchLayout)
        {
            var allFixtures = AllFixtures().Select(f => f.Fixture).ToList();
            skBitmap = await Task.Run(() => _renderer.RenderColorCoded(allFixtures, 12));
        }
        else
        {
            var selected = AllFixtures().Where(f => f.IsSelected).Select(f => f.Fixture).ToList();
            var ranges = _parsedRanges.ToList();
            var invertMask = _settings.InvertMask;
            var fullColumnForcesWhiteCrc = _settings.FullColumnForcesWhiteCrc;

            skBitmap = await Task.Run(() => 
                _renderer.Render(selected, ranges, invertMask, fullColumnForcesWhiteCrc));
        }
        
        PreviewBitmap = ConvertToAvaloniaBitmap(skBitmap);
        skBitmap.Dispose();
        
        this.RaisePropertyChanged(nameof(MaskedSummary));
        this.RaisePropertyChanged(nameof(DmxRangeError));
        this.RaisePropertyChanged(nameof(TogglePreviewModeLabel));

        UpdateStatus();
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

        try
        {
            Library = FixtureLibrary.LoadFromCsv(path);
            StatusText = $"Fixture library loaded · {Library.Count} definitions";
        }
        catch (Exception ex)
        {
            await ShowError("Failed to load fixture library",
                $"The file could not be parsed as a fixture library.\n\n{ex.Message}");
        }
    }

    private async Task LoadPatchAsync()
    {
        if (_library is null) return;

        var path = await PickFileAsync("Load patch", "csv");
        if (path is null) return;

        try
        {
            var entries = PatchEntry.ParsePatch(path);
            _patch = PatchedFixture.Resolve(entries, Library);
            RebuildFixtureList(_patch);
        }
        catch (Exception ex)
        {
            await ShowError("Failed to load patch",
                $"The file could not be parsed as a patch.\n\n{ex.Message}");
        }
    }

    private async Task ExportMaskAsync()
    {
        var path = await SaveFileAsync("Export mask PNG", "png");
        if (path is null) return;

        var selected = AllFixtures().Where(f => f.IsSelected).Select(f => f.Fixture);
        var ranges = _parsedRanges.ToList();

        if (_settings.SixteenNineExport)
            _renderer.RenderAndSaveAs169(selected, ranges, path, _settings.GridWidth,
                _settings.InvertMask, _settings.ExportBlackAsTransparent, 
                _settings.WhitePadding, _settings.FullColumnForcesWhiteCrc);
        else
            _renderer.RenderAndSave(selected, ranges, path,
                _settings.InvertMask, _settings.ExportBlackAsTransparent);

        _settings.LastExportPath = path;
        _settingsService.Save(_settings);
        this.RaisePropertyChanged(nameof(CanOpenExportFolder));
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
        
        try
        {
            var selected = _presetService.Load(path);

            foreach (var fixture in AllFixtures())
                fixture.IsSelected = selected.Contains((fixture.Universe, fixture.Channel));
            
            PresetText = "Loaded: "+ Path.GetFileName(path);
            UpdateStatus();
        }
        catch (Exception ex)
        {
            await ShowError("Failed to load preset",
                $"The file could not be parsed as a preset.\n\n{ex.Message}");
        }
    }
    
    private async Task OpenSettingsAsync()
    {
        if (ShowSettingsDialog is null) return;

        var updated = await ShowSettingsDialog(_settings);
        if (updated is null) return;

        _settings = updated;
        _settingsService.Save(_settings);
        _renderer = new MaskRenderer(_settings.GridWidth);
        RefreshPreview();
        this.RaisePropertyChanged(nameof(CanOpenExportFolder));
    }

    private void OpenExportFolder()
    {
        var folder = Path.GetDirectoryName(_settings.LastExportPath);
        if (folder is null) return;

        // cross-platform folder open
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = folder,
            UseShellExecute = true
        };
        System.Diagnostics.Process.Start(psi);
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