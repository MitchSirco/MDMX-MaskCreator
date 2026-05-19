using System;
using System.Reactive.Linq;
using MDMX_MaskCreator.Grid;
using ReactiveUI;

namespace MDMX_MaskCreator.ViewModels;

public class SettingsWindowViewModel : ReactiveObject
{
    private int _gridWidth;
    private bool _sixteenNineExport;
    private bool _invertMask;
    private bool _exportBlackAsTransparent;

    public bool ExportBlackAsTransparent
    {
        get => _exportBlackAsTransparent;
        set => this.RaiseAndSetIfChanged(ref _exportBlackAsTransparent, value);
    }
    public int GridWidth
    {
        get => _gridWidth;
        set => this.RaiseAndSetIfChanged(ref _gridWidth, value);
    }

    public bool SixteenNineExport
    {
        get => _sixteenNineExport;
        set => this.RaiseAndSetIfChanged(ref _sixteenNineExport, value);
    }
    
    public bool InvertMask
    {
        get => _invertMask;
        set => this.RaiseAndSetIfChanged(ref _invertMask, value);
    }
    
    public SettingsWindowViewModel(AppSettings settings)
    {
        _gridWidth = settings.GridWidth;
        _sixteenNineExport = settings.SixteenNineExport;
        _invertMask = settings.InvertMask;
        _exportBlackAsTransparent = settings.ExportBlackAsTransparent;

        this.WhenAnyValue(x => x.GridWidth)
            .Do(_ => this.RaisePropertyChanged(nameof(ExportSizeLabel)))
            .Subscribe();

        this.WhenAnyValue(x => x.SixteenNineExport)
            .Do(_ => this.RaisePropertyChanged(nameof(ExportSizeLabel)))
            .Subscribe();

    }

    
    
    public AppSettings ToSettings(AppSettings existing) => new AppSettings
    {
        GridWidth = GridWidth,
        SixteenNineExport = SixteenNineExport,
        InvertMask = InvertMask,
        ExportBlackAsTransparent = ExportBlackAsTransparent,
        LastExportPath = existing.LastExportPath
    };
    
    public string ExportSizeLabel => SixteenNineExport
        ? $"Output: {GridWidth} × {GridWidth * 9 / 16} px"
        : $"Output: {GridWidth} × {MaskRenderer.GridHeight} px";

    
}