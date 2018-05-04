using System;
using MumbleSharp;
using NAudio.Wave;

namespace MumbleClient
{
    public class MicrophoneRecorder
    {
        private readonly IMumbleProtocol _protocol;

        private bool _recording = true;

        public MicrophoneRecorder(IMumbleProtocol protocol)
        {
            _protocol = protocol;
            var sourceStream = new WaveInEvent
            {
                WaveFormat = new WaveFormat(Constants.SAMPLE_RATE, Constants.SAMPLE_BITS, Constants.CHANNELS)
            };
            sourceStream.DataAvailable += VoiceDataAvailable;

            sourceStream.StartRecording();
        }

        private void VoiceDataAvailable(object sender, WaveInEventArgs e)
        {
            if (!_recording)
                return;

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
                _protocol.LocalUser.Channel.SendVoice(new ArraySegment<byte>(e.Buffer, 0, e.BytesRecorded));
        }

        public void Record()
        {
            _recording = true;
        }

        public void Stop()
        {
            _recording = false;
            _protocol.LocalUser.Channel.SendVoiceStop();
        }
    }
}
