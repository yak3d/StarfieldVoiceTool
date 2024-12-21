using System.IO;

using Mutagen.Bethesda.Archives;

using NAudio.Vorbis;
using NAudio.Wave;

using Serilog;

namespace StarfieldVT.UI.Audio
{
    public class VoiceManager
    {
        private VoiceManager() { }
        private static readonly Lazy<VoiceManager> lazy = new Lazy<VoiceManager>(() => new VoiceManager());
        public static VoiceManager Instance
        {
            get
            {
                return lazy.Value;
            }
        }

        private readonly WaveOutEvent outputDevice = null;
        private readonly VorbisWaveReader soundBeingPlayed = null;

        public void PlayVoiceLine(IArchiveFile voiceFileOne)
        {
            try
            {
                Log.Information($"Playing voice file {voiceFileOne.Path}");

                var oggPath = AudioConverter.Wem2Ogg(voiceFileOne.GetBytes());
                AudioOutputManager.Instance.PlaySound(Path.ChangeExtension(oggPath, ".ogg"));
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Exception thrown when attempting to play soundfile at {voiceFileOne.Path}");
            }
        }
    }
}
