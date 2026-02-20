using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading.Tasks;
using System.IO;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.Logging;
using PacmanGame.Services.Interfaces;
using PacmanGame.Models.CustomLevel;
using ReactiveUI;
using System.Linq;
using System.Reactive.Linq;

namespace PacmanGame.ViewModels;

public sealed class CustomLevelsLibraryViewModel : ViewModelBase
{
    private readonly ICustomLevelManagerService _customLevelManager;
    private readonly IStorageProvider _storageProvider;
    private readonly ILogger<CustomLevelsLibraryViewModel> _logger;

    private CustomLevelSummary? _selectedLevel;
    public CustomLevelSummary? SelectedLevel
    {
        get => _selectedLevel;
        set => this.RaiseAndSetIfChanged(ref _selectedLevel, value);
    }

    public ObservableCollection<CustomLevelSummary> Library { get; } = new();

    public ReactiveCommand<Unit, Unit> ReloadCommand { get; }
    public ReactiveCommand<Unit, Unit> ImportCommand { get; }
    public ReactiveCommand<Unit, Unit> DeleteCommand { get; }
    public ReactiveCommand<Unit, Unit> PlayCommand { get; }
    public ReactiveCommand<Unit, Unit> EditCommand { get; }
    public ReactiveCommand<Unit, Unit> BackToMenuCommand { get; }

    private readonly MainWindowViewModel _mainWindowViewModel;

    public CustomLevelsLibraryViewModel(
        ICustomLevelManagerService customLevelManager,
        IStorageProvider storageProvider,
        MainWindowViewModel mainWindowViewModel,
        ILogger<CustomLevelsLibraryViewModel> logger)
    {
        _customLevelManager = customLevelManager;
        _storageProvider = storageProvider;
        _mainWindowViewModel = mainWindowViewModel;
        _logger = logger;

        ReloadCommand = ReactiveCommand.CreateFromTask(ReloadLibraryAsync);
        ImportCommand = ReactiveCommand.CreateFromTask(ImportAsync);
        DeleteCommand = ReactiveCommand.CreateFromTask(DeleteAsync, this.WhenAnyValue(vm => vm.SelectedLevel).Select(level => level != null));
        PlayCommand = ReactiveCommand.Create(() => { /* TODO: tie into gameplay */});
        EditCommand = ReactiveCommand.Create(() => { /* TODO: open creative mode */});
        BackToMenuCommand = ReactiveCommand.Create(() => _mainWindowViewModel.NavigateTo<MainMenuViewModel>());

        _ = ReloadLibraryAsync();
    }

    private async Task ReloadLibraryAsync()
    {
        Library.Clear();
        var entries = await _customLevelManager.GetCustomLevelsAsync();
        foreach (var entry in entries)
        {
            Library.Add(entry);
        }
    }

    private async Task ImportAsync()
    {
        var result = await _storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Import Pac-Man Project",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Pac-Man Project")
                {
                    Patterns = new[] {"*.pacproj"}
                }
            }
        });

        var file = result.FirstOrDefault();
        if (file == null)
        {
            return;
        }

        await using var importStream = await file.OpenReadAsync();
        var tempFile = Path.GetTempFileName();
        await using (var targetStream = File.Create(tempFile))
        {
            await importStream.CopyToAsync(targetStream);
        }

        var summary = await _customLevelManager.ImportProjectAsync(tempFile);
        Library.Add(summary);
        _logger.LogInformation("Imported custom project {Name}", summary.ProjectName);
    }

    private async Task DeleteAsync()
    {
        if (SelectedLevel == null) return;
        await _customLevelManager.DeleteCustomLevelAsync(SelectedLevel.Id);
        Library.Remove(SelectedLevel);
    }
}
