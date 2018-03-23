using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;

namespace MumbleGuiClient
{
    public class SpeakerPlayback
    {
        private static int _selectedDevice = -1;
        public static int SelectedDevice {
            get { return _selectedDevice; }
            set {
                _selectedDevice = value;
                foreach(var player in _players)
                {
                    player.Value.ChangeDevice();
                }
            }
        }
        private static readonly Dictionary<uint, Player> _players = new Dictionary<uint, Player>();

        public static void AddPlayer(uint id, NAudio.Wave.IWaveProvider provider)
        {
            if (!_players.ContainsKey(id))
                _players.Add(id, new Player(provider));
        }

        public static void RemovePlayer(uint id)
        {
            if(_players.ContainsKey(id))
                _players[id].Dispose();
            _players.Remove(id);
        }

        public static void Play(uint id)
        {
            if (_players.ContainsKey(id))
                _players[id].Play();
        }

        public static void Stop(uint id)
        {
            if (_players.ContainsKey(id))
                _players[id].Stop();
        }

        public static void Pause(uint id)
        {
            if (_players.ContainsKey(id))
                _players[id].Pause();
        }

        private class Player : IDisposable
        {
            private NAudio.Wave.WaveOut _waveOut;
            private readonly NAudio.Wave.IWaveProvider _provider;

            public Player(NAudio.Wave.IWaveProvider provider)
            {
                _provider = provider;
                _waveOut = new NAudio.Wave.WaveOut();
                _waveOut.DeviceNumber = SelectedDevice;
                _waveOut.Init(_provider);
                Play();
            }

            public void Dispose()
            {
                Stop();
                _waveOut.Dispose();
            }

            public void Stop()
            {
                if (_waveOut.PlaybackState != PlaybackState.Stopped)
                    _waveOut.Stop();
            }

            public void Play()
            {
                if (_waveOut.PlaybackState != PlaybackState.Playing)
                    _waveOut.Play();
            }

            public void Pause()
            {
                if(_waveOut.PlaybackState == PlaybackState.Playing)
                    _waveOut.Pause();
            }

            public void ChangeDevice()
            {
                var state = _waveOut.PlaybackState;
                var latency = _waveOut.DesiredLatency;
                var numberOfBuffers = _waveOut.NumberOfBuffers;
                var waveFormat = _waveOut.OutputWaveFormat;
                var volume = _waveOut.Volume;

                _waveOut.Dispose();
                _waveOut = new WaveOut();
                _waveOut.DeviceNumber = SelectedDevice;
                _waveOut.Init(_provider);

                switch (state)
                {
                    case PlaybackState.Paused: Pause(); break;
                    case PlaybackState.Playing: Play(); break;
                }
            }

        }


    }
}
