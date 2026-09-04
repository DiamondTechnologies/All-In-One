using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using All_In_One.Models;
using All_In_One.Services;
using Microsoft.Windows.ApplicationModel.Resources;

namespace All_In_One.ViewModels;

public partial class MainViewModel(IFileService fileService) : INotifyPropertyChanged
{
    private readonly IFileService _fileService = fileService;

    private readonly HashSet<string> _selectedExtensions =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, bool> _selectedStateCache =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly List<string> _allValidFiles = [];

    public ObservableCollection<FileNode> TreeNodes { get; } = [];

    public ObservableCollection<string> AvailableExtensions { get; } = [];

    private static readonly ResourceLoader _resources = new();

    public string SearchText
    {
        get;

        set
        {
            if (field == value)
            {
                return;
            }

            field = value;

            OnPropertyChanged(nameof(SearchText));

            ApplyFilters();
        }
    } = string.Empty;

    public bool IsLoading
    {
        get;

        set
        {
            if (field == value)
            {
                return;
            }

            field = value;

            OnPropertyChanged(nameof(IsLoading));
        }
    }

    public string StatusText
    {
        get;

        set
        {
            if (field == value)
            {
                return;
            }

            field = value;

            OnPropertyChanged(nameof(StatusText));
        }
    } = _resources.GetString("FileScanning/FileScan");

    public int ProgressValue
    {
        get;

        set
        {
            if (field == value)
            {
                return;
            }

            field = value;

            OnPropertyChanged(nameof(ProgressValue));
        }
    }

    public string CounterText
    {
        get;

        set
        {
            if (field == value)
            {
                return;
            }

            field = value;

            OnPropertyChanged(nameof(CounterText));
        }
    } = "0 / 0";

    public bool HasFiles => _allValidFiles.Count > 0;

    public bool MaskData { get; set; } = true;

    public bool WarnKeys { get; set; } = true;

    public bool RemoveComments { get; set; } = true;

    public CommentTypeOptions CommentTypes { get; set; } = new();

