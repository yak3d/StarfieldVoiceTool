using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Starfield;

namespace StarfieldVT.Core;

public class StarfieldManager
{
    public static string GetGamePath()
    {
        using var env = GameEnvironment.Typical.Starfield(StarfieldRelease.Starfield);
        return env.DataFolderPath.Path;
    }
}