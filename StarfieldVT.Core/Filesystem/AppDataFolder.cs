using System.IO;

namespace StarfieldVT.Core.Filesystem
{
    public static class AppDataFolder
    {
        private static readonly string _appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        public static string GetAppDataFolder() => Path.Combine(_appDataPath, "StarfieldVT");
        public static string GetLogDir() => Path.Combine(GetAppDataFolder(), "logs");
    }
}
