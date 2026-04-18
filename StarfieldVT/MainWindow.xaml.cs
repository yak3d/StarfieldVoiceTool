using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

using StarfieldVT.Core;
using StarfieldVT.Core.Dependencies;
using StarfieldVT.Core.Models;
using StarfieldVT.UI.Audio;
using StarfieldVT.UI.Events;
using StarfieldVT.UI.View;
using StarfieldVT.UI.ViewModel;

using VoiceType = StarfieldVT.Core.Models.VoiceType;

namespace StarfieldVT
{
    public sealed class MainWindowModel : INotifyPropertyChanged
    {
        public VoiceLineTableViewModel? VoiceLineTableViewModel { get; private init; }

        private string _searchBarText = "";
        public string SearchBarText
        {
            get => _searchBarText;
            set
            {
                _searchBarText = value;
                OnPropertyChanged();
            }
        }

        public static MainWindowModel GetMainWindowModel()
        {
            var mainWindowModel = new MainWindowModel
            {
                VoiceLineTableViewModel = new VoiceLineTableViewModel(),
                SearchBarText = "",
            };

            return mainWindowModel;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public partial class MainWindow : Window
    {
        public ObservableCollection<Master> Masters { get; set; } = [];

        readonly List<Master> _tree = [];

        private readonly VoiceManager voiceManager = VoiceManager.Instance;
        private readonly VoiceLineTreeCacheManager cacheManager = new VoiceLineTreeCacheManager();
        private readonly MainWindowModel _mainWindowModel = MainWindowModel.GetMainWindowModel();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = _mainWindowModel;
        }

        protected override async void OnOpened(EventArgs e)
        {
            base.OnOpened(e);
            await EnsureGamePathConfigured();
            await CheckAndInstallFfmpeg();
            SetLoadingState();
        }

        private void SetLoadingState()
        {
            TreeBuilderProgressBar.IsEnabled = true;
            TreeBuilderProgressBar.IsVisible = true;
        }

        private async Task CheckAndInstallFfmpeg()
        {
            var ffmpegInterstitial = new FfmpegInterstitial();
            var ffmpegDepMgr = new FfmpegDependencyManager();

            ffmpegInterstitial.Show();
            this.IsEnabled = false;
            await ffmpegDepMgr.DownloadFfmpegIfNotExists();
            ffmpegInterstitial.Close();
            this.IsEnabled = true;
        }

        private async Task EnsureGamePathConfigured()
        {
            var settings = SettingsManager.Instance;

            if (settings.HasValidGamePath())
                return;

            var autoPath = settings.TryAutoDetectGamePath();
            if (autoPath != null)
            {
                settings.StarfieldDataPath = autoPath;
                return;
            }

            await PromptForGamePath();
        }

        private async Task PromptForGamePath()
        {
            var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select Starfield Data Folder",
                AllowMultiple = false
            });

            if (folders.Count > 0)
            {
                var path = folders[0].Path.LocalPath;
                SettingsManager.Instance.StarfieldDataPath = path;
            }
        }

        private void VoiceTypeTree_OnVoiceTypeSelected(object sender, VoiceTypeSelectedArgs<IVoiceTypeTreeItem> e)
        {
            if (e.NewValue is VoiceType selectedVoiceType)
            {
                if (_mainWindowModel.VoiceLineTableViewModel == null) return;
                _mainWindowModel.VoiceLineTableViewModel.VoiceLines = null;
                _mainWindowModel.VoiceLineTableViewModel.VoiceLines =
                    new ObservableCollection<VoiceLine>(selectedVoiceType.VoiceLines);
                _mainWindowModel.VoiceLineTableViewModel.SelectedVoiceType = selectedVoiceType.EditorId;
                _mainWindowModel.VoiceLineTableViewModel.SelectedMaster = selectedVoiceType.FromMaster;
            }
        }

        private void VoiceTypeTree_OnProgressChanged(object? sender, VoiceTypeTree.VoiceTypeTreeProgressChangedEventHandler e)
        {
            if (e.Progress.Num < 0)
            {
                TreeBuilderProgressBar.IsVisible = false;
                ProgressText.Text = "Loaded";
                return;
            }
            TreeBuilderProgressBar.Value = e.Progress.Num;
            ProgressText.Text = $"Parsing quest {e.Progress.EsmName}";

            if (!(TreeBuilderProgressBar.Value >= TreeBuilderProgressBar.Maximum)) return;
            TreeBuilderProgressBar.IsVisible = false;
            ProgressText.Text = "Loaded";
        }

        private void DeleteCache_Click(object sender, RoutedEventArgs e)
        {
            LuceneManager.Instance().DeleteIndex();
            cacheManager.BustCache();
            VoiceTypeTree.Reload();
        }

        private void StopButton_OnClick(object sender, RoutedEventArgs e)
        {
            AudioOutputManager.Instance.StopSound();
        }

        private void VoiceTypeTree_OnMasterSelected(object sender, MasterSelectedArgs<IVoiceTypeTreeItem> masterSelectedArgs)
        {
            if (masterSelectedArgs.NewValue is Master master)
            {
                var masterVoiceLines = master.VoiceTypes.Select(vt => vt.VoiceLines)
                    .Aggregate((currentLines, newLines) => [.. currentLines, .. newLines]);
                if (_mainWindowModel.VoiceLineTableViewModel == null) return;
                _mainWindowModel.VoiceLineTableViewModel.VoiceLines = null;
                _mainWindowModel.VoiceLineTableViewModel.VoiceLines =
                    new ObservableCollection<VoiceLine>(masterVoiceLines);
                _mainWindowModel.VoiceLineTableViewModel.SelectedMaster = master.Filename;
                _mainWindowModel.VoiceLineTableViewModel.SelectedVoiceType = null;
            }
        }

        private void MenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (Avalonia.Application.Current?.ApplicationLifetime is
                Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown();
            }
        }

        private async void SetGamePath_Click(object sender, RoutedEventArgs e)
        {
            await PromptForGamePath();
        }
    }
}
