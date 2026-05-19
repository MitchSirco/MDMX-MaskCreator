namespace MDMX_MaskCreator;

public class AppSettings
{
    public int GridWidth { get; set; } = 2560;
    public bool SixteenNineExport { get; set; } = true;
    public bool InvertMask { get; set; } = true;
    public string? LastExportPath { get; set; }
    public bool ExportBlackAsTransparent { get; set; } = false;
}