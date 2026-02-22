using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.Logging;
using PacmanGame.Helpers;
using PacmanGame.Models.Creative;
using PacmanGame.Models.CustomLevel;
using PacmanGame.Services.Interfaces;
using PacmanGame.ViewModels;
using ReactiveUI;

namespace PacmanGame.ViewModels.Creative;

public class CreativeModeViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainWindowViewModel;
    private readonly ILogger<CreativeModeViewModel> _logger;
    private readonly ICustomLevelManagerService _customLevelManager;
    private readonly IProfileManager _profileManager;
    private readonly IStorageProvider _storageProvider;

    public LevelCanvasViewModel CanvasViewModel { get; }
    public ToolboxViewModel ToolboxViewModel { get; }

    public ReactiveCommand<Unit, Unit> CloseCommand { get; }
    public ReactiveCommand<Unit, Unit> ExportCommand { get; }
    public ReactiveCommand<Unit, Unit> ConfirmExportCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelExportCommand { get; }
    public ReactiveCommand<Unit, Unit> ImportCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelImportPreviewCommand { get; }
    public ReactiveCommand<Unit, Unit> ImportPlayCommand { get; }
    public ReactiveCommand<Unit, Unit> ImportEditCommand { get; }
    public ReactiveCommand<Unit, Unit> PlayTestCommand { get; }
    public ReactiveCommand<Unit, Unit> PreviousLevelCommand { get; }
    public ReactiveCommand<Unit, Unit> NextLevelCommand { get; }

    public ProjectConfig ProjectConfig { get; } = new()
    {
        ProjectName = "Custom Level",
        Author = "Unknown Creator",
        CreatedDate = DateTime.UtcNow,
        GlobalConfig = new GlobalConfig
        {
            Lives = 3,
            WinScore = 5000,
            LevelCount = 1
        },
        LevelConfigs = new List<LevelConfig>
        {
            new()
            {
                LevelNumber = 1,
                Filename = "level1.txt",
                PacmanSpeedMultiplier = 1.0,
                GhostSpeedMultiplier = 0.9,
                FrightenedDuration = 10,
                FruitPoints = 5,
                GhostEatPoints = 30
            }
        }
    };

    public int Lives
    {
        get => ProjectConfig.GlobalConfig.Lives;
        set
        {
            var clamped = Math.Clamp(value, 1, 5);
            if (ProjectConfig.GlobalConfig.Lives == clamped) return;
            ProjectConfig.GlobalConfig.Lives = clamped;
            this.RaisePropertyChanged();
        }
    }

    public int LevelCount
    {
        get => ProjectConfig.GlobalConfig.LevelCount;
        set
        {
            var clamped = Math.Clamp(value, 1, 10);
            if (ProjectConfig.GlobalConfig.LevelCount == clamped) return;
            ProjectConfig.GlobalConfig.LevelCount = clamped;
            EnsureLevelConfigs();
            // Keep win score within the allowed range for the selected level count.
            var clampedWin = Math.Clamp(ProjectConfig.GlobalConfig.WinScore, WinScoreMin, WinScoreMax);
            if (ProjectConfig.GlobalConfig.WinScore != clampedWin)
            {
                ProjectConfig.GlobalConfig.WinScore = clampedWin;
                this.RaisePropertyChanged(nameof(WinScore));
            }
            this.RaisePropertyChanged();
            this.RaisePropertyChanged(nameof(WinScoreMin));
            this.RaisePropertyChanged(nameof(WinScoreMax));
            this.RaisePropertyChanged(nameof(WinScoreRange));
            this.RaisePropertyChanged(nameof(LevelConfigs));
            this.RaisePropertyChanged(nameof(CanGoPreviousLevel));
            this.RaisePropertyChanged(nameof(CanGoNextLevel));
            this.RaisePropertyChanged(nameof(CurrentLevelInfo));
        }
    }

    public int WinScore
    {
        get => ProjectConfig.GlobalConfig.WinScore;
        set
        {
            var clamped = Math.Clamp(value, WinScoreMin, WinScoreMax);
            if (ProjectConfig.GlobalConfig.WinScore == clamped) return;
            ProjectConfig.GlobalConfig.WinScore = clamped;
            this.RaisePropertyChanged();
        }
    }

    public string WinScoreRange => $"Range: {WinScoreMin:N0} - {WinScoreMax:N0}";

    public IReadOnlyList<LevelConfig> LevelConfigs => ProjectConfig.LevelConfigs;

    public LevelConfig? SelectedLevelConfig
        => CurrentLevelIndex >= 0 && CurrentLevelIndex < ProjectConfig.LevelConfigs.Count
            ? ProjectConfig.LevelConfigs[CurrentLevelIndex]
            : null;

    private readonly Dictionary<int, string[]> _levelLinesByIndex = new();
    private int _currentLevelIndex;

    public int CurrentLevelIndex
    {
        get => _currentLevelIndex;
        private set
        {
            if (_currentLevelIndex == value) return;
            _currentLevelIndex = value;
            this.RaisePropertyChanged();
            this.RaisePropertyChanged(nameof(SelectedLevelNumber));
            this.RaisePropertyChanged(nameof(CanGoPreviousLevel));
            this.RaisePropertyChanged(nameof(CanGoNextLevel));
            this.RaisePropertyChanged(nameof(CurrentLevelInfo));
            this.RaisePropertyChanged(nameof(SelectedLevelConfig));
        }
    }

    public bool CanGoPreviousLevel => CurrentLevelIndex > 0;
    public bool CanGoNextLevel => CurrentLevelIndex < (LevelCount - 1);
    public string CurrentLevelInfo => $"Editing Level {CurrentLevelIndex + 1} of {LevelCount}";

    public int SelectedLevelNumber
    {
        get => CurrentLevelIndex + 1;
        set => NavigateToLevel(value - 1);
    }

    public int WinScoreMin => LevelCount switch
    {
        1 => 100,
        2 => 1000,
        _ => 5000
    };

    public int WinScoreMax => LevelCount switch
    {
        1 => 1000,
        2 => 5000,
        3 => 12000,
        _ => 12000 + ((LevelCount - 3) * 5000)
    };

    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };
    private readonly JsonSerializerOptions _jsonReadOptions = new() { PropertyNameCaseInsensitive = true };

    private bool _isExportOptionsOpen;
    public bool IsExportOptionsOpen
    {
        get => _isExportOptionsOpen;
        set => this.RaiseAndSetIfChanged(ref _isExportOptionsOpen, value);
    }

    private string _exportProjectName = "Custom Level";
    public string ExportProjectName
    {
        get => _exportProjectName;
        set => this.RaiseAndSetIfChanged(ref _exportProjectName, value);
    }

    private bool _exportIsEditable = true;
    public bool ExportIsEditable
    {
        get => _exportIsEditable;
        set => this.RaiseAndSetIfChanged(ref _exportIsEditable, value);
    }

    private bool _isImportPreviewOpen;
    public bool IsImportPreviewOpen
    {
        get => _isImportPreviewOpen;
        set => this.RaiseAndSetIfChanged(ref _isImportPreviewOpen, value);
    }

    private CustomLevelSummary? _importSummary;
    public CustomLevelSummary? ImportSummary
    {
        get => _importSummary;
        set
        {
            this.RaiseAndSetIfChanged(ref _importSummary, value);
            this.RaisePropertyChanged(nameof(ImportCanEdit));
        }
    }

    public bool ImportCanEdit => ImportSummary?.IsEditable ?? false;

    private string? _pendingImportArchivePath;

    public CreativeModeViewModel(
        MainWindowViewModel mainWindowViewModel,
        LevelCanvasViewModel canvasViewModel,
        ToolboxViewModel toolboxViewModel,
        ILogger<CreativeModeViewModel> logger,
        IAudioManager audioManager,
        ICustomLevelManagerService customLevelManager,
        IProfileManager profileManager,
        IStorageProvider storageProvider)
    {
        _mainWindowViewModel = mainWindowViewModel;
        CanvasViewModel = canvasViewModel;
        ToolboxViewModel = toolboxViewModel;
        _logger = logger;
        _customLevelManager = customLevelManager;
        _profileManager = profileManager;
        _storageProvider = storageProvider;

        CloseCommand = ReactiveCommand.Create(CloseEditor);
        ExportCommand = ReactiveCommand.Create(OpenExportOptions);
        ConfirmExportCommand = ReactiveCommand.CreateFromTask(ExportProjectAsync);
        CancelExportCommand = ReactiveCommand.Create(CloseExportOptions);
        ImportCommand = ReactiveCommand.CreateFromTask(OpenImportPreviewAsync);
        CancelImportPreviewCommand = ReactiveCommand.Create(CloseImportPreview);
        ImportPlayCommand = ReactiveCommand.CreateFromTask(ImportAndPlayAsync);
        ImportEditCommand = ReactiveCommand.CreateFromTask(ImportAndEditAsync);
        PlayTestCommand = ReactiveCommand.CreateFromTask(PlayTestAsync);
        PreviousLevelCommand = ReactiveCommand.Create(() => NavigateToLevel(CurrentLevelIndex - 1), this.WhenAnyValue(vm => vm.CanGoPreviousLevel));
        NextLevelCommand = ReactiveCommand.Create(() => NavigateToLevel(CurrentLevelIndex + 1), this.WhenAnyValue(vm => vm.CanGoNextLevel));
        ZoomInCommand = ReactiveCommand.Create(() => { ZoomLevel = Math.Min(2.0, ZoomLevel + 0.1); });
        ZoomOutCommand = ReactiveCommand.Create(() => { ZoomLevel = Math.Max(0.5, ZoomLevel - 0.1); });

        audioManager.PlayMusic(Constants.MenuMusic);

        ExportProjectName = ProjectConfig.ProjectName;
        ExportIsEditable = true;
        IsImportPreviewOpen = false;
        ImportSummary = null;
        EnsureLevelConfigs();

        CurrentLevelIndex = 0;
        _levelLinesByIndex[0] = CanvasViewModel.BuildLevelLines();

        ToolboxViewModel.WhenAnyValue(vm => vm.SelectedToolEntry)
            .Subscribe(_ =>
            {
                this.RaisePropertyChanged(nameof(SelectedToolEntry));
                this.RaisePropertyChanged(nameof(HasSelectedTool));
                this.RaisePropertyChanged(nameof(SelectedToolRotatable));
                this.RaisePropertyChanged(nameof(CursorShortcut));
            });

        CanvasViewModel.WhenAnyValue(vm => vm.CursorX, vm => vm.CursorY, vm => vm.CurrentCellRotation)
            .Subscribe(_ =>
            {
                this.RaisePropertyChanged(nameof(CursorLeft));
                this.RaisePropertyChanged(nameof(CursorTop));
                this.RaisePropertyChanged(nameof(CursorRotation));
                this.RaisePropertyChanged(nameof(CursorRotationLabelLeft));
                this.RaisePropertyChanged(nameof(CursorRotationLabelTop));
            });
    }

    private double _zoomLevel = 1.0;

    public double ZoomLevel
    {
        get => _zoomLevel;
        set
        {
            if (Math.Abs(_zoomLevel - value) < 0.001) return;
            _zoomLevel = value;
            this.RaisePropertyChanged();
            this.RaisePropertyChanged(nameof(ZoomLabel));
        }
    }

    public string ZoomLabel => ZoomLevel.ToString("P0");

    public ReactiveCommand<Unit, Unit> ZoomInCommand { get; }
    public ReactiveCommand<Unit, Unit> ZoomOutCommand { get; }

    private void OpenExportOptions()
    {
        ExportProjectName = ProjectConfig.ProjectName;
        ExportIsEditable = true;
        IsExportOptionsOpen = true;
    }

    private void CloseExportOptions()
    {
        IsExportOptionsOpen = false;
    }

    private void CloseImportPreview()
    {
        IsImportPreviewOpen = false;
        ImportSummary = null;
        if (!string.IsNullOrEmpty(_pendingImportArchivePath))
        {
            try
            {
                File.Delete(_pendingImportArchivePath);
            }
            catch
            {
                // ignore cleanup failures
            }
        }
        _pendingImportArchivePath = null;
    }

    private void CloseEditor()
    {
        _logger.LogInformation("Closing Creative Mode and returning to main menu.");
        _mainWindowViewModel.NavigateTo<MainMenuViewModel>();
    }

    public ToolboxViewModel.ToolEntry? SelectedToolEntry => ToolboxViewModel.SelectedToolEntry;

    public bool HasSelectedTool => SelectedToolEntry != null;

    public bool SelectedToolRotatable => SelectedToolEntry?.IsRotatable ?? false;

    public string CursorShortcut => SelectedToolEntry?.Shortcut ?? string.Empty;

    public int CursorLeft => CanvasViewModel.CursorX * LevelCanvasViewModel.TotalCellSize;

    public int CursorTop => CanvasViewModel.CursorY * LevelCanvasViewModel.TotalCellSize;

    public int CursorRotation => CanvasViewModel.CurrentCellRotation;

    public IEnumerable<double> VerticalGridLines => Enumerable.Range(1, LevelCanvasViewModel.GridWidth - 1)
        .Select(i => i * (double)LevelCanvasViewModel.TotalCellSize);

    public IEnumerable<double> HorizontalGridLines => Enumerable.Range(1, LevelCanvasViewModel.GridHeight - 1)
        .Select(i => i * (double)LevelCanvasViewModel.TotalCellSize);

    public IEnumerable<double> MajorVerticalLines => Enumerable.Range(1, LevelCanvasViewModel.GridWidth - 1)
        .Where(i => i % 5 == 0)
        .Select(i => i * (double)LevelCanvasViewModel.TotalCellSize);

    public IEnumerable<double> MajorHorizontalLines => Enumerable.Range(1, LevelCanvasViewModel.GridHeight - 1)
        .Where(i => i % 5 == 0)
        .Select(i => i * (double)LevelCanvasViewModel.TotalCellSize);

    public double GridPixelWidth => LevelCanvasViewModel.GridWidth * LevelCanvasViewModel.TotalCellSize;

    public double GridPixelHeight => LevelCanvasViewModel.GridHeight * LevelCanvasViewModel.TotalCellSize;

    public double CursorRotationLabelLeft => CursorLeft + LevelCanvasViewModel.TotalCellSize - 14;

    public double CursorRotationLabelTop => CursorTop + LevelCanvasViewModel.TotalCellSize - 16;

    public double CellDisplaySize => LevelCanvasViewModel.TotalCellSize;

    private async Task ExportProjectAsync()
    {
        if (!IsExportOptionsOpen)
        {
            OpenExportOptions();
            return;
        }

        var storageProvider = _storageProvider;
        if (storageProvider == null)
        {
            _logger.LogWarning("Storage provider unavailable. Export cancelled.");
            return;
        }

        var project = BuildProjectConfig(ExportProjectName, ExportIsEditable);
        var fileName = $"{project.ProjectName}.pacproj";
        var saveOptions = new FilePickerSaveOptions
        {
            Title = "Export Pac-Man Project",
            SuggestedFileName = fileName,
            DefaultExtension = ".pacproj",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("Pac-Man Project")
                {
                    Patterns = new[] { "*.pacproj" }
                }
            }
        };

        var file = await storageProvider.SaveFilePickerAsync(saveOptions);
        if (file == null)
        {
            _logger.LogInformation("Export cancelled by user.");
            return;
        }

        SaveCurrentLevelToMemory();

        await using var memory = new MemoryStream();
        using (var archive = new ZipArchive(memory, ZipArchiveMode.Create, true))
        {
            var perLevelLayout = new Dictionary<int, (bool HasGhostHouse, List<GridPoint> GhostSpawns)>();

            // Export all levels (1..LevelCount).
            var originalLevelIndex = CurrentLevelIndex;
            try
            {
                for (var index = 0; index < LevelCount; index++)
                {
                    LoadLevelIntoCanvas(index);
                    EnsurePacmanSpawn();
                    GenerateDotsForPlayability();

                    var errors = ValidateLayout().ToList();
                    if (errors.Any())
                    {
                        CanvasViewModel.StatusMessage = $"Export aborted: {string.Join("; ", errors)}";
                        _logger.LogWarning("Export aborted due to validation errors: {Errors}", string.Join("; ", errors));
                        return;
                    }

                    var levelText = BuildLevelText();

                    perLevelLayout[index + 1] = (
                        HasGhostHouse: CanvasViewModel.Cells.Any(c => c.IsPartOfGhostHouse),
                        GhostSpawns: CanvasViewModel.Cells
                            .Where(c => c.TileType == CreativeTileType.GhostSpawn)
                            .Select(c => new GridPoint { X = c.X, Y = c.Y })
                            .ToList()
                    );

                    var levelEntry = archive.CreateEntry($"level{index + 1}.txt");
                    await using (var levelStream = levelEntry.Open())
                    await using (var writer = new StreamWriter(levelStream, Encoding.UTF8))
                    {
                        await writer.WriteAsync(levelText);
                    }
                }
            }
            finally
            {
                LoadLevelIntoCanvas(originalLevelIndex);
                CurrentLevelIndex = originalLevelIndex;
            }

            var configEntry = archive.CreateEntry("project.json");
            await using (var configStream = configEntry.Open())
            await using (var writer = new StreamWriter(configStream, Encoding.UTF8))
            {
                await writer.WriteAsync(JsonSerializer.Serialize(project, _jsonOptions));
            }

            var metadataEntry = archive.CreateEntry("metadata.json");
            await using (var metadataStream = metadataEntry.Open())
            await using (var writer = new StreamWriter(metadataStream, Encoding.UTF8))
            {
                var metadata = BuildProjectMetadata(project, perLevelLayout);
                await writer.WriteAsync(JsonSerializer.Serialize(metadata, _jsonOptions));
            }
        }

        memory.Position = 0;
        await using var targetStream = await file.OpenWriteAsync();
        await memory.CopyToAsync(targetStream);
        await targetStream.FlushAsync();
        _logger.LogInformation("Exported creative project to {Path}", file.Name);

        IsExportOptionsOpen = false;
    }

    private async Task OpenImportPreviewAsync()
    {
        var storageProvider = _storageProvider;
        if (storageProvider == null)
        {
            _logger.LogWarning("Storage provider unavailable. Import cancelled.");
            return;
        }

        var openOptions = new FilePickerOpenOptions
        {
            Title = "Import Pac-Man Project",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Pac-Man Project")
                {
                    Patterns = new[] { "*.pacproj" }
                }
            }
        };

        var file = (await storageProvider.OpenFilePickerAsync(openOptions)).FirstOrDefault();
        if (file == null)
        {
            _logger.LogInformation("Import cancelled by user.");
            return;
        }

        var tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        await using (var sourceStream = await file.OpenReadAsync())
        await using (var destination = File.Create(tempFile))
        {
            await sourceStream.CopyToAsync(destination);
        }

        try
        {
            var config = await ReadProjectConfigFromArchiveAsync(tempFile);
            ImportSummary = new CustomLevelSummary
            {
                Id = string.Empty,
                ProjectName = config?.ProjectName ?? "Unknown Project",
                Author = string.IsNullOrWhiteSpace(config?.Author) ? "Unknown Creator" : config!.Author,
                CreatedDate = config?.CreatedDate ?? DateTime.UtcNow,
                LevelCount = config?.GlobalConfig.LevelCount ?? 1,
                IsEditable = config?.IsEditable ?? true,
                DirectoryPath = string.Empty
            };

            _pendingImportArchivePath = tempFile;
            IsImportPreviewOpen = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read import preview");
            try { File.Delete(tempFile); } catch { }
        }
    }

    private async Task<ProjectConfig?> ReadProjectConfigFromArchiveAsync(string archivePath)
    {
        using var archive = ZipFile.OpenRead(archivePath);
        var configEntry = archive.GetEntry("project.json") ?? archive.Entries.FirstOrDefault(e => string.Equals(e.Name, "project.json", StringComparison.OrdinalIgnoreCase));
        if (configEntry == null)
        {
            return null;
        }

        await using var stream = configEntry.Open();
        return await JsonSerializer.DeserializeAsync<ProjectConfig>(stream, _jsonReadOptions);
    }

    private async Task ImportAndPlayAsync()
    {
        if (string.IsNullOrEmpty(_pendingImportArchivePath))
        {
            return;
        }

        try
        {
            var config = await ReadProjectConfigFromArchiveAsync(_pendingImportArchivePath);

            // Import into library (permanent copy) first.
            var summary = await _customLevelManager.ImportProjectAsync(_pendingImportArchivePath);
            _logger.LogInformation("Imported custom project {Name}", summary.ProjectName);

            // Extract all levels for immediate play.
            using var archive = ZipFile.OpenRead(_pendingImportArchivePath);
            var tempDir = Path.Combine(Path.GetTempPath(), "pacman-recreation-import-play");
            Directory.CreateDirectory(tempDir);
            var extractedPaths = new List<string>();
            foreach (var levelCfg in (config?.LevelConfigs ?? new List<LevelConfig>()))
            {
                var entry = archive.GetEntry(levelCfg.Filename) ??
                            archive.GetEntry($"level{levelCfg.LevelNumber}.txt") ??
                            archive.Entries.FirstOrDefault(e => e.Name.Equals(levelCfg.Filename, StringComparison.OrdinalIgnoreCase));
                if (entry == null) continue;

                var mapPath = Path.Combine(tempDir, $"import_{DateTime.UtcNow:yyyyMMdd_HHmmss}_level{levelCfg.LevelNumber}.txt");
                await using (var levelStream = entry.Open())
                await using (var fileStream = File.Create(mapPath))
                {
                    await levelStream.CopyToAsync(fileStream);
                }
                extractedPaths.Add(mapPath);
            }

            if (extractedPaths.Count == 0)
            {
                // Fallback: play the first .txt found.
                var first = archive.Entries.FirstOrDefault(entry => entry.Name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase));
                if (first == null)
                {
                    _logger.LogWarning("No level file found in imported project.");
                    return;
                }
                var mapPath = Path.Combine(tempDir, $"import_{DateTime.UtcNow:yyyyMMdd_HHmmss}.txt");
                await using (var levelStream = first.Open())
                await using (var fileStream = File.Create(mapPath))
                {
                    await levelStream.CopyToAsync(fileStream);
                }
                extractedPaths.Add(mapPath);
            }

            var gameVm = _mainWindowViewModel.CreateViewModel<GameViewModel>();
            gameVm.Level = 1;
            gameVm.Score = 0;
            gameVm.Lives = config?.GlobalConfig.Lives ?? Lives;
            gameVm.CustomProjectWinScore = config?.GlobalConfig.WinScore ?? WinScore;
            if (extractedPaths.Count > 1)
            {
                gameVm.CustomProjectMapPaths = extractedPaths;
                gameVm.CustomProjectLevelSettings = config?.LevelConfigs;
            }
            else
            {
                gameVm.CustomMapPath = extractedPaths[0];
                gameVm.CustomLevelSettings = config?.LevelConfigs.FirstOrDefault();
            }
            _mainWindowViewModel.NavigateTo(gameVm);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import and play project");
        }
        finally
        {
            CloseImportPreview();
        }
    }

    private async Task ImportAndEditAsync()
    {
        if (string.IsNullOrEmpty(_pendingImportArchivePath))
        {
            return;
        }

        if (!(ImportSummary?.IsEditable ?? true))
        {
            _logger.LogWarning("Import edit requested but project is locked.");
            return;
        }

        try
        {
            var summary = await _customLevelManager.ImportProjectAsync(_pendingImportArchivePath);
            _logger.LogInformation("Imported custom project {Name}", summary.ProjectName);
            await PopulateCanvasFromArchiveAsync(_pendingImportArchivePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import and edit project");
        }
        finally
        {
            CloseImportPreview();
        }
    }

    private Task PlayTestAsync()
    {
        SaveCurrentLevelToMemory();

        var tempDir = Path.Combine(Path.GetTempPath(), "pacman-recreation-playtest");
        Directory.CreateDirectory(tempDir);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");

        var originalLevelIndex = CurrentLevelIndex;
        var mapPaths = new List<string>();
        try
        {
            for (var index = 0; index < LevelCount; index++)
            {
                LoadLevelIntoCanvas(index);
                EnsurePacmanSpawn();
                GenerateDotsForPlayability();
                var errors = ValidateLayout().ToList();
                if (errors.Any())
                {
                    _logger.LogWarning("Play test aborted due to validation errors: {Errors}", string.Join("; ", errors));
                    CanvasViewModel.StatusMessage = $"Play test aborted: {string.Join("; ", errors)}";
                    return Task.CompletedTask;
                }

                var mapPath = Path.Combine(tempDir, $"playtest_{timestamp}_level{index + 1}.txt");
                var levelText = BuildLevelText();
                File.WriteAllText(mapPath, levelText, Encoding.UTF8);
                _logger.LogInformation(
                    "Playtest initialization: wrote level {Level} map to {Path} (walls {WallCount}, doors {DoorCount}).",
                    index + 1,
                    mapPath,
                    levelText.Count(c => c == Constants.WallChar),
                    levelText.Count(c => c == Constants.GhostDoorChar));
                mapPaths.Add(mapPath);
            }
        }
        finally
        {
            LoadLevelIntoCanvas(originalLevelIndex);
            CurrentLevelIndex = originalLevelIndex;
        }

        _logger.LogInformation("Launching play test from {Path}", mapPaths[0]);

        var gameVm = _mainWindowViewModel.CreateViewModel<GameViewModel>();
        gameVm.Level = 1;
        gameVm.Score = 0;
        gameVm.Lives = Lives;
        gameVm.CustomProjectWinScore = WinScore;
        if (mapPaths.Count > 1)
        {
            gameVm.CustomProjectMapPaths = mapPaths;
            gameVm.CustomProjectLevelSettings = ProjectConfig.LevelConfigs;
        }
        else
        {
            gameVm.CustomMapPath = mapPaths[0];
            gameVm.CustomLevelSettings = ProjectConfig.LevelConfigs.FirstOrDefault();
        }
        _mainWindowViewModel.NavigateTo(gameVm);

        return Task.CompletedTask;
    }

    private void EnsureLevelConfigs()
    {
        var desired = Math.Clamp(LevelCount, 1, 10);
        ProjectConfig.GlobalConfig.LevelCount = desired;

        while (ProjectConfig.LevelConfigs.Count < desired)
        {
            var levelNumber = ProjectConfig.LevelConfigs.Count + 1;
            ProjectConfig.LevelConfigs.Add(new LevelConfig
            {
                LevelNumber = levelNumber,
                Filename = $"level{levelNumber}.txt",
                PacmanSpeedMultiplier = 1.0,
                GhostSpeedMultiplier = 0.9,
                FrightenedDuration = 10,
                FruitPoints = 5 + ((levelNumber - 1) * 5),
                GhostEatPoints = 30 + ((levelNumber - 1) * 15)
            });

            // New levels must start from a valid template (border walls + ghost house) to avoid validation failures.
            var levelIndex = levelNumber - 1;
            if (!_levelLinesByIndex.ContainsKey(levelIndex))
            {
                _levelLinesByIndex[levelIndex] = CreateTemplateLevelLines();
            }
        }

        while (ProjectConfig.LevelConfigs.Count > desired)
        {
            ProjectConfig.LevelConfigs.RemoveAt(ProjectConfig.LevelConfigs.Count - 1);
        }

        // Remove any stored layouts beyond the current level count.
        foreach (var key in _levelLinesByIndex.Keys.Where(k => k >= desired).ToList())
        {
            _levelLinesByIndex.Remove(key);
        }

        for (var i = 0; i < ProjectConfig.LevelConfigs.Count; i++)
        {
            var cfg = ProjectConfig.LevelConfigs[i];
            cfg.LevelNumber = i + 1;
            cfg.Filename = $"level{cfg.LevelNumber}.txt";

            if (cfg.PacmanSpeedMultiplier <= 0) cfg.PacmanSpeedMultiplier = 1.0;
            if (cfg.GhostSpeedMultiplier <= 0) cfg.GhostSpeedMultiplier = 0.9;
            if (cfg.FrightenedDuration <= 0) cfg.FrightenedDuration = 10;
            if (cfg.FruitPoints <= 0) cfg.FruitPoints = 5 + (i * 5);
            if (cfg.GhostEatPoints <= 0) cfg.GhostEatPoints = 30 + (i * 15);

            // Clamp to the dynamic per-level limits (Max* properties) and safe speed ranges.
            cfg.PacmanSpeedMultiplier = Math.Clamp(cfg.PacmanSpeedMultiplier, LevelConfig.MinSpeedMultiplier, LevelConfig.MaxSpeedMultiplier);
            cfg.GhostSpeedMultiplier = Math.Clamp(cfg.GhostSpeedMultiplier, LevelConfig.MinSpeedMultiplier, LevelConfig.MaxSpeedMultiplier);
            cfg.FrightenedDuration = Math.Clamp(cfg.FrightenedDuration, 1, cfg.MaxFrightenedDuration);
            cfg.FruitPoints = Math.Clamp(cfg.FruitPoints, 1, cfg.MaxFruitPoints);
            cfg.GhostEatPoints = Math.Clamp(cfg.GhostEatPoints, 10, cfg.MaxGhostEatPoints);
        }

        if (CurrentLevelIndex > desired - 1)
        {
            CurrentLevelIndex = 0;
            LoadLevelIntoCanvas(0);
        }

        // Selected level config instance can change when adding/removing levels.
        this.RaisePropertyChanged(nameof(SelectedLevelConfig));
    }

    private void GenerateDotsForPlayability()
    {
        // Auto-generate dots in reachable empty spaces, excluding the ghost house region.
        var width = LevelCanvasViewModel.GridWidth;
        var height = LevelCanvasViewModel.GridHeight;

        var spawn = CanvasViewModel.Cells.FirstOrDefault(c => c.TileType == CreativeTileType.PacmanSpawn);
        if (spawn == null)
        {
            return;
        }

        var visited = new bool[width, height];
        var queue = new Queue<(int X, int Y)>();
        queue.Enqueue((spawn.X, spawn.Y));
        visited[spawn.X, spawn.Y] = true;

        bool IsWalkable(LevelCell cell)
        {
            if (cell.TileType == CreativeTileType.Wall) return false;
            if (cell.IsPartOfGhostHouse) return false;
            return true;
        }

        while (queue.Count > 0)
        {
            var (x, y) = queue.Dequeue();

            foreach (var (nx, ny) in new[] { (x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1) })
            {
                if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
                if (visited[nx, ny]) continue;

                var cell = CanvasViewModel.Cells.First(c => c.X == nx && c.Y == ny);
                if (!IsWalkable(cell)) continue;

                visited[nx, ny] = true;
                queue.Enqueue((nx, ny));
            }
        }

        foreach (var cell in CanvasViewModel.Cells)
        {
            if (!visited[cell.X, cell.Y]) continue;
            if (cell.TileType != CreativeTileType.Empty) continue;
            cell.TileType = CreativeTileType.Dot;
        }
    }

    private void EnsurePacmanSpawn()
    {
        if (CanvasViewModel.Cells.Any(cell => cell.TileType == CreativeTileType.PacmanSpawn))
        {
            return;
        }

        var spawnCell = CanvasViewModel.Cells.FirstOrDefault(cell => cell.TileType == CreativeTileType.Empty && !cell.IsPartOfGhostHouse);
        if (spawnCell != null)
        {
            spawnCell.TileType = CreativeTileType.PacmanSpawn;
        }
    }

    private IEnumerable<string> ValidateLayout()
    {
        var errors = new List<string>();
        var lines = CanvasViewModel.BuildLevelLines();
        if (!CanvasViewModel.Cells.Any(c => c.TileType == CreativeTileType.PacmanSpawn))
        {
            errors.Add("Pac-Man spawn is missing.");
        }

        // Prefer the editor's ghost house marker; the structure may come from a template and
        // is easier to validate via the flagged region than by ASCII pattern matching.
        var hasGhostHouse = CanvasViewModel.Cells.Any(c => c.IsPartOfGhostHouse);
        if (!hasGhostHouse)
        {
            errors.Add("Ghost House is missing. Place exactly one 7x5 Ghost House structure.");
        }

        var ghostSpawnCount = CanvasViewModel.Cells.Count(c => c.TileType == CreativeTileType.GhostSpawn);
        if (ghostSpawnCount < 4)
        {
            errors.Add($"Ghost spawns are missing (need 4, found {ghostSpawnCount}). Place the Ghost House tool again.");
        }

        var pelletCount = lines.Sum(l => l.Count(c => c == 'o' || c == 'O'));
        if (pelletCount < 4)
        {
            errors.Add($"At least 4 power pellets are required (found {pelletCount}).");
        }

        if (!lines.Any(line => line.Contains('.')))
        {
            errors.Add("Dots are missing.");
        }
        return errors;
    }

    private static int CountGhostHouses(IReadOnlyList<string> lines)
    {
        const int ghostHouseWidth = 7;
        const int ghostHouseHeight = 5;

        if (lines.Count == 0)
        {
            return 0;
        }

        var width = lines.Max(l => l.Length);
        var height = lines.Count;

        char At(int x, int y)
        {
            if (y < 0 || y >= height) return ' ';
            var line = lines[y];
            return x < 0 || x >= line.Length ? ' ' : line[x];
        }

        bool IsGhostHouseAt(int startX, int startY)
        {
            bool IsGateRowAt(int rowY)
            {
                for (int dx = 0; dx < ghostHouseWidth; dx++)
                {
                    var c = At(startX + dx, rowY);
                    if (dx == 2 || dx == 3 || dx == 4)
                    {
                        if (c != '-') return false;
                    }
                    else
                    {
                        if (c != '#') return false;
                    }
                }
                return true;
            }

            bool IsWallRowAt(int rowY)
            {
                for (int dx = 0; dx < ghostHouseWidth; dx++)
                {
                    if (At(startX + dx, rowY) != '#') return false;
                }
                return true;
            }

            bool MiddleRowsOk()
            {
                for (int dy = 1; dy < ghostHouseHeight - 1; dy++)
                {
                    if (At(startX, startY + dy) != '#') return false;
                    if (At(startX + ghostHouseWidth - 1, startY + dy) != '#') return false;
                    for (int dx = 1; dx < ghostHouseWidth - 1; dx++)
                    {
                        var c = At(startX + dx, startY + dy);
                        if (c == '#' || c == '-') return false;
                    }
                }
                return true;
            }

            bool HasAnyGhostSpawn()
            {
                for (int dy = 0; dy < ghostHouseHeight; dy++)
                {
                    for (int dx = 0; dx < ghostHouseWidth; dx++)
                    {
                        if (At(startX + dx, startY + dy) == 'G') return true;
                    }
                }
                return false;
            }

            var topGate = IsGateRowAt(startY);
            var bottomGate = IsGateRowAt(startY + ghostHouseHeight - 1);

            if (!MiddleRowsOk())
            {
                return false;
            }

            if (topGate && IsWallRowAt(startY + ghostHouseHeight - 1) && HasAnyGhostSpawn()) return true;
            if (bottomGate && IsWallRowAt(startY) && HasAnyGhostSpawn()) return true;
            return false;
        }

        var count = 0;
        for (int y = 0; y <= height - ghostHouseHeight; y++)
        {
            for (int x = 0; x <= width - ghostHouseWidth; x++)
            {
                if (IsGhostHouseAt(x, y))
                {
                    count++;
                }
            }
        }
        return count;
    }

    private async Task PopulateCanvasFromArchiveAsync(string archivePath)
    {
        using var archive = ZipFile.OpenRead(archivePath);
        var config = await ReadProjectConfigFromArchiveAsync(archivePath);
        if (config == null)
        {
            _logger.LogWarning("No project.json found in project; loading first level only.");
        }

        if (config != null)
        {
            ProjectConfig.ProjectName = config.ProjectName;
            ProjectConfig.Author = config.Author;
            ProjectConfig.CreatedDate = config.CreatedDate;
            ProjectConfig.IsEditable = config.IsEditable;
            ProjectConfig.GlobalConfig = config.GlobalConfig;
            ProjectConfig.LevelConfigs = config.LevelConfigs;
        }

        _levelLinesByIndex.Clear();

        if (config != null)
        {
            ProjectConfig.GlobalConfig.LevelCount = config.GlobalConfig.LevelCount;
            ProjectConfig.GlobalConfig.WinScore = config.GlobalConfig.WinScore;
            ProjectConfig.GlobalConfig.Lives = config.GlobalConfig.Lives;
            this.RaisePropertyChanged(nameof(LevelCount));
            this.RaisePropertyChanged(nameof(WinScore));
            this.RaisePropertyChanged(nameof(Lives));
        }

        EnsureLevelConfigs();

        var levelCfgs = config?.LevelConfigs ?? new List<LevelConfig> { new() { LevelNumber = 1, Filename = "level1.txt" } };
        foreach (var levelCfg in levelCfgs)
        {
            var entry = archive.GetEntry(levelCfg.Filename) ??
                        archive.GetEntry($"level{levelCfg.LevelNumber}.txt") ??
                        archive.Entries.FirstOrDefault(e => e.Name.Equals(levelCfg.Filename, StringComparison.OrdinalIgnoreCase));
            if (entry == null) continue;

            await using var stream = entry.Open();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var lines = new List<string>();
            while (!reader.EndOfStream)
            {
                lines.Add(await reader.ReadLineAsync() ?? string.Empty);
            }
            _levelLinesByIndex[levelCfg.LevelNumber - 1] = NormalizeLines(lines);
        }

        CurrentLevelIndex = 0;
        LoadLevelIntoCanvas(0);
        _logger.LogInformation("Loaded {Count} level layouts from imported project.", _levelLinesByIndex.Count);
    }

    private string BuildLevelText()
    {
        var lines = CanvasViewModel.BuildLevelLines();
        return string.Join(Environment.NewLine, lines);
    }

    private ProjectConfig BuildProjectConfig(string projectName, bool isEditable)
    {
        var activeProfile = _profileManager.GetActiveProfile();
        ProjectConfig.ProjectName = string.IsNullOrWhiteSpace(projectName)
            ? $"Custom Level {DateTime.UtcNow:yyyyMMdd_HHmmss}"
            : projectName.Trim();
        ProjectConfig.Author = activeProfile?.Name ?? "Unknown Creator";
        ProjectConfig.CreatedDate = DateTime.UtcNow;
        ProjectConfig.IsEditable = isEditable;
        ProjectConfig.GlobalConfig.Lives = Lives;
        ProjectConfig.GlobalConfig.WinScore = WinScore;
        ProjectConfig.GlobalConfig.LevelCount = LevelCount;
        EnsureLevelConfigs();

        return ProjectConfig;
    }

    private ProjectMetadata BuildProjectMetadata(ProjectConfig project, IReadOnlyDictionary<int, (bool HasGhostHouse, List<GridPoint> GhostSpawns)> perLevelLayout)
    {
        var metadata = new ProjectMetadata
        {
            ProjectName = project.ProjectName,
            Author = project.Author,
            CreatedDate = project.CreatedDate,
            IsEditable = project.IsEditable,
            Global = new ProjectMetadataGlobal
            {
                Lives = project.GlobalConfig.Lives,
                LevelCount = project.GlobalConfig.LevelCount,
                WinScore = project.GlobalConfig.WinScore,
                WinScoreMin = WinScoreMin,
                WinScoreMax = WinScoreMax
            }
        };

        foreach (var level in project.LevelConfigs.OrderBy(l => l.LevelNumber))
        {
            var layout = perLevelLayout.TryGetValue(level.LevelNumber, out var value)
                ? value
                : (HasGhostHouse: false, GhostSpawns: new List<GridPoint>());

            metadata.Levels.Add(new ProjectMetadataLevel
            {
                LevelNumber = level.LevelNumber,
                Filename = level.Filename,
                PacmanSpeedMultiplier = level.PacmanSpeedMultiplier,
                GhostSpeedMultiplier = level.GhostSpeedMultiplier,
                SpeedMinMultiplier = LevelConfig.MinSpeedMultiplier,
                SpeedMaxMultiplier = LevelConfig.MaxSpeedMultiplier,
                FrightenedDurationSeconds = level.FrightenedDuration,
                FrightenedMaxSeconds = level.MaxFrightenedDuration,
                FruitPoints = level.FruitPoints,
                FruitMinPoints = 1,
                FruitMaxPoints = level.MaxFruitPoints,
                GhostEatPoints = level.GhostEatPoints,
                GhostEatMinPoints = 10,
                GhostEatMaxPoints = level.MaxGhostEatPoints,
                HasGhostHouse = layout.HasGhostHouse,
                GhostSpawns = layout.GhostSpawns
            });
        }

        return metadata;
    }

    private void NavigateToLevel(int index)
    {
        if (index < 0 || index >= LevelCount) return;
        SaveCurrentLevelToMemory();
        CurrentLevelIndex = index;
        LoadLevelIntoCanvas(index);
    }

    private void SaveCurrentLevelToMemory()
    {
        _levelLinesByIndex[CurrentLevelIndex] = CanvasViewModel.BuildLevelLines();
    }

    private void LoadLevelIntoCanvas(int index)
    {
        var lines = _levelLinesByIndex.TryGetValue(index, out var stored)
            ? stored
            : CreateTemplateLevelLines();
        CanvasViewModel.LoadFromLines(lines);
    }

    private static string[] NormalizeLines(IEnumerable<string> lines)
    {
        var normalized = lines.Take(LevelCanvasViewModel.GridHeight).ToList();
        while (normalized.Count < LevelCanvasViewModel.GridHeight) normalized.Add(string.Empty);

        for (var i = 0; i < normalized.Count; i++)
        {
            var line = normalized[i] ?? string.Empty;
            if (line.Length < LevelCanvasViewModel.GridWidth)
            {
                line = line.PadRight(LevelCanvasViewModel.GridWidth, ' ');
            }
            else if (line.Length > LevelCanvasViewModel.GridWidth)
            {
                line = line[..LevelCanvasViewModel.GridWidth];
            }
            normalized[i] = line;
        }

        return normalized.ToArray();
    }

    private static string[] CreateBlankLevelLines()
    {
        var lines = new string[LevelCanvasViewModel.GridHeight];
        for (var y = 0; y < LevelCanvasViewModel.GridHeight; y++)
        {
            lines[y] = new string(' ', LevelCanvasViewModel.GridWidth);
        }
        return lines;
    }

    private static string[] CreateTemplateLevelLines()
    {
        // Template mirrors LevelCanvasViewModel.SeedDemoLayout:
        // - outer boundary walls
        // - one 7x5 ghost house with '-' gate
        // - Pac-Man spawn
        // - 4 power pellets (required by validation)
        const int width = LevelCanvasViewModel.GridWidth;
        const int height = LevelCanvasViewModel.GridHeight;

        var grid = new char[height, width];
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            grid[y, x] = ' ';
        }

        // Border walls.
        for (var x = 0; x < width; x++)
        {
            grid[0, x] = '#';
            grid[height - 1, x] = '#';
        }

        for (var y = 0; y < height; y++)
        {
            grid[y, 0] = '#';
            grid[y, width - 1] = '#';
        }

        // Ghost house at a stable position (top-left of 7x5 region).
        var houseX = 10;
        var houseY = 11;
        var gateRow = "##---##".ToCharArray();     // 7 chars
        var middleRow = "#     #".ToCharArray();   // 7 chars
        var wallRow = "#######".ToCharArray();     // 7 chars

        for (var dx = 0; dx < 7; dx++) grid[houseY + 0, houseX + dx] = gateRow[dx];
        for (var dx = 0; dx < 7; dx++) grid[houseY + 1, houseX + dx] = middleRow[dx];
        for (var dx = 0; dx < 7; dx++) grid[houseY + 2, houseX + dx] = middleRow[dx];
        for (var dx = 0; dx < 7; dx++) grid[houseY + 3, houseX + dx] = middleRow[dx];
        for (var dx = 0; dx < 7; dx++) grid[houseY + 4, houseX + dx] = wallRow[dx];

        // Pac-Man spawn.
        var pacX = width / 2;
        var pacY = height - 6;
        if (grid[pacY, pacX] == ' ')
        {
            grid[pacY, pacX] = 'P';
        }

        // Required pellets.
        grid[1, 1] = 'o';
        grid[1, width - 2] = 'o';
        grid[height - 2, 1] = 'o';
        grid[height - 2, width - 2] = 'o';

        var lines = new string[height];
        for (var y = 0; y < height; y++)
        {
            var row = new char[width];
            for (var x = 0; x < width; x++) row[x] = grid[y, x];
            lines[y] = new string(row);
        }
        return lines;
    }
}
