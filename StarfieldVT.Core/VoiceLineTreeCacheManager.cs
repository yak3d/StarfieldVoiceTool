using System.IO;
using System.Reflection;
using System.Text.Json;

using Serilog;

using StarfieldVT.Core.Filesystem;
using StarfieldVT.Core.Models;

namespace StarfieldVT.Core
{
    public class VoiceLineTreeCacheManager
    {
        private readonly string _appDataPath = AppDataFolder.GetAppDataFolder();
        private const string CacheFilename = "cache.json";
        private readonly string _cachePath;

        public VoiceLineTreeCacheManager()
        {
            this._cachePath = Path.Combine(_appDataPath, CacheFilename);
        }

        public void SaveCurrentTree(List<Master> tree)
        {
            Log.Information("Writing cache file to {cachePath}", this._cachePath);
            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;

            var cacheFile = new CacheFile { Masters = tree, Version = currentVersion ?? new Version() };
            string treeAsJson = JsonSerializer.Serialize(cacheFile);
            File.WriteAllText(this._cachePath, treeAsJson);
        }

        private bool cacheExists()
        {
            return File.Exists(this._cachePath);
        }

        private bool CacheIsUpToDate(CacheFile cacheFile)
        {
            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
            var isCacheOutOfDate = currentVersion > cacheFile.Version;

            if (isCacheOutOfDate)
            {
                Log.Information("Cache file is from an older version, invalidating it and rebuilding the voice line tree.");
            }

            return isCacheOutOfDate;
        }

        public List<Master>? TryToLoadCache()
        {
            if (!cacheExists()) return null;
            Log.Information($"Loading cache from {_cachePath}");
            var cacheText = File.ReadAllText(this._cachePath);

            try
            {
                var cache = JsonSerializer.Deserialize<CacheFile>(cacheText);
                if (cache == null || CacheIsUpToDate(cache))
                {
                    return null;
                }

                return cache.Masters;
            }
            catch (Exception ex)
            {
                Log.Warning($"Failed to load cache from {_cachePath} and will invalidate + reload instead: {ex.Message}");
            }

            return null;
        }

        public void BustCache()
        {
            Log.Information($"Deleting cache file at {_cachePath}");

            if (File.Exists(_cachePath))
            {
                File.Delete(_cachePath);
            }
        }
    }
}
