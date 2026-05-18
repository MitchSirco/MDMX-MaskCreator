using System;
using System.Data;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace MDMX_MaskCreator;

public partial class MainWindow : Window
{
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
            Title = "Open JSON fixture",
            AllowMultiple = false
        });
        if (files.Count >= 1)
        {            
            await using var stream = await files[0].OpenReadAsync();
            using var streamReader = new StreamReader(stream);
            // Reads all the content of file as a text.
            var fileContent = await streamReader.ReadToEndAsync();
            Console.WriteLine(fileContent.ToString());
        }
        
        var btn = (Button)sender;
        
        btn.Content = $"Clicked {++_clickCount} times";
    }
}