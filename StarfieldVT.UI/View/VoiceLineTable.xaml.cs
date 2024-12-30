using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using Mutagen.Bethesda;
using Mutagen.Bethesda.Archives;
using Mutagen.Bethesda.Installs;

using Noggog;

using Serilog;

using StarfieldVT.Core;

using StarfieldVT.UI.Audio;
using StarfieldVT.UI.ViewModel;

using Path = System.IO.Path;

namespace StarfieldVT.UI.View;

public partial class VoiceLineTable : UserControl
{
    private readonly VoiceManager voiceManager = VoiceManager.Instance;

    public VoiceLineTable()
    {
        InitializeComponent();

        // DataContext = new VoiceLineTableViewModel();
        DataContextChanged += (sender, args) =>
        {
            if (DataContext != null && DataContext is VoiceLineTableViewModel vm)
            {
                ViewModel.PropertyChanged += (sender, args) =>
                {
                    if (args.PropertyName == nameof(vm.VoiceLines))
                    {

                    }
                };
            }
        };
    }

    public VoiceLineTableViewModel ViewModel => (VoiceLineTableViewModel)DataContext;

    public DirectoryPath dataFolder = GameLocations.GetDataFolder(GameRelease.Starfield);
    //
    //
    // public static readonly DependencyProperty SelectedVoiceLineProperty = DependencyProperty.Register(
    //     nameof(SelectedVoiceLine),
    //     typeof(VoiceLine),
    //     typeof(UserControl),
    //     new FrameworkPropertyMetadata(null));
    //
    // public List<VoiceLine> VoiceLines
    // {
    //     get => (List<VoiceLine>) GetValue(VoiceLinesProperty);
    //     set
    //     {
    //         SetValue(VoiceLinesProperty, value);
    //     }
    // }
    //
    // public static readonly DependencyProperty VoiceLinesProperty = DependencyProperty.Register(
    //     nameof(VoiceLines),
    //     typeof(List<VoiceLine>),
    //     typeof(VoiceLineTable),
    //     new FrameworkPropertyMetadata(VoiceLinesChanged)
    //     {
    //         BindsTwoWayByDefault = true
    //     }
    // );
    //
    // private static void VoiceLinesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    // {
    //         if (d is VoiceLineTable voiceLineTable)
    //         {
    //             voiceLineTable.VoiceLines = e.NewValue as List<VoiceLine> ?? new List<VoiceLine>();
    //         }
    // }

    private IArchiveFile? getArchiveFileFromSelectedVoiceLine()
    {
        if (ViewModel.SelectedVoiceLine != null)
        {
            var archivePrefix = this.ViewModel.SelectedVoiceLine.ModName.Replace(".esm", "").Replace(".esp", "");
            var applicableArchives = Archive.GetApplicableArchivePaths(GameRelease.Starfield, this.dataFolder).Where(archive =>
                archive.NameWithoutExtension.StartsWith("Starfield - Voices")
                || archive.NameWithoutExtension.Contains(archivePrefix)
                && archive.NameWithoutExtension.ToLower().Contains("voices")
                && archive.NameWithoutExtension.ToLower().Contains("en"));

            foreach (var archive in applicableArchives)
            {
                var archiveReader = Archive.CreateReader(GameRelease.Starfield, archive);
                var voiceFile = archiveReader.Files.FirstOrDefault(file => file.Path.Equals(this.ViewModel.SelectedVoiceLine.Filename, StringComparison.InvariantCultureIgnoreCase));

                if (voiceFile != null)
                {
                    return voiceFile;
                }
            }
        }
        return null;
    }

    private void DialogueGrid_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ViewModel.SelectedVoiceLine != null)
        {
            var voiceFile = getArchiveFileFromSelectedVoiceLine();
            if (voiceFile != null)
            {
                voiceManager.PlayVoiceLine(voiceFile);
            }
            else
            {
                Log.Warning($"Attempted to play selected voice line {ViewModel.SelectedVoiceLine.Filename}, but unable to find the file in the archive for {ViewModel.SelectedVoiceLine.ModName}");
            }
        }
    }

    private void ExportBtn_OnClick(object sender, RoutedEventArgs e)
    {
        var voiceFile = getArchiveFileFromSelectedVoiceLine();
        if (voiceFile != null)
        {
            var wemPath = AudioConverter.Wem2Ogg(voiceFile.GetBytes());
            AudioConverter.Ogg2Wav(wemPath, Path.Join(StarfieldManager.GetGamePath(), Path.ChangeExtension(voiceFile.Path, "wav")));
        }
    }
}