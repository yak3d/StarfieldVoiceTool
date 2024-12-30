using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DynamicData;

using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Starfield;

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

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public required ObservableCollection<Master> Masters { get; set; }

        readonly List<Master> _tree = [];

        private readonly VoiceManager voiceManager = VoiceManager.Instance;
        private readonly VoiceLineTreeCacheManager cacheManager = new VoiceLineTreeCacheManager();
        private readonly MainWindowModel _mainWindowModel = MainWindowModel.GetMainWindowModel();

        public MainWindow()
        {
            InitializeComponent();
            SetLoadingState();
            DataContext = _mainWindowModel;
        }

        protected override void OnContentRendered(EventArgs e)
        {
            CheckAndInstallFfmpeg();
            base.OnContentRendered(e);
        }

        private void SetLoadingState()
        {
            var test = GameEnvironment.Typical.Starfield(StarfieldRelease.Starfield).LoadOrder.PriorityOrder.ToList();

            TreeBuilderProgressBar.IsEnabled = true;
            TreeBuilderProgressBar.Visibility = Visibility.Visible;
            TreeBuilderProgressBar.Maximum = test.Where(esm => esm.Enabled && esm.ExistsOnDisk).Quest().WinningOverrides().Count();
        }

        private async void CheckAndInstallFfmpeg()
        {
            var ffmpegInterstitial = new FfmpegInterstitial();
            var ffmpegDepMgr = new FfmpegDependencyManager();

            ffmpegInterstitial.Owner = this;
            this.IsEnabled = false;
            ffmpegInterstitial.Show();
            ffmpegInterstitial.Focus();
            await ffmpegDepMgr.DownloadFfmpegIfNotExists();
            ffmpegInterstitial.Close();
            this.IsEnabled = true;
        }

        private void dialogueGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
        }

        private void searchTreeView_TextChanged(object sender, TextChangedEventArgs e)
        {
            var newSearchQuery = ((TextBox)sender).Text;
            _mainWindowModel.SearchBarText = newSearchQuery;
            var filteredVoiceTypes = _tree.Where(master => master.VoiceTypes.Any(vt => vt.EditorId.StartsWith(newSearchQuery))).Select(master => new Master(master.Filename, master.VoiceTypes.Where(vt => vt.EditorId.StartsWith(newSearchQuery)).ToList()));
            this.Masters.Clear();
            this.Masters.Add(filteredVoiceTypes);

            e.Handled = true;
        }

        private void VoiceTypeTree_OnVoiceTypeSelected(object sender, VoiceTypeSelectedArgs<IVoiceTypeTreeItem> e)
        {
            if (e.NewValue is VoiceType)
            {
                var selectedVoiceType = (VoiceType)e.NewValue;
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
                TreeBuilderProgressBar.Visibility = Visibility.Collapsed;
                ProgressText.Content = "Loaded";
                return;
            }
            TreeBuilderProgressBar.Value = e.Progress.Num;
            ProgressText.Content = $"Parsing quest {e.Progress.EsmName}";

            if (!(TreeBuilderProgressBar.Value >= TreeBuilderProgressBar.Maximum)) return;
            TreeBuilderProgressBar.Visibility = Visibility.Collapsed;
            ProgressText.Content = "Loaded";
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void DeleteCache_Click(object sender, RoutedEventArgs e)
        {
            cacheManager.BustCache();
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
            System.Windows.Application.Current.Shutdown();
        }
    }
}