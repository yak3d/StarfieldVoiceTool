using System.IO;
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
            string treeAsJson = JsonSerializer.Serialize(tree);
            File.WriteAllText(this._cachePath, treeAsJson);
        }

        private bool cacheExists()
        {
            return File.Exists(this._cachePath);
        }

        public List<Master>? TryToLoadCache()
        {
            if (cacheExists())
            {
                Log.Information($"Loading cache from {_cachePath}");
                var cache = File.ReadAllText(this._cachePath);

                if (cache == null)
                {
                    return null;
                }

                return JsonSerializer.Deserialize<List<Master>>(cache);
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
