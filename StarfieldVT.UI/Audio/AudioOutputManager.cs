using LibVLCSharp.Shared;

using Serilog;

namespace StarfieldVT.UI.Audio;

public class AudioOutputManager
{
    private readonly LibVLC _libVlc;
    private MediaPlayer? _mediaPlayer;

    private AudioOutputManager()
    {
        LibVLCSharp.Shared.Core.Initialize();
        _libVlc = new LibVLC("--no-video");
    }

    private static AudioOutputManager? _instance = null;

    public static AudioOutputManager Instance
    {
        get
        {
            if (_instance != null) return _instance;

            _instance = new AudioOutputManager();
            return _instance;
        }
    }

    public void PlaySound(string soundPath)
    {
        if (_mediaPlayer != null)
        {
            _mediaPlayer.Stop();
            _mediaPlayer.Dispose();
            _mediaPlayer = null;
        }

        _mediaPlayer = new MediaPlayer(_libVlc);

        using var media = new Media(_libVlc, soundPath, FromType.FromPath);
        _mediaPlayer.EndReached += (_, _) =>
        {
            Log.Debug("Playback ended, disposing media player");
            // Must be dispatched - cannot dispose from event handler thread
            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                _mediaPlayer?.Dispose();
                _mediaPlayer = null;
            });
        };

        _mediaPlayer.Play(media);
    }

    public void StopSound()
    {
        if (_mediaPlayer is { IsPlaying: true })
        {
            _mediaPlayer.Stop();
        }
    }
}
