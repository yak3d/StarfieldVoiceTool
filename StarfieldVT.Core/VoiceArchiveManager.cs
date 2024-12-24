using System.IO;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Archives;
using Mutagen.Bethesda.Installs;
using Noggog;
using Serilog;

namespace StarfieldVT.Core;

public class WemKey(string masterName, string wemName)
{
    private string MasterName { get; } = masterName.ToLower();
    private string WemName { get; } = wemName.ToLower();

    public override bool Equals(object? obj)
    {
        if (obj is not WemKey other)
        {
            return false;
        }

        return MasterName.Equals(other.MasterName, StringComparison.InvariantCultureIgnoreCase) && WemName.Equals(other.WemName, StringComparison.InvariantCultureIgnoreCase);
    }

    public override int GetHashCode()
    {
        return MasterName.GetHashCode() + WemName.GetHashCode();
    }
}

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

    public Dictionary<WemKey, List<WemFileReference>> BuildWemMap(string modFileName)
    {
        Log.Information("Building wem Map for {0}", modFileName);
        var archives = GetApplicableVoiceArchives(modFileName);
        var wemDict = new Dictionary<WemKey, List<WemFileReference>>();

        archives.ForEach(archive =>
        {
            Log.Debug("Reading archive file {0}", archive.Path);
            var archiveReader = Archive.CreateReader(GameRelease.Starfield, archive);
            archiveReader.Files.Where(file => file.Path.EndsWith(".wem")).ForEach(file =>
            {
                // wem key needs to contain the esm name to properly namespace
                var esmName = file.Path.Split("/").First(
                    pathPart => pathPart.EndsWith(".esm", StringComparison.InvariantCultureIgnoreCase)
                                || pathPart.EndsWith(".esp", StringComparison.InvariantCultureIgnoreCase
                                ));
                var wemFilename = Path.GetFileName(file.Path);
                var wemKey = new WemKey(esmName, wemFilename);
                var wemFileReference = new WemFileReference(file.Path);

                if (wemDict.ContainsKey(wemKey))
                {
                    wemDict[wemKey].Add(wemFileReference);
                }
                else
                {
                    wemDict.Add(wemKey, [wemFileReference]);
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