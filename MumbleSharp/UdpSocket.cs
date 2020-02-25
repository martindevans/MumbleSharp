using MumbleProto;
using MumbleSharp.Audio;
using MumbleSharp.Audio.Codecs;
using MumbleSharp.Packets;
using ProtoBuf;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace MumbleSharp
{
    internal class UdpSocket
    {
        readonly UdpClient _client;
        readonly IPEndPoint _host;
        readonly IMumbleProtocol _protocol;
        readonly MumbleConnection _connection;

        public bool IsConnected { get; private set; }

        public UdpSocket(IPEndPoint host, IMumbleProtocol protocol, MumbleConnection connection)
        {
            _host = host;
            _protocol = protocol;
            _connection = connection;
            _client = new UdpClient();
        }

        public void Connect()
        {
            _client.Connect(_host);
            IsConnected = true;
        }

        public void Close()
        {
            IsConnected = false;
            _client.Close();
        }

        public void SendPing()
        {
            long timestamp = DateTime.UtcNow.Ticks;

            byte[] buffer = new byte[9];
            buffer[0] = 1 << 5;
            buffer[1] = (byte)((timestamp >> 56) & 0xFF);
            buffer[2] = (byte)((timestamp >> 48) & 0xFF);
            buffer[3] = (byte)((timestamp >> 40) & 0xFF);
            buffer[4] = (byte)((timestamp >> 32) & 0xFF);
            buffer[5] = (byte)((timestamp >> 24) & 0xFF);
            buffer[6] = (byte)((timestamp >> 16) & 0xFF);
            buffer[7] = (byte)((timestamp >> 8) & 0xFF);
            buffer[8] = (byte)((timestamp) & 0xFF);

            _client.Send(buffer, buffer.Length);
        }

        public bool Process()
        {
            if (_client.Client == null
                || _client.Available == 0)
                return false;

            IPEndPoint sender = _host;
            byte[] data = _client.Receive(ref sender);

            _connection.ReceivedEncryptedUdp(data);

            return true;
        }
    }
}
