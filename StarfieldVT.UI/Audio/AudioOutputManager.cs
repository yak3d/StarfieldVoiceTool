using NAudio.Vorbis;
using NAudio.Wave;

namespace StarfieldVT.UI.Audio;

public class AudioOutputManager
{
    private WaveOutEvent? _outputDevice;
    private VorbisWaveReader? _soundBeingPlayed = null;

    private AudioOutputManager()
    {

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

        if (_outputDevice != null
            && _soundBeingPlayed != null
            && _outputDevice.PlaybackState != PlaybackState.Stopped)
        {
            _outputDevice.Dispose();
            _outputDevice = null;
            _soundBeingPlayed.Dispose();
            _soundBeingPlayed = null;
        }

        if (_outputDevice == null)
        {
            _outputDevice = new WaveOutEvent();
        }

        if (_soundBeingPlayed == null)
        {
            _soundBeingPlayed = new VorbisWaveReader(soundPath);
            _outputDevice.Init(_soundBeingPlayed);
        }

        _outputDevice.Play();
    }

    public void StopSound()
    {
        if (_outputDevice != null
            && _soundBeingPlayed != null
            && _outputDevice.PlaybackState != PlaybackState.Stopped)
        {
            _outputDevice.Stop();
            _outputDevice.Dispose();
            _outputDevice = null;

            _soundBeingPlayed.Dispose();
            _soundBeingPlayed = null;
        }
    }
}