    public async Task InitializeWithPathsAsync(List<string> paths)
    {
        if (paths == null || paths.Count == 0)
        {
            return;
        }

        IsLoading = true;
        StatusText = _resources.GetString("FileScanning/FileScan");

        try
        {
            (List<string>? scannedFiles, bool accessDenied) =
                await _fileService.ScanPathsAsync(paths);

            foreach (string file in scannedFiles)
            {
                if (!_selectedStateCache.ContainsKey(file))
                {
                    _selectedStateCache[file] = true;
                }
            }

            _allValidFiles.AddRange(scannedFiles);

            List<string> distinctFiles = [.. _allValidFiles.Distinct(StringComparer.OrdinalIgnoreCase)];

            _allValidFiles.Clear();
            _allValidFiles.AddRange(distinctFiles);

            UpdateExtensionsList();
            ApplyFilters();

            OnPropertyChanged(nameof(HasFiles));

            if (accessDenied)
            {
                AccessDeniedOccurred?.Invoke();
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task PerformMergeAsync(
        string saveFilePath,
        Progress<int> progress)
    {
        List<string> selectedFiles = [.. _allValidFiles
            .Where(file =>
                _selectedStateCache.TryGetValue(
                    file,
                    out bool selected) &&
                selected)];

        if (selectedFiles.Count == 0)
        {
            return;
        }

        IsLoading = true;

        try
        {
            MergeOptions options = new(
                MaskData: MaskData,
                WarnKeys: WarnKeys,
                RemoveComments: RemoveComments,
                CommentTypes: CommentTypes);

            Dictionary<string, string> filesWithKeys =
                await _fileService.MergeFilesAsync(
                    selectedFiles,
                    saveFilePath,
                    options,
                    progress);

            if (filesWithKeys.Count > 0)
            {
                KeyLeakDetected?.Invoke(filesWithKeys);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void SetSelectionState(bool state)
    {
        foreach (string file in _allValidFiles)
        {
            _selectedStateCache[file] = state;
        }

        foreach (FileNode folder in TreeNodes)
        {
            folder.SetIsSelectedSilently(state);
        }

        foreach (FileNode folder in TreeNodes)
        {
            folder.UpdateFolderSelectionState();
        }

        RefreshTreeBindings();
        UpdateCounter();
    }

    public void ResetSelection()
    {
        _allValidFiles.Clear();
        _selectedStateCache.Clear();
        TreeNodes.Clear();

        SearchText = string.Empty;

        UpdateExtensionsList();
        UpdateCounter();

        OnPropertyChanged(nameof(HasFiles));
    }

    private void UpdateExtensionsList()
    {
        List<string> extensions = [.. _allValidFiles
            .Select(file =>
            {
                string extension =
                    Path.GetExtension(file);

                return string.IsNullOrEmpty(extension)
                    ? _resources.GetString("FileFilter/NoExtension")
                    : extension;
            })
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(extension => extension)];

        AvailableExtensions.Clear();

        foreach (string? extension in extensions)
        {
            AvailableExtensions.Add(extension);
        }

        _selectedExtensions.IntersectWith(
            AvailableExtensions);
    }

    public void ApplyFilters()
    {
        string search =
            SearchText.Trim().ToLowerInvariant();

        List<string> filteredFiles = [.. _allValidFiles
            .Where(file =>
            {
                bool matchesSearch =
                    string.IsNullOrEmpty(search) ||
                    Path.GetFileName(file).Contains(search, StringComparison.InvariantCultureIgnoreCase);

                string extension = Path.GetExtension(file);

                if (string.IsNullOrEmpty(extension))
                {
                    extension = _resources.GetString("FileFilter/NoExtension");
                }

                bool matchesExtension =
                    _selectedExtensions.Count == 0 ||
                    _selectedExtensions.Contains(extension);

                return matchesSearch &&
                       matchesExtension;
            })];

        BuildTree(filteredFiles);

        UpdateCounter();
    }

    private void BuildTree(List<string> files)
    {
        TreeNodes.Clear();

        Dictionary<string, FileNode> foldersMap =
            new(
                StringComparer.OrdinalIgnoreCase);

        foreach (string file in files)
        {
            string directory =
                Path.GetDirectoryName(file) ?? string.Empty;

            string directoryName =
                Path.GetFileName(directory);

            if (string.IsNullOrEmpty(directoryName))
            {
                directoryName = directory;
            }

            if (!foldersMap.TryGetValue(
                    directory,
                    out FileNode? folderNode))
            {
                folderNode = new FileNode
                {
                    Name = directoryName,
                    FullPath = directory,
                    IsFolder = true,
                    IsExpanded = true
                };

                foldersMap[directory] = folderNode;

                TreeNodes.Add(folderNode);
            }

            bool isSelected =
                !_selectedStateCache.TryGetValue(
                    file,
                    out bool cachedState) ||
                cachedState;

            FileNode fileNode = new()
            {
                Name = Path.GetFileName(file),
                FullPath = file,
                IsFolder = false,
                Parent = folderNode,
                IsSelected = isSelected
            };

            fileNode.SelectionChangedCallback =
                () =>
                {
                    if (fileNode.IsSelected.HasValue)
                    {
                        _selectedStateCache[
                            fileNode.FullPath] =
                            fileNode.IsSelected.Value;
                    }

                    UpdateCounter();

                    folderNode.UpdateFolderSelectionState();

                    RefreshTreeBindings();
                };

            folderNode.Children.Add(fileNode);
        }

        foreach (FileNode folder in TreeNodes)
        {
            folder.SelectionChangedCallback =
                () =>
                {
                    SyncFolderSelectionToCache(folder);

                    UpdateCounter();

                    folder.UpdateFolderSelectionState();

                    RefreshTreeBindings();
                };

            folder.UpdateFolderSelectionState();
        }
    }

    private void SyncFolderSelectionToCache(FileNode folder)
    {
        foreach (FileNode child in folder.Children)
        {
            if (child.IsFolder)
            {
                SyncFolderSelectionToCache(child);
            }
            else if (child.IsSelected.HasValue)
            {
                _selectedStateCache[
                    child.FullPath] =
                    child.IsSelected.Value;
            }
        }
    }

    private static void RefreshTreeBindings() { }

    private void UpdateCounter()
    {
        int selected =
            _allValidFiles.Count(
                file =>
                    _selectedStateCache.TryGetValue(
                        file,
                        out bool isSelected) &&
                    isSelected);

        CounterText =
            $"{selected} / {_allValidFiles.Count}";
    }

    public bool IsExtensionSelected(string extension)
    {
        return _selectedExtensions.Count == 0 ||
               _selectedExtensions.Contains(extension);
    }

    public void UpdateExtensionFilter(
        List<string> selectedExtensions)
    {
        _selectedExtensions.Clear();

        if (selectedExtensions.Count > 0 &&
            selectedExtensions.Count !=
            AvailableExtensions.Count)
        {
            foreach (string extension in selectedExtensions)
            {
                _ = _selectedExtensions.Add(extension);
            }
        }

        ApplyFilters();
    }

    public event Action? AccessDeniedOccurred;

    public event Action<Dictionary<string, string>>?
        KeyLeakDetected;

    public event PropertyChangedEventHandler?
        PropertyChanged;

    protected void OnPropertyChanged(
        string propertyName)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}