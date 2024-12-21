using System.IO;

using BnkExtractor;

using FFMpegCore;

using Serilog;

namespace StarfieldVT.UI.Audio;

public class AudioConverter
{
    public static string Wem2Ogg(byte[] wemBytes)
    {
        var tempWemPath = Path.GetTempFileName();
        using (FileStream fs = new FileStream(tempWemPath, FileMode.Create, FileAccess.Write))
        {
            fs.Write(wemBytes);
            fs.Close();
        }

        Extractor.ConvertWem(tempWemPath);
        return Path.ChangeExtension(tempWemPath, "ogg");
    }

    public static void Ogg2Wav(string oggPath, string wavPath)
    {
        try
        {
            Log.Information("Converting the ogg file to a wav");
            var directoryName = Path.GetDirectoryName(wavPath);
            if (directoryName != null && !Path.Exists(directoryName))
            {
                Log.Information($"Directory {directoryName} does not exist, creating it.");
                Directory.CreateDirectory(directoryName);
            }
            Log.Information($"Writing converted wem file to wav file at {wavPath.Replace("/", "\\")}");
            FFMpegArguments
                .FromFileInput(oggPath)
                .OutputToFile(wavPath)
                .ProcessSynchronously();
        }
        catch (Exception e)
        {
            Log.Information("There was an error converting the ogg file to wav", e);
            throw;
        }
    }
}