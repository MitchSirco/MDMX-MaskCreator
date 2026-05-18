using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using MDMX_MaskCreator.ViewModels;

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