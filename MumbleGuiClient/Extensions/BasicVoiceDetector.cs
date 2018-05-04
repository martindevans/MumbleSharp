using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MumbleGuiClient
{
    public class BasicVoiceDetector : IVoiceDetector
    {
        public short VoiceDetectionSampleVolume { get; set; }
        public short NoiseDetectionSampleVolume { get; set; }

        public BasicVoiceDetector()
        {
            VoiceDetectionSampleVolume = Convert.ToInt16(short.MaxValue * 0.5f);
            NoiseDetectionSampleVolume = Convert.ToInt16(short.MaxValue * 0.25f);
        }

        private enum SoundType
        {
            NOTHING,
            NOISE,
            VOICE
        }

        private DateTime _lastDetectedSoundTime = DateTime.MinValue;
        private TimeSpan _minVoiceHoldTime = TimeSpan.FromMilliseconds(1000);
        private TimeSpan _minNoiseHoldTime = TimeSpan.FromMilliseconds(250);
        private SoundType _lastDetectedSound = SoundType.NOTHING;

        public bool VoiceDetected(WaveBuffer waveBuffer, int bytesRecorded)
        {
            var now = DateTime.Now;

            SoundType detectedSound = DetectSound(waveBuffer, bytesRecorded, VoiceDetectionSampleVolume, NoiseDetectionSampleVolume);
            if (detectedSound != SoundType.NOTHING)
                _lastDetectedSoundTime = now;

            //adjust the detectedSound to take into account to hold times.
            if (_lastDetectedSound == SoundType.NOISE && (_lastDetectedSoundTime + _minNoiseHoldTime > now))
            {
                switch (detectedSound)
                {
                    case SoundType.NOTHING:
                    case SoundType.NOISE:
                        detectedSound = SoundType.NOISE;
                        break;
                    case SoundType.VOICE:
                        detectedSound = SoundType.VOICE;
                        break;
                }
            }
            else if (_lastDetectedSound == SoundType.VOICE && (_lastDetectedSoundTime + _minVoiceHoldTime > now))
            {
                detectedSound = SoundType.VOICE;
            }

            _lastDetectedSound = detectedSound;

            if (detectedSound == SoundType.NOTHING)
                return false;
            else
                return true;
        }

        private static SoundType DetectSound(WaveBuffer buffer, int bytesRecorded, short minVoiceRecordSampleVolume, short minNoiseRecordSampleVolume)
        {
            if (minVoiceRecordSampleVolume == 0)
                return SoundType.VOICE;
            if (minNoiseRecordSampleVolume == 0)
                return SoundType.NOISE;

            SoundType result = SoundType.NOTHING;

            //check if the volume peaks above the MinRecordVolume
            // interpret as 32 bit floating point audio
            for (int index = 0; index < bytesRecorded / 4; index++)
            {
                var sample = buffer.ShortBuffer[index];

                //Check voice volume threshold
                if (sample > minVoiceRecordSampleVolume || sample < -minVoiceRecordSampleVolume)
                {
                    result = SoundType.VOICE;
                    //skip testing the rest of the sample data as soon as voice volume threshold has been reached
                    break;
                }
                //Check noise volume threshold
                else if (sample > minNoiseRecordSampleVolume || sample < -minNoiseRecordSampleVolume)
                {
                    result = SoundType.NOISE;
                }
            }

            return result;
        }
    }
}
