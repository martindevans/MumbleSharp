using NAudio.Wave;

namespace MumbleGuiClient
{
    public interface IVoiceDetector
    {
        bool VoiceDetected(WaveBuffer waveBuffer, int bytesRecorded);
    }
}