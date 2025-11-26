using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace HudlReader.UI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private string? _inputFolder;
    
    [ObservableProperty]
    private string? _outputFolder;

    [ObservableProperty] private int _progress;
    
    // This will be called from the Window code-behind
    public Func<Task<string?>> FuncPickFolderAsync { get; set; }
    
    [RelayCommand]
    private async Task BrowseInputFolder()
    {
        string? folder = await this.FuncPickFolderAsync();
        if (folder != null) this.InputFolder = folder;
    }
    
    [RelayCommand]
    private async Task BrowseOutputFolder()
    {
        string? folder = await this.FuncPickFolderAsync();
        if (folder != null) this.OutputFolder = folder;
    }
    
    [RelayCommand]
    private async Task StartProcessing()
    {
        if (string.IsNullOrEmpty(this.InputFolder) || string.IsNullOrEmpty(this.OutputFolder))
        {
            // TODO: Show validation message
            
            return;
        }
    
        // TODO: Implement your processing logic here
        
        // Simulating progress for now
        for (int i = 0; i <= 100; i += 10)
        {
            this.Progress = i;
            await Task.Delay(200);
        }
    }
}