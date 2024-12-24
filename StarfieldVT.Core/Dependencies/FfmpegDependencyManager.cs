using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;

using Noggog;

using Serilog;

namespace StarfieldVT.Core.Dependencies;

public class FfmpegDependencyManager
{
    private readonly string ffmpegDownloadUrl =
        "https://github.com/ffbinaries/ffbinaries-prebuilt/releases/download/v5.1/ffmpeg-5.1-win-64.zip";

    [DllImport("shlwapi.dll", CharSet = CharSet.Auto, SetLastError = false)]
    static extern bool PathFindOnPath([In, Out] StringBuilder pszFile, [In] string[] ppszOtherDirs);

    public bool FfmpegOnPath()
    {
        var sb = new StringBuilder("ffmpeg.exe", 260);
        if (PathFindOnPath(sb, null!))
        {
            var pathToFfmpeg = sb.ToString();

            Log.Information($"Path to ffmpeg found at {pathToFfmpeg}");

            return !pathToFfmpeg.IsNullOrEmpty();
        }

        return false;
    }

    public bool FfmpegInBaseDirectory() => File.Exists(Path.Join(AppContext.BaseDirectory, "ffmpeg.exe"));

    public async Task DownloadFfmpegIfNotExists()
    {
        Log.Information("Checking for ffmpeg.exe in PATH");
        if (!FfmpegOnPath() && !FfmpegInBaseDirectory())
        {
            Log.Information($"ffmpeg.exe not found in PATH or base directory, downloading from {ffmpegDownloadUrl}");
            using var httpClient = new HttpClient();
            var dlPath = AppContext.BaseDirectory;
            await using var stream = await httpClient.GetStreamAsync(ffmpegDownloadUrl);
            var ffmpegZipPath = Path.Combine(dlPath, "ffmpeg.zip");
            await using var fileWriter = File.OpenWrite(ffmpegZipPath);
            await stream.CopyToAsync(fileWriter);
            fileWriter.Close();
            Log.Information($"ffmpeg completed download, at path {ffmpegZipPath}");

            var ffmpegExePath = Path.Combine(dlPath, "ffmpeg.exe");
            Log.Information($"Unzipping ffmpeg.exe to {ffmpegExePath}");
            ZipFile.ExtractToDirectory(ffmpegZipPath, dlPath, true);
            Log.Information($"ffmpeg.exe should be found at {ffmpegExePath}");
        }
        else
        {
            Log.Information($"ffmpeg.exe already exists, skipping download");
        }
    }
}