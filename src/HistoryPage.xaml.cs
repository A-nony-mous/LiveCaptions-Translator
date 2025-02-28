using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LiveCaptionsTranslator.controllers;
using LiveCaptionsTranslator.models;
using Microsoft.Win32;
using Wpf.Ui.Controls;

namespace LiveCaptionsTranslator
{
    public partial class HistoryPage : Page
    {
        public HistoryPage()
        {
            InitializeComponent();
            var viewModel = new HistoryPageViewModel();

            viewModel.ShowSnackbarAction = (title, message, isError) =>
            {
                ShowSnackbar(title, message, isError);
            };
            DataContext = viewModel;
        }

        private void ShowSnackbar(string title, string message, bool isError = false)
        {
            var snackbar = new Snackbar(SnackbarHost)
            {
                Title = title,
                Content = message,
                Appearance = isError ? ControlAppearance.Danger : ControlAppearance.Light,
                Timeout = TimeSpan.FromSeconds(2)
            };
            snackbar.Show();
        }

        #region RelayCommand 

        public class RelayCommand : ICommand
        {
            private readonly Func<object, Task> _executeAsync;
            private readonly Action<object> _execute;
            private readonly Predicate<object> _canExecute;

            public RelayCommand(Func<object, Task> executeAsync, Predicate<object> canExecute = null)
            {
                _executeAsync = executeAsync;
                _canExecute = canExecute;
            }

            public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
            {
                _execute = execute;
                _canExecute = canExecute;
            }

            public bool CanExecute(object parameter) => _canExecute == null || _canExecute(parameter);

            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }

            public async void Execute(object parameter)
            {
                if (_executeAsync != null)
                {
                    await _executeAsync(parameter);
                }
                else
                {
                    _execute?.Invoke(parameter);
                }
            }
        }
        
        #endregion

        #region HistoryPageViewModel

        public class HistoryPageViewModel : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;
            private void OnPropertyChanged(string propertyName) =>
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            
            private int _page = 1;
            public int Page
            {
                get => _page;
                set { _page = value; OnPropertyChanged(nameof(Page)); UpdatePageNumberText(); }
            }

            private int _maxPage = 1;
            public int MaxPage
            {
                get => _maxPage;
                set { _maxPage = value; OnPropertyChanged(nameof(MaxPage)); UpdatePageNumberText(); }
            }

            private int _maxRow = 20;
            public int MaxRow
            {
                get => _maxRow;
                set { _maxRow = value; OnPropertyChanged(nameof(MaxRow)); LoadHistoryAsync(); }
            }

            private string _pageNumberText = "1/1";
            public string PageNumberText
            {
                get => _pageNumberText;
                set { _pageNumberText = value; OnPropertyChanged(nameof(PageNumberText)); }
            }
            
            private string _selectedMaxRowString = "20";
            public string SelectedMaxRowString
            {
                get => _selectedMaxRowString;
                set
                {
                    if (_selectedMaxRowString != value)
                    {
                        _selectedMaxRowString = value;
                        OnPropertyChanged(nameof(SelectedMaxRowString));
                        if (int.TryParse(value, out int newMaxRow))
                        {
                            MaxRow = newMaxRow;
                        }
                        if (value == "20") App.Settings.HistoryMaxRow = 0;
                        else if (value == "30") App.Settings.HistoryMaxRow = 1;
                        else if (value == "50") App.Settings.HistoryMaxRow = 2;
                        else if (value == "100") App.Settings.HistoryMaxRow = 3;
                    }
                }
            }

            private ObservableCollection<TranslationHistoryEntry> _historyEntries = new ObservableCollection<TranslationHistoryEntry>();
            public ObservableCollection<TranslationHistoryEntry> HistoryEntries
            {
                get => _historyEntries;
                set { _historyEntries = value; OnPropertyChanged(nameof(HistoryEntries)); }
            }
            
            public Action<string, string, bool> ShowSnackbarAction { get; set; }
            public ICommand PageDownCommand { get; }
            public ICommand PageUpCommand { get; }
            public ICommand ReloadLogsCommand { get; }
            public ICommand DeleteHistoryCommand { get; }
            public ICommand ExportHistoryCommand { get; }

            public HistoryPageViewModel()
            {
                switch (App.Settings.HistoryMaxRow)
                {
                    case 1: _selectedMaxRowString = "30"; break;
                    case 2: _selectedMaxRowString = "50"; break;
                    case 3: _selectedMaxRowString = "100"; break;
                    default: _selectedMaxRowString = "20"; break;
                }
                _maxRow = int.Parse(_selectedMaxRowString);

                PageDownCommand = new RelayCommand(async _ =>
                {
                    if (Page > 1)
                    {
                        Page--;
                        await LoadHistoryAsync();
                    }
                });

                PageUpCommand = new RelayCommand(async _ =>
                {
                    if (Page < MaxPage)
                    {
                        Page++;
                        await LoadHistoryAsync();
                    }
                });

                ReloadLogsCommand = new RelayCommand(async _ =>
                {
                    await LoadHistoryAsync();
                });

                DeleteHistoryCommand = new RelayCommand(async _ =>
                {
                    var dialog = new ContentDialog
                    {
                        Title = "Do you want to delete all history?",
                        PrimaryButtonText = "Yes",
                        CloseButtonText = "No",
                        DefaultButton = ContentDialogButton.Close,
                        DialogHost = (Application.Current.MainWindow as MainWindow)?.DialogHostContainer,
                        Padding = new Thickness(8, 4, 8, 8)
                    };

                    var result = await dialog.ShowAsync();
                    if (result == ContentDialogResult.Primary)
                    {
                        Page = 1;
                        SQLiteHistoryLogger.ClearHistory();
                        await LoadHistoryAsync();
                    }
                });

                ExportHistoryCommand = new RelayCommand(async _ =>
                {
                    SaveFileDialog saveFileDialog = new SaveFileDialog
                    {
                        Filter = "CSV (*.csv)|*.csv|All file (*.*)|*.*",
                        DefaultExt = ".csv",
                        FileName = $"exported_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.csv",
                        InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                    };

                    if (saveFileDialog.ShowDialog() == true)
                    {
                        try
                        {
                            await SQLiteHistoryLogger.ExportToCsv(saveFileDialog.FileName);
                            ShowSnackbarAction?.Invoke("Saved Success", $"File saved to: {saveFileDialog.FileName}", false);
                        }
                        catch (Exception ex)
                        {
                            ShowSnackbarAction?.Invoke("Save Failed", $"File save failed: {ex.Message}", true);
                        }
                    }
                });
                TranslationController.TranslationLogged += async () => await LoadHistoryAsync();
                _ = LoadHistoryAsync();
            }

            private async Task LoadHistoryAsync()
            {
                var data = await SQLiteHistoryLogger.LoadHistoryAsync(Page, MaxRow);
                var list = data.Item1;
                MaxPage = (data.Item2 > 0) ? data.Item2 : 1;

                if (Page > MaxPage)
                {
                    Page = MaxPage;
                    data = await SQLiteHistoryLogger.LoadHistoryAsync(Page, MaxRow);
                    list = data.Item1;
                }
                Application.Current.Dispatcher.Invoke(() =>
                {
                    HistoryEntries = new ObservableCollection<TranslationHistoryEntry>(list);
                });
            }

            private void UpdatePageNumberText()
            {
                PageNumberText = $"{Page}/{MaxPage}";
            }
        }

        #endregion
    }
}
