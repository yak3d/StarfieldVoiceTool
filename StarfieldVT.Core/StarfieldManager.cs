using System.Runtime.InteropServices;

using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Starfield;

namespace StarfieldVT.Core;

public class StarfieldManager
{
    public static string GetGamePath()
    {
        var settings = SettingsManager.Instance;
        if (settings.HasValidGamePath())
            return settings.StarfieldDataPath!;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var autoPath = GetGamePathFromRegistry();
            settings.StarfieldDataPath = autoPath;
            return autoPath;
        }

        throw new InvalidOperationException(
            "Starfield data path is not configured. Please set the game path in File > Set Game Path.");
    }

    internal static string GetGamePathFromRegistry()
    {
        using var env = GameEnvironment.Typical.Starfield(StarfieldRelease.Starfield);
        return env.DataFolderPath.Path;
    }
}
