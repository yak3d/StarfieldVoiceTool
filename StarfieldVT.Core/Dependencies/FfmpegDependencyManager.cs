using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.InteropServices;

using Serilog;

namespace StarfieldVT.Core.Dependencies;

public class FfmpegDependencyManager
{
    private static string FfmpegBinaryName =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "ffmpeg.exe" : "ffmpeg";

    private static string FfmpegDownloadUrl =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "https://github.com/ffbinaries/ffbinaries-prebuilt/releases/download/v5.1/ffmpeg-5.1-win-64.zip"
            : "https://github.com/ffbinaries/ffbinaries-prebuilt/releases/download/v5.1/ffmpeg-5.1-linux-64.zip";

    public bool FfmpegOnPath()
    {
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathEnv))
            return false;

        var paths = pathEnv.Split(Path.PathSeparator);
        foreach (var dir in paths)
        {
            var fullPath = Path.Combine(dir, FfmpegBinaryName);
            if (File.Exists(fullPath))
            {
                Log.Information("Path to ffmpeg found at {Path}", fullPath);
                return true;
            }
        }

        return false;
    }

    public bool FfmpegInBaseDirectory() => File.Exists(Path.Join(AppContext.BaseDirectory, FfmpegBinaryName));

    public async Task DownloadFfmpegIfNotExists()
    {
        Log.Information("Checking for {FfmpegBinary} in PATH", FfmpegBinaryName);
        if (!FfmpegOnPath() && !FfmpegInBaseDirectory())
        {
            Log.Information("{FfmpegBinary} not found in PATH or base directory, downloading from {Url}",
                FfmpegBinaryName, FfmpegDownloadUrl);

            using var httpClient = new HttpClient();
            var dlPath = AppContext.BaseDirectory;
            await using var stream = await httpClient.GetStreamAsync(FfmpegDownloadUrl);
            var ffmpegZipPath = Path.Combine(dlPath, "ffmpeg.zip");
            await using var fileWriter = File.OpenWrite(ffmpegZipPath);
            await stream.CopyToAsync(fileWriter);
            fileWriter.Close();
            Log.Information("ffmpeg completed download, at path {ZipPath}", ffmpegZipPath);

            var ffmpegPath = Path.Combine(dlPath, FfmpegBinaryName);
            Log.Information("Unzipping ffmpeg to {Path}", ffmpegPath);
            ZipFile.ExtractToDirectory(ffmpegZipPath, dlPath, true);

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Log.Information("Setting executable permission on {Path}", ffmpegPath);
                Process.Start("chmod", $"+x \"{ffmpegPath}\"")?.WaitForExit();
            }

            Log.Information("ffmpeg should be found at {Path}", ffmpegPath);
        }
        else
        {
            Log.Information("ffmpeg already exists, skipping download");
        }
    }
}
