using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using MDMX_MaskCreator.ViewModels;
using MDMX_MaskCreator.Views;

namespace MDMX_MaskCreator;

public partial class MainWindow : Window
{
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
}