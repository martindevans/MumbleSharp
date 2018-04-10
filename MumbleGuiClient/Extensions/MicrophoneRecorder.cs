using System;
using MumbleSharp;
using NAudio.Wave;

namespace MumbleGuiClient
{
    public class MicrophoneRecorder
    {
        private readonly IMumbleProtocol _protocol;

        public bool _recording = false;
        public double lastPingSendTime;
        WaveInEvent sourceStream;
        public static int SelectedDevice;
        private float _voiceDetectionVolume;
        private short _voiceDetectionSampleVolume;
        private short _noiseDetectionSampleVolume;
        public float VoiceDetectionVolume
        {
            get
            {
                return _voiceDetectionVolume;
            }
            set
            {
                _voiceDetectionVolume = value;
                _voiceDetectionSampleVolume = Convert.ToInt16(short.MaxValue * value);
                _noiseDetectionSampleVolume = Convert.ToInt16(short.MaxValue * value * 0.75);
            }
        }

        public MicrophoneRecorder(IMumbleProtocol protocol)
        {
            VoiceDetectionVolume = 0.5f;
            _protocol = protocol;
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
        private void VoiceDataAvailable(object sender, WaveInEventArgs e)
        {
            if (!_recording)
                return;

            var now = DateTime.Now;

            SoundType detectedSound = DetectSound(new WaveBuffer(e.Buffer), e.BytesRecorded, _voiceDetectionSampleVolume, _noiseDetectionSampleVolume);
            if (detectedSound != SoundType.NOTHING)
                _lastDetectedSoundTime = now;

            //adjust the detectedSound to tyke into account to hold times.
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
                return;
            else
            {
                //At the moment we're sending *from* the local user, this is kinda stupid.
                //What we really want is to send *to* other users, or to channels. Something like:
                //
                //    _connection.Users.First().SendVoiceWhisper(e.Buffer);
                //
                //    _connection.Channels.First().SendVoice(e.Buffer, shout: true);

                //if (_protocol.LocalUser != null)
                //    _protocol.LocalUser.SendVoice(new ArraySegment<byte>(e.Buffer, 0, e.BytesRecorded));

                //Send to the channel LocalUser is currently in
                if (_protocol.LocalUser != null && _protocol.LocalUser.Channel != null)
                {
                    //_protocol.Connection.SendControl<>
                    _protocol.LocalUser.Channel.SendVoice(new ArraySegment<byte>(e.Buffer, 0, e.BytesRecorded));
                }

                //if (DateTime.Now.TimeOfDay.TotalMilliseconds - lastPingSendTime > 1000 || DateTime.Now.TimeOfDay.TotalMilliseconds < lastPingSendTime)
                //{
                //    _protocol.Connection.SendVoice
                //}
            }
        }

        private static SoundType DetectSound(WaveBuffer buffer, int bytesRecorded, short minVoiceRecordSampleVolume, short minNoiseRecordSampleVolume)
        {
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

        public void Record()
        {
            _recording = true;

            if (sourceStream != null)
                sourceStream.Dispose();
            sourceStream = new WaveInEvent
            {
                WaveFormat = new WaveFormat(Constants.SAMPLE_RATE, Constants.SAMPLE_BITS, Constants.CHANNELS)
            };
            sourceStream.BufferMilliseconds = 10;
            sourceStream.DeviceNumber = SelectedDevice;
            sourceStream.NumberOfBuffers = 3;
            sourceStream.DataAvailable += VoiceDataAvailable;

            sourceStream.StartRecording();
        }

        public void Stop()
        {
            _recording = false;
            _protocol.LocalUser?.Channel.SendVoiceStop();

            sourceStream.StopRecording();
            sourceStream.Dispose();
            sourceStream = null;
        }
    }
}
