using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HudlReader.Lib;

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
    private Task StartProcessing()
    {
        if (string.IsNullOrEmpty(this.InputFolder) || string.IsNullOrEmpty(this.OutputFolder))
        {
            // TODO: Show validation message
            return Task.CompletedTask;
        }
    
        // Copy the dashboard file to the output folder
        string dashboardFilename = "Dashboard.html";
        this.CopyResourceFile(dashboardFilename, Path.Combine(this.OutputFolder, dashboardFilename));
        
        // Start parsing on a background thread
        Task task = Task.Run(async () =>
        {
            InStatParser parser = new(this.InputFolder, this.OutputFolder, i =>
            {
                // Update the progress bar on the UI thread 
                Dispatcher.UIThread.InvokeAsync(() => { this.Progress = i; });
            });
            
            await parser.ParsePlayerReports();
        });
        
        return task;
    }

    private void CopyResourceFile(string resourceFileName, string outputPath)
    {
        Assembly assembly = typeof(InStatParser).GetTypeInfo().Assembly;

        string resourceName = assembly.GetManifestResourceNames()
            .Single(str => str.EndsWith(resourceFileName));
        
        using Stream? stream = assembly.GetManifestResourceStream(resourceName);
        using Stream s = File.Create(outputPath);
        stream?.CopyTo(s);
    }
}