using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using All_In_One.Models;
using All_In_One.Services;
using All_In_One.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.Windows.ApplicationModel.Resources;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace All_In_One
{
    public sealed partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; }

        private readonly ResourceLoader _resources;

        private bool _changingRemoveCommentsState;

        [GeneratedRegex(@"(?<=-----BEGIN [^\r\n]+-----\r?\n)([\s\S]*?)(?=\r?\n-----END)")]
        private static partial Regex PemKeyMaskRegex();

        public MainWindow()
        {
            TextProcessorService textProcessor = new();
            FileService fileService = new(textProcessor);

            ViewModel = new MainViewModel(fileService);

            InitializeComponent();

            nint hwnd = WindowNative.GetWindowHandle(this);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            AppWindow appWindow = AppWindow.GetFromWindowId(windowId);

            appWindow.SetIcon(
                System.IO.Path.Combine(
                    AppContext.BaseDirectory,
                    "Assets",
                    "AppIcon.ico"));

            _resources = new ResourceLoader();

            FileTree.ItemsSource = ViewModel.TreeNodes;

            ViewModel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(ViewModel.HasFiles))
                {
                    UpdateFilesView();
                }
            };

            UpdateFilesView();

            ViewModel.AccessDeniedOccurred += async () =>
                await ShowSimpleDialogAsync(
                    _resources.GetString("AccessDenied/Title"),
                    _resources.GetString("AccessDenied/Text"));

            ViewModel.KeyLeakDetected += async (files) =>
                await ShowKeyLeakDialogAsync(files);
        }

        private string GetString(string key)
        {
            string value = _resources.GetString(key);

            return string.IsNullOrEmpty(value)
                ? $"[{key}]"
                : value;
        }

        private void UpdateFilesView()
        {
            bool hasFiles = ViewModel.HasFiles;

            FilterPanel.Visibility = hasFiles ? Visibility.Visible : Visibility.Collapsed;

            FileTree.Visibility = hasFiles ? Visibility.Visible : Visibility.Collapsed;

            EmptyStatePanel.Visibility = hasFiles ? Visibility.Collapsed : Visibility.Visible;
        }

        public async Task InitializeWithPathsAsync(List<string> paths)
        {
            await ViewModel.InitializeWithPathsAsync(paths);
            UpdateFilesView();
        }

        private async void AddFile_Click(object _, RoutedEventArgs __)
        {
            try
            {
                nint hwnd = WindowNative.GetWindowHandle(this);

                FileOpenPicker picker = new();

                InitializeWithWindow.Initialize(
                    picker,
                    hwnd);

                picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

                picker.FileTypeFilter.Add("*");

                IReadOnlyList<StorageFile> files =
                    await picker.PickMultipleFilesAsync();

                if (files != null && files.Count > 0)
                {
                    await ViewModel.InitializeWithPathsAsync(
                        [.. files.Select(f => f.Path)]);
                }
            }
            catch (Exception mainEx)
            {
                try
                {
                    nint hwnd = WindowNative.GetWindowHandle(this);

                    List<string> files = Win32DialogHelper.PickFiles(hwnd);

                    if (files != null && files.Count > 0)
                    {
                        await ViewModel.InitializeWithPathsAsync(files);
                    }
                }
                catch (Exception fallbackEx)
                {
                    await ShowSimpleDialogAsync(
                        _resources.GetString("FilePickerError/Title"),
                        string.Format(
                            _resources.GetString("FilePickerError/Text"),
                            mainEx.Message,
                            fallbackEx.Message));
                }
            }
        }

        private async void AddFolder_Click(object _, RoutedEventArgs __)
        {
            try
            {
                nint hwnd = WindowNative.GetWindowHandle(this);

                FolderPicker folderPicker = new();

                InitializeWithWindow.Initialize(folderPicker, hwnd);

                folderPicker.SuggestedStartLocation = PickerLocationId.Desktop;

                folderPicker.FileTypeFilter.Add("*");

                StorageFolder folder = await folderPicker.PickSingleFolderAsync();

                if (folder != null)
                {
                    await ViewModel.InitializeWithPathsAsync([folder.Path]);
                }
            }
            catch (Exception mainEx)
            {
                try
                {
                    nint hwnd = WindowNative.GetWindowHandle(this);

                    string? folderPath = Win32DialogHelper.PickFolder(hwnd);

                    if (!string.IsNullOrEmpty(folderPath))
                    {
                        await ViewModel.InitializeWithPathsAsync([folderPath]);
                    }
                }
                catch (Exception fallbackEx)
                {
                    await ShowSimpleDialogAsync(
                        _resources.GetString("FolderPickerError/Title"),
                        string.Format(
                            _resources.GetString("FolderPickerError/Text"),
                            mainEx.Message,
                            fallbackEx.Message));
                }
            }
        }

        private async void MergeButton_Click(object _, RoutedEventArgs __)
        {
            ViewModel.WarnKeys = WarnKeysCheckBox.IsChecked == true;

            ViewModel.MaskData = MaskDataCheckBox.IsChecked == true;

            try
            {
                FileSavePicker savePicker = new();

                nint hwnd = WindowNative.GetWindowHandle(this);

                InitializeWithWindow.Initialize(savePicker, hwnd);

                savePicker.FileTypeChoices.Add(_resources.GetString("SaveFileType/Text"), [".txt"]);

                savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

                savePicker.SuggestedFileName = "MergedOutput.txt";

                StorageFile saveFile = await savePicker.PickSaveFileAsync();

                if (saveFile != null)
                {
                    Progress<int> progress = new(percent =>
                    {
                        ViewModel.ProgressValue = percent;

                        ViewModel.StatusText =
                            string.Format(_resources.GetString("Loading/Merge/Text"), percent);
                    });

                    await ViewModel.PerformMergeAsync(
                        saveFile.Path,
                        progress);
                }
            }
            catch (Exception ex)
            {
                await ShowSimpleDialogAsync(
                    _resources.GetString("ErrorDialog/Title"),
                    string.Format(_resources.GetString("SaveDialogError/Text"), ex.Message));
            }
        }

        private void SelectAll_Click(object _, RoutedEventArgs __)
        {
            ViewModel.SetSelectionState(true);
        }

        private void DeselectAll_Click(object _, RoutedEventArgs __)
        {
            ViewModel.SetSelectionState(false);
        }

        private void ResetSelection_Click(object _, RoutedEventArgs __)
        {
            ViewModel.ResetSelection();
        }

        private void Grid_DragOver(object _, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                e.AcceptedOperation = DataPackageOperation.Copy;
                e.Handled = true;
            }
        }

        private async void Grid_Drop(object _, DragEventArgs e)
        {
            if (!e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                return;
            }

            DragOperationDeferral deferral = e.GetDeferral();

            try
            {
                IReadOnlyList<IStorageItem> items = await e.DataView.GetStorageItemsAsync();

                if (items != null && items.Count > 0)
                {
                    List<string> paths =
                        [.. items
                            .Select(i => i.Path)
                            .Where(p => !string.IsNullOrEmpty(p))];

                    if (paths.Count > 0)
                    {
                        await ViewModel.InitializeWithPathsAsync(paths);
                    }
                }
            }
            catch (Exception ex)
            {
                await ShowSimpleDialogAsync(_resources.GetString("ErrorDialog/Title"), string.Format(_resources.GetString("DropError/Text"), ex.Message));
            }
            finally
            {
                deferral.Complete();
            }
        }

        private void FileNodeCheckBox_Click(object sender, RoutedEventArgs _)
        {
            if (sender is not CheckBox checkBox)
            {
                return;
            }

            if (checkBox.Tag is not FileNode node)
            {
                return;
            }

            node.ToggleSelection();
        }

        private void SearchBox_TextChanged(object _, TextChangedEventArgs __)
        {
            ViewModel.SearchText = SearchBox.Text;
        }

        private void RemoveCommentsCheckBox_Click(object _, RoutedEventArgs __)
        {
            if (_changingRemoveCommentsState)
            {
                return;
            }

            if (RemoveCommentsCheckBox.IsChecked == null)
            {
                _changingRemoveCommentsState = true;

                RemoveCommentsCheckBox.IsChecked = false;
                ViewModel.RemoveComments = false;
                SelectCommentTypesButton.IsEnabled = false;

                _changingRemoveCommentsState = false;
                return;
            }

            bool enabled = RemoveCommentsCheckBox.IsChecked == true;

            ViewModel.RemoveComments = enabled;
            SelectCommentTypesButton.IsEnabled = enabled;
        }

        private async void SelectCommentTypes_Click(object _, RoutedEventArgs __)
        {
            StackPanel panel = new()
            {
                Spacing = 10
            };

            CheckBox cbSlash = new()
            {
                Content = GetString("CommentType/CStyle"),

                IsChecked = ViewModel.CommentTypes.RemoveSlashSlash
            };

            CheckBox cbHash = new()
            {
                Content = GetString("CommentType/Hash"),

                IsChecked = ViewModel.CommentTypes.RemoveHash
            };

            CheckBox cbBlock = new()
            {
                Content = GetString("CommentType/Block"),

                IsChecked = ViewModel.CommentTypes.RemoveBlock
            };

            CheckBox cbSummary = new()
            {
                Content = GetString("CommentType/Summary"),

                IsChecked = ViewModel.CommentTypes.RemoveSummary
            };

            CheckBox cbTriple = new()
            {
                Content = GetString("CommentType/TripleQuotes"),

                IsChecked = ViewModel.CommentTypes.RemoveTripleQuotes
            };

            CheckBox cbXml = new()
            {
                Content = GetString("CommentType/XmlHtml"),

                IsChecked = ViewModel.CommentTypes.RemoveXmlHtml
            };

            panel.Children.Add(cbSlash);
            panel.Children.Add(cbHash);
            panel.Children.Add(cbBlock);
            panel.Children.Add(cbSummary);
            panel.Children.Add(cbTriple);
            panel.Children.Add(cbXml);

            ContentDialog dialog = new()
            {
                Title = GetString("SelectCommentTypesDialog/Title"),

                Content = new ScrollViewer { Content = panel },

                PrimaryButtonText = GetString("ApplyButton/Text"),

                CloseButtonText = GetString("CancelButton/Text"),

                XamlRoot = Content.XamlRoot
            };

            if (await dialog.ShowAsync() ==
                ContentDialogResult.Primary)
            {
                bool removeSlashSlash = cbSlash.IsChecked == true;
                bool removeHash = cbHash.IsChecked == true;
                bool removeBlock = cbBlock.IsChecked == true;
                bool removeSummary = cbSummary.IsChecked == true;
                bool removeTripleQuotes = cbTriple.IsChecked == true;
                bool removeXmlHtml = cbXml.IsChecked == true;

                ViewModel.CommentTypes = new CommentTypeOptions(
                    RemoveSlashSlash: removeSlashSlash,
                    RemoveHash: removeHash,
                    RemoveBlock: removeBlock,
                    RemoveSummary: removeSummary,
                    RemoveTripleQuotes: removeTripleQuotes,
                    RemoveXmlHtml: removeXmlHtml
                );

                int selectedCount =
                    (removeSlashSlash ? 1 : 0) +
                    (removeHash ? 1 : 0) +
                    (removeBlock ? 1 : 0) +
                    (removeSummary ? 1 : 0) +
                    (removeTripleQuotes ? 1 : 0) +
                    (removeXmlHtml ? 1 : 0);

                int totalCount = 6;

                _changingRemoveCommentsState = true;

                if (selectedCount == 0)
                {
                    RemoveCommentsCheckBox.IsChecked = false;
                    ViewModel.RemoveComments = false;
                    SelectCommentTypesButton.IsEnabled = false;
                }
                else if (selectedCount == totalCount)
                {
                    RemoveCommentsCheckBox.IsChecked = true;
                    ViewModel.RemoveComments = true;
                    SelectCommentTypesButton.IsEnabled = true;
                }
                else
                {
                    RemoveCommentsCheckBox.IsChecked = null;
                    ViewModel.RemoveComments = true;
                    SelectCommentTypesButton.IsEnabled = true;
                }

                _changingRemoveCommentsState = false;
            }
        }

        private async Task ShowKeyLeakDialogAsync(
            Dictionary<string, string> leaks)
        {
            StackPanel mainPanel = new()
            {
                Spacing = 12
            };

            mainPanel.Children.Add(new TextBlock
            {
                Text = _resources.GetString(
                            "KeyLeak/Text"),

                TextWrapping = TextWrapping.Wrap
            });

            foreach (KeyValuePair<string, string> leak in leaks)
            {
                string fileName = leak.Key;
                string rawKey = leak.Value;

                string maskedKey = rawKey.Contains("-----END")
                    ? PemKeyMaskRegex().Replace(
                            rawKey,
                            "****************************************\n****************************************")
                    : rawKey +
                        "\n****************************************\n****************************************";
                StackPanel keyContainer = new()
                {
                    Spacing = 4,
                    Margin = new Thickness(0, 4, 0, 8)
                };

                Grid headerGrid = new();

                headerGrid.ColumnDefinitions.Add(new ColumnDefinition
                {
                    Width = new GridLength(1, GridUnitType.Star)
                });

                headerGrid.ColumnDefinitions.Add(new ColumnDefinition
                {
                    Width = GridLength.Auto
                });

                TextBlock fileLabel = new()
                {
                    Text = fileName,
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                    VerticalAlignment = VerticalAlignment.Center
                };

                Grid.SetColumn(fileLabel, 0);

                ToggleButton toggleButton = new()
                {
                    Content = new FontIcon
                    {
                        Glyph = "\uE890"
                    },

                    IsChecked = false
                };

                ToolTipService.SetToolTip(toggleButton, _resources.GetString("KeyLeak/ShowHide/ToolTip"));

                Grid.SetColumn(toggleButton, 1);

                headerGrid.Children.Add(fileLabel);
                headerGrid.Children.Add(toggleButton);

                TextBox keyBox = new()
                {
                    Text = maskedKey,
                    IsReadOnly = true,
                    AcceptsReturn = true,
                    TextWrapping = TextWrapping.Wrap,
                    FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"),
                    MinHeight = 90,
                    MaxHeight = 140
                };

                toggleButton.Click += (_, __) =>
                {
                    bool isVisible = toggleButton.IsChecked == true;

                    keyBox.Text =
                        isVisible
                            ? rawKey
                            : maskedKey;

                    ((FontIcon) toggleButton.Content).Glyph =
                        isVisible
                            ? "\uED1A"
                            : "\uE890";
                };

                keyContainer.Children.Add(headerGrid);

                keyContainer.Children.Add(keyBox);

                mainPanel.Children.Add(keyContainer);
            }

            ContentDialog dialog = new()
            {
                Title = _resources.GetString("KeyLeak/Title"),

                Content =
                        new ScrollViewer
                        {
                            Content = mainPanel,
                            MaxHeight = 400
                        },

                CloseButtonText = _resources.GetString("KeyLeak/CloseButton/Text"),

                XamlRoot = Content.XamlRoot
            };

            _ = await dialog.ShowAsync();
        }

        private async Task ShowSimpleDialogAsync(string title, string content)
        {
            ContentDialog dialog =
                new()
                {
                    Title = title,

                    Content = content,

                    CloseButtonText =
                        _resources.GetString(
                            "DialogCloseButton/Text"),

                    XamlRoot =
                        Content.XamlRoot
                };

            _ = await dialog.ShowAsync();
        }

        private async void ExtensionFilterButton_Click(
            object _,
            RoutedEventArgs __)
        {
            if (ViewModel.AvailableExtensions.Count == 0)
            {
                await ShowSimpleDialogAsync(
                    _resources.GetString("FilterDialog/Title"),
                    _resources.GetString("NoFilesForFilter/Text"));

                return;
            }

            StackPanel panel = new()
            {
                Spacing = 10
            };

            List<(string Ext, CheckBox Cb)> checkBoxes = [];

            foreach (string ext in ViewModel.AvailableExtensions)
            {
                CheckBox cb = new()
                {
                    Content = ext,
                    IsChecked = ViewModel.IsExtensionSelected(ext)
                };

                checkBoxes.Add((ext, cb));
                panel.Children.Add(cb);
            }

            ContentDialog dialog = new()
            {
                Title = _resources.GetString("FilterDialog/Title"),
                Content = new ScrollViewer
                {
                    Content = panel,
                    MaxHeight = 300
                },
                PrimaryButtonText = _resources.GetString("ApplyButton/Text"),
                CloseButtonText = _resources.GetString("ResetFiltersButton/Text"),
                XamlRoot = Content.XamlRoot
            };

            ContentDialogResult result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                List<string> selected = [.. checkBoxes
                    .Where(x => x.Cb.IsChecked == true)
                    .Select(x => x.Ext)];

                ViewModel.UpdateExtensionFilter(selected);
            }
            else if (result == ContentDialogResult.Secondary)
            {
                ViewModel.UpdateExtensionFilter([]);
            }
        }
    }
}