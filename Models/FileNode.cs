using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace All_In_One.Models;

public partial class FileNode : INotifyPropertyChanged
{
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public bool IsFolder { get; set; }
    public string IconGlyph => IsFolder ? "\uE8B7" : "\uE8A5";
    public ObservableCollection<FileNode> Children { get; } = [];
    public FileNode? Parent { get; set; }
    public Action? SelectionChangedCallback { get; set; }
    public bool IsExpanded
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            OnPropertyChanged(nameof(IsExpanded));
        }
    } = true;
    private bool? _isSelected = true;
    public bool? IsSelected
    {
        get => _isSelected;
        set
        {
            if (!IsFolder && !value.HasValue)
            {
                value = false;
            }

            if (_isSelected == value)
            {
                return;
            }

            _isSelected = value;

            OnPropertyChanged(nameof(IsSelected));

            if (IsFolder && value.HasValue)
            {
                foreach (FileNode child in Children)
                {
                    child.SetIsSelectedSilently(value.Value);
                }
            }

            Parent?.UpdateFolderSelectionState();

            SelectionChangedCallback?.Invoke();
        }
    }
    public void ToggleSelection()
    {
        IsSelected = IsSelected != true;
    }
    internal void SetIsSelectedSilently(bool value)
    {
        bool changed = _isSelected != value;

        _isSelected = value;

        if (changed)
        {
            OnPropertyChanged(nameof(IsSelected));
        }

        if (IsFolder)
        {
            foreach (FileNode child in Children)
            {
                child.SetIsSelectedSilently(value);
            }
        }
    }
    public void UpdateFolderSelectionState()
    {
        if (!IsFolder || Children.Count == 0)
        {
            return;
        }

        bool allSelected = Children.All(c => c.IsSelected == true);

        bool allUnselected = Children.All(c => c.IsSelected == false);

        bool? newState = allSelected ? true : allUnselected ? false : null;
        if (_isSelected != newState)
        {
            _isSelected = newState;

            OnPropertyChanged(nameof(IsSelected));

            Parent?.UpdateFolderSelectionState();
        }
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}