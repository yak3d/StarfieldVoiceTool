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
    public class MainWindowModel : INotifyPropertyChanged
    {
        // private ObservableCollection<VoiceLine> _voiceLines2 = new ObservableCollection<VoiceLine>();
        // public ObservableCollection<VoiceLine> VoiceLines2
        // {
        //     get => _voiceLines2;
        //     set
        //     {
        //         _voiceLines2 = value;
        //         OnPropertyChanged();
        //     }
        // }
        public VoiceLineTableViewModel VoiceLineTableViewModel { get; set; }

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

        public VoiceLine SelectedVoiceLine { get; set; }

        public static MainWindowModel GetMainWindowModel()
        {
            var mainWindowModel = new MainWindowModel()
            {
                VoiceLineTableViewModel = new VoiceLineTableViewModel(),
                SearchBarText = "",
                SelectedVoiceLine = null
            };

            return mainWindowModel;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
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
        public ObservableCollection<Master> Masters { get; set; }

        readonly DialogueTreeBuilder dialogueTreeBuilder = new DialogueTreeBuilder();
        readonly List<Master> tree;

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
            var filteredVoiceTypes = tree.Where(master => master.VoiceTypes.Any(vt => vt.EditorId.StartsWith(newSearchQuery))).Select(master => new Master(master.Filename, master.VoiceTypes.Where(vt => vt.EditorId.StartsWith(newSearchQuery)).ToList()));
            this.Masters.Clear();
            this.Masters.Add(filteredVoiceTypes);

            e.Handled = true;
        }

        private void VoiceTypeTree_OnVoiceTypeSelected(object sender, VoiceTypeSelectedArgs<VoiceType> e)
        {
            _mainWindowModel.VoiceLineTableViewModel.VoiceLines = null;
            _mainWindowModel.VoiceLineTableViewModel.VoiceLines =
                new ObservableCollection<VoiceLine>(e.NewValue.VoiceLines);
        }

        private void VoiceTypeTree_OnProgressChanged(object? sender, VoiceTypeTree.VoiceTypeTreeProgressChangedEventHandler e)
        {
            if (e.Progress.num < 0)
            {
                TreeBuilderProgressBar.Visibility = Visibility.Collapsed;
                ProgressText.Content = "Loaded";
                return;
            }
            TreeBuilderProgressBar.Value = e.Progress.num;
            ProgressText.Content = $"Parsing quest {e.Progress.esmName}";

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

        private void VoiceTypeTree_OnMasterSelected(object sender, MasterSelectedArgs<Master> e)
        {
            var masterVoiceLines = e.NewValue.VoiceTypes.Select(vt => vt.VoiceLines)
                .Aggregate((currentLines, newLines) => [.. currentLines, .. newLines]);
            _mainWindowModel.VoiceLineTableViewModel.VoiceLines = null;
            _mainWindowModel.VoiceLineTableViewModel.VoiceLines =
                new ObservableCollection<VoiceLine>(masterVoiceLines);
        }

        private void MenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }
    }
}