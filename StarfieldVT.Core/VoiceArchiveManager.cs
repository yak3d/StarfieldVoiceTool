using System.IO;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Archives;
using Mutagen.Bethesda.Installs;
using Noggog;
using Serilog;

namespace StarfieldVT.Core;

public class VoiceArchiveManager
{
    private DirectoryPath _dataFolder = GameLocations.GetDataFolder(GameRelease.Starfield);
    public IEnumerable<FilePath> GetApplicableVoiceArchives(string modFileName)
    {
        var archivePrefix = modFileName.Replace(".esm", "").Replace(".esp", "");
        return Archive.GetApplicableArchivePaths(GameRelease.Starfield, _dataFolder).Where(archive =>
            archive.NameWithoutExtension.StartsWith("Starfield - Voices")
            || archive.NameWithoutExtension.Contains(archivePrefix)
            && archive.NameWithoutExtension.ToLower().Contains("voices")
            && archive.NameWithoutExtension.ToLower().Contains("en"));
    }
    public List<IArchiveFile> FindVoiceFileByName(string name, string modFileName)
    {
        var archives = GetApplicableVoiceArchives(modFileName);
        var voiceFiles = new List<IArchiveFile>();

        foreach (var archive in archives)
        {
            var archiveReader = Archive.CreateReader(GameRelease.Starfield, archive);
            var voiceFile = archiveReader.Files.FirstOrDefault(file => file.Path.EndsWith(name));

            if (voiceFile is not null)
            {
                voiceFiles.Add(voiceFile);
            }
        }

        return voiceFiles;
    }

    public Dictionary<string, List<WemFileReference>> BuildWemMap(string modFileName)
    {
        Log.Information("Building wem Map for {0}", modFileName);
        var archives = GetApplicableVoiceArchives(modFileName);
        var wemDict = new Dictionary<string, List<WemFileReference>>();

        archives.ForEach(archive =>
        {
            Log.Debug("Reading archive file {0}", archive.Path);
            var archiveReader = Archive.CreateReader(GameRelease.Starfield, archive);
            archiveReader.Files.Where(file => file.Path.EndsWith(".wem")).ForEach(file =>
            {
                var wemFilename = Path.GetFileName(file.Path);
                var wemFileReference = new WemFileReference(file.Path);

                if (wemDict.ContainsKey(wemFilename))
                {
                    wemDict[wemFilename].Add(wemFileReference);
                }
                else
                {
                    wemDict.Add(wemFilename, [wemFileReference]);
                }
            });
        });

        return wemDict;
    }
}

public class WemFileReference
{
    public string WemPath { get; set; }
    public string VoiceType { get; }

    public WemFileReference(string wemPath, string voiceType)
    {
        this.WemPath = wemPath;
        this.VoiceType = voiceType;
    }

    public WemFileReference(string wemPath)
    {
        this.WemPath = wemPath;
        this.VoiceType = Path.GetFileName(Path.GetDirectoryName(wemPath)) ?? throw new InvalidOperationException();
    }
}