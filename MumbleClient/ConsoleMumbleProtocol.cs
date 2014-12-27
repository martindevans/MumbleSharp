using System;
using System.Linq;
using MumbleSharp;
using MumbleSharp.Model;
using NAudio.Wave;

namespace MumbleClient
{
    /// <summary>
    /// A test mumble protocol. Currently just prints the name of whoever is speaking, as well as printing messages it receives
    /// </summary>
    public class ConsoleMumbleProtocol
        : BasicMumbleProtocol
    {
        readonly AudioPlayer _player = new AudioPlayer();

        public override void Voice(byte[] pcm, uint userId, long sequence)
        {
            User user = Users.FirstOrDefault(u => u.Id == userId);
            if (user != null)
                Console.WriteLine(user.Name + " is speaking. Seq" + sequence);

            _player.Add(pcm);
        }

        protected override void ChannelMessageReceived(ChannelMessage message)
        {
            if (message.Channel.Equals(LocalUser.Channel))
                Console.WriteLine(string.Format("{0} (channel message): {1}", message.Sender.Name, message.Text));

            base.ChannelMessageReceived(message);
        }

        protected override void PersonalMessageReceived(PersonalMessage message)
        {
            Console.WriteLine(string.Format("{0} (personal message): {1}", message.Sender.Name, message.Text));

            base.PersonalMessageReceived(message);
        }

        private class AudioPlayer
        {
            private readonly WaveOut _playbackDevice = new WaveOut();
            private readonly BufferedWaveProvider _source = new BufferedWaveProvider(new WaveFormat(48000, 16, 1));

            public AudioPlayer()
            {
                _playbackDevice.Init(_source);
                _playbackDevice.Play();
            }

            public void Add(byte[] pcm)
            {
                _source.AddSamples(pcm, 0, pcm.Length);
            }
        }
    }
}
