using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using HudlReader.UI.ViewModels;

namespace HudlReader.UI.Views;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;
    
    
    public MainWindow()
    {
        this.InitializeComponent();
        
        this._viewModel = new();
        this._viewModel.FuncPickFolderAsync = this.PickFolderAsync;
        this.DataContext = this._viewModel;
    }
    
    private async Task<string?> PickFolderAsync()
    {
        IReadOnlyList<IStorageFolder> folders = 
            await this.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Folder",
            AllowMultiple = false
        });

        return folders.Count > 0 ? folders[0].Path.LocalPath : null;
    }
}