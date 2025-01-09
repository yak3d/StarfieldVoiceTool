using NAudio.Wave;
using Serilog;

namespace StarfieldVT.UI.Audio;

public class AudioOutputManager
{
    private WaveOutEvent? _outputDevice;
    private WaveFileReader? _soundBeingPlayed = null;

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
            _soundBeingPlayed = new WaveFileReader(soundPath);
            _outputDevice.Init(_soundBeingPlayed);
        }

        _outputDevice.PlaybackStopped += OutputDeviceOnPlaybackStopped;
        _outputDevice.Play();
    }

    private void OutputDeviceOnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        Log.Debug("disposing audio device");
        _outputDevice?.Dispose();
        _outputDevice = null;

        _soundBeingPlayed?.Dispose();
        _soundBeingPlayed = null;
    }

    public void StopSound()
    {
        if (_outputDevice != null
            && _soundBeingPlayed != null
            && _outputDevice.PlaybackState != PlaybackState.Stopped)
        {
            _outputDevice.Stop();
        }
    }
}