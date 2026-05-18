using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using CsvHelper;

namespace MDMX_MaskCreator;

public partial class MainWindow : Window
{
    private List<DmxFixture> fixtures = new List<DmxFixture>();
    private List<PatchEntry> patchEntries = new List<PatchEntry>();
    
    public MainWindow()
    {
        InitializeComponent();
    }
    private int _clickCount = 0;
    
    private async void Button_OnClick(object sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open CSV fixture",
            AllowMultiple = false
        });
        if (files.Count >= 1)
        {   
            fixtures = DmxFixture.ParseFixtures(files[0].TryGetLocalPath());
            Status.Text += "\nFixture added: " + fixtures.Count + " entries";
        }

    }

    private async void PatchButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open CSV Patch",
            AllowMultiple = false
        });

        if (files.Count >= 1)
        {
            patchEntries = PatchEntry.ParsePatch(files[0].TryGetLocalPath());
            Status.Text += "\nPatch added: " + patchEntries.Count + " entries";
        }
        
    }
    
    public static List<PatchedFixture> Resolve(
        IEnumerable<PatchEntry> patch,
        FixtureLibrary library)
    {
        var result = new List<PatchedFixture>();

        foreach (var entry in patch)
        {
            var definition = library.Get(entry.Fixture);
            if (definition is null)
            {
                // fixture in patch but not in library — log and skip
                Console.WriteLine($"Warning: fixture '{entry.Fixture}' not found in library");
                continue;
            }
            result.Add(new PatchedFixture(entry, definition));
        }

        return result;
    }
    
    private void BtnCreatePatch_OnClick(object? sender, RoutedEventArgs e)
    {
        var result = Resolve(patchEntries, new FixtureLibrary(fixtures));
        
        Status.Text += "\nActual Patches: " + result.Count + " entries";

        Console.WriteLine("test");
        
    }
    
    
}