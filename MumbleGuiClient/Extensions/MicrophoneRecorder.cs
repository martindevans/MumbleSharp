﻿using System;
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

        public MicrophoneRecorder(IMumbleProtocol protocol)
        {
            _protocol = protocol;
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
            {
                //_protocol.Connection.SendControl<>
                _protocol.LocalUser.Channel.SendVoice(new ArraySegment<byte>(e.Buffer, 0, e.BytesRecorded));
            }

            //if (DateTime.Now.TimeOfDay.TotalMilliseconds - lastPingSendTime > 1000 || DateTime.Now.TimeOfDay.TotalMilliseconds < lastPingSendTime)
            //{
            //    _protocol.Connection.SendVoice
            //}
        }

        public void Record()
        {
            _recording = true;

            if (sourceStream != null)
                sourceStream.Dispose();
            sourceStream = new WaveInEvent
            {
                WaveFormat = new WaveFormat(48000, 16, 1)
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
