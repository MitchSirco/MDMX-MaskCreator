# MDMX-MaskCreator
A tool to programmatically create DMX mask PNGs for MDMX Gridnode.

Features:
* Create masks using a patch and fixture csv
* Manually create masks using the manual dmx range input
* Export with gridnode only or in 16:9 aspect ratio
* export with invert or transparent pixels
* enable / disable crc for full columns


## Tech
Written in C# .NET 8 with the Avalonia UI Framework as an attempt to make it Cross-Platform

Also, for transparency, this was written using the help of Claude Sonnet 4.6 (Helped with Structure and UI)

## CSV formats

### Fixture library

```csv
Status,Special,Channel Name,Channel,Type,Notes
HEAD,,Fixture Name,Xch,Channel Type,Description
working,Group,Channel Name,1,Linear,Notes
...
```

`HEAD` rows define fixture boundaries. `Channel` in the HEAD row is the channel count in the format `Xch`. (12ch, 18ch)

### Patch

```csv
Safe-to-patch,Universe,Channel,Fixture,Location,Alignment,Fixture #,Notes
Yes - Locked,1,1,Fixture Name,Location,0,1,
...
```
  
`Alignment` is the slot offset within the starting column (0–5), used when fixtures are packed mid-column.

## Getting started

  

### Download
Download the latest [Releases](../../releases):

| Platform | File |
|---|---|
| Windows | `MDMX-MaskCreator-windows.zip` |
| macOS | `MDMX-MaskCreator-macos.zip` |
| Linux | `MDMX-MaskCreator-linux.zip` |


**macOS note:** The app is unsigned because no dev acc from apple. If you see "damaged and cannot be opened",
run `xattr -cr MDMX-MaskCreator.app` in Terminal, then right-click -> Open.

Note: Linux is untested, lmk if it works

### Building from source

Requires [.NET 8 SDK](https://dotnet.microsoft.com/download).

```bash
git clone https://github.com/MitchSirco/MDMX-MaskCreator
cd MDMX-MaskCreator
dotnet run
```

## Settings

Settings are stored at:
- Windows: `%APPDATA%\MDMX-MaskCreator\settings.json`
- macOS/Linux: `~/.config/MDMX-MaskCreator/settings.json`
