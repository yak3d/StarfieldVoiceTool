using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Archives;

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

        DataContextChanged += (sender, args) =>
        {
        };
    }

    public VoiceLineTableViewModel ViewModel => (VoiceLineTableViewModel)DataContext!;

    private DirectoryPath DataFolder => new DirectoryPath(StarfieldManager.GetGamePath());

    private IArchiveFile? getArchiveFileFromSelectedVoiceLine()
    {
        if (ViewModel.SelectedVoiceLine != null)
        {
            var archivePrefix = this.ViewModel.SelectedVoiceLine.ModName.Replace(".esm", "").Replace(".esp", "");
            var applicableArchives = Archive.GetApplicableArchivePaths(GameRelease.Starfield, DataFolder).Where(archive =>
                archive.NameWithoutExtension.StartsWith("Starfield - Voices")
                || archive.NameWithoutExtension.Contains(archivePrefix)
                && archive.NameWithoutExtension.ToLower().Contains("voices")
                && archive.NameWithoutExtension.ToLower().Contains("en"));

            foreach (var archive in applicableArchives)
            {
                var archiveReader = Archive.CreateReader(GameRelease.Starfield, archive);
                var voiceFile = archiveReader.Files.FirstOrDefault(file =>
                    file.Path.Equals(this.ViewModel.SelectedVoiceLine.Filename,
                        StringComparison.InvariantCultureIgnoreCase));

                if (voiceFile != null)
                {
                    return voiceFile;
                }
            }
        }
        return null;
    }

    private void DialogueGrid_OnDoubleTapped(object? sender, TappedEventArgs e)
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
                Log.Warning(
                    "Attempted to play selected voice line {Filename}, but unable to find the file in the archive for {ModName}",
                    ViewModel.SelectedVoiceLine.Filename, ViewModel.SelectedVoiceLine.ModName);
            }
        }
    }

    private void ExportBtn_OnClick(object sender, RoutedEventArgs e)
    {
        var voiceFile = getArchiveFileFromSelectedVoiceLine();
        if (voiceFile != null)
        {
            var wemPath = AudioConverter.Wem2Ogg(voiceFile.GetBytes());
            AudioConverter.Ogg2Wav(wemPath,
                Path.Join(StarfieldManager.GetGamePath(), Path.ChangeExtension(voiceFile.Path, "wav")));
        }
    }
}
