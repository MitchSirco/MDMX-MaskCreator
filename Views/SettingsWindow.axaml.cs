using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MDMX_MaskCreator.ViewModels;

namespace MDMX_MaskCreator.Views;

public partial class SettingsWindow : Window
{
    public bool Saved { get; private set; }

    public SettingsWindow(AppSettings settings)
    {
        InitializeComponent();
        DataContext = new SettingsWindowViewModel(settings);
    }

    private void OnSaveClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Saved = true;
        Close();
    }

    private void OnCancelClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Saved = false;
        Close();
    }

    private void OnPreset1920Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ((SettingsWindowViewModel)DataContext!).GridWidth = 1920;

    private void OnPreset2560Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ((SettingsWindowViewModel)DataContext!).GridWidth = 2560;
}
