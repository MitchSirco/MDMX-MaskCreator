using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using MDMX_MaskCreator.Grid;
using MDMX_MaskCreator.ViewModels;
using MDMX_MaskCreator.Views;
using ReactiveUI;

namespace MDMX_MaskCreator;

public partial class MainWindow : Window
{
    
    private int _gridWidth = 2560;

    public MainWindow()
    {
        InitializeComponent();

        var vm = new MainWindowViewModel();

        vm.RegisterFilePickers(
            pickFile: (title, ext) => PickFileAsync(title, ext),
            saveFile: (title, ext) => SaveFileAsync(title, ext)
        );

        vm.ShowSettingsDialog = async (current) =>
        {
            var dialog = new SettingsWindow(current);
            await dialog.ShowDialog(this);

            if (!dialog.Saved) return null;

            var settingsVm = (SettingsWindowViewModel)dialog.DataContext!;
            return settingsVm.ToSettings(current);
        };
        
        vm.ShowErrorDialog = async (title, message) =>
        {
            var dialog = new Window
            {
                Title = title,
                Width = 420,
                Height = 200,
                CanResize = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new StackPanel
                {
                    Margin = new Avalonia.Thickness(20),
                    Spacing = 16,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = message,
                            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                            FontSize = 13
                        },
                        new Button
                        {
                            Content = "OK",
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
                        }
                    }
                }
            };

            // wire OK button to close
            var okButton = ((StackPanel)dialog.Content).Children
                .OfType<Button>().First();
            okButton.Click += (_, _) => dialog.Close();

            await dialog.ShowDialog(this);
        };
        
        DataContext = vm;
        _gridWidth = vm.GridWidth;
        vm.WhenAnyValue(x => x.CurrentLabels)
            .Subscribe(labels => RebuildLabelGrid(labels));

    }

    private async Task<string?> PickFileAsync(string title, string extension)
    {
        var files = await StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = title,
                AllowMultiple = false,
                FileTypeFilter = [new FilePickerFileType(extension.ToUpper())
                {
                    Patterns = [$"*.{extension}"]
                }]
            });

        return files.Count > 0 ? files[0].Path.LocalPath : null;
    }

    private async Task<string?> SaveFileAsync(string title, string extension)
    {
        var file = await StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title = title,
                DefaultExtension = extension,
                FileTypeChoices = [new FilePickerFileType(extension.ToUpper())
                {
                    Patterns = [$"*.{extension}"]
                }]
            });

        return file?.Path.LocalPath;
    }
    
    private void RebuildLabelGrid(List<FixtureRunLabel> labels)
{
    LabelGrid.ColumnDefinitions.Clear();
    LabelGrid.Children.Clear();
    TextLabelGrid.ColumnDefinitions.Clear();
    TextLabelGrid.Children.Clear();

    if (labels.Count == 0) return;

    int currentColumn = 0;
    int totalColumns = _gridWidth / GridLayout.ColumnWidth;
    int colIndex = 0;

    for (int i = 0; i < labels.Count; i++)
    {
        var label = labels[i];
        int labelStartColumn = (int)(label.XFraction * totalColumns);
        int labelEndColumn = labelStartColumn + (int)(label.WidthFraction * totalColumns);

        // gap spacer
        if (labelStartColumn > currentColumn)
        {
            var gapWidth = labelStartColumn - currentColumn;
            LabelGrid.ColumnDefinitions.Add(new ColumnDefinition(gapWidth, GridUnitType.Star));
            TextLabelGrid.ColumnDefinitions.Add(new ColumnDefinition(gapWidth, GridUnitType.Star));
            colIndex++;
            currentColumn += gapWidth;
        }

        var colWidth = Math.Max(1, labelEndColumn - labelStartColumn);
        LabelGrid.ColumnDefinitions.Add(new ColumnDefinition(colWidth, GridUnitType.Star));
        TextLabelGrid.ColumnDefinitions.Add(new ColumnDefinition(colWidth, GridUnitType.Star));

        // invisible tooltip hit zone over the image
        var hitZone = new Border
        {
            Background = Brushes.Transparent,
            [ToolTip.TipProperty] = label.Name,
            [ToolTip.ShowDelayProperty] = 100
        };
        Avalonia.Controls.Grid.SetColumn(hitZone, colIndex);
        LabelGrid.Children.Add(hitZone);

        // text label below
        if (label.PixelWidth >= 5)
        {
            var text = new TextBlock
            {
                Text = label.Name,
                FontSize = 11,
                Foreground = Brushes.White,
                IsVisible = false,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
                RenderTransform = new RotateTransform(90),
                TextWrapping = Avalonia.Media.TextWrapping.NoWrap,
                ClipToBounds = false
            };
            Avalonia.Controls.Grid.SetColumn(text, colIndex);
            TextLabelGrid.Children.Add(text);
        }

        colIndex++;
        currentColumn = labelEndColumn;
    }

    // trailing gap
    if (currentColumn < totalColumns)
    {
        var trailing = totalColumns - currentColumn;
        LabelGrid.ColumnDefinitions.Add(new ColumnDefinition(trailing, GridUnitType.Star));
        TextLabelGrid.ColumnDefinitions.Add(new ColumnDefinition(trailing, GridUnitType.Star));
    }
}
}