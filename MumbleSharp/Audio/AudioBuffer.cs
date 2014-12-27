using MumbleSharp.Audio.Codecs;
using NAudio.Wave;
using System;
using System.Collections.Generic;

namespace MumbleSharp.Audio
{
    public class AudioBuffer
        : IWaveProvider
    {
        private static readonly WaveFormat _format = new WaveFormat(48000, 16, 1);
        public WaveFormat WaveFormat
        {
            get
            {
                return _format;
            }
        }

        private int _decodedOffset;
        private int _decodedCount;
        private readonly byte[] _decodedBuffer = new byte[16384];

        private long _nextSequenceToDecode;
        private readonly List<BufferPacket> _encodedBuffer = new List<BufferPacket>(); 

        private IVoiceCodec _codec;

        public int Read(byte[] buffer, int offset, int count)
        {
            int readCount = 0;
            while (readCount < count)
            {
                readCount += ReadFromBuffer(buffer, offset + readCount, count - readCount);

                //Try to decode some more data into the buffer
                if (!FillBuffer())
                    break;
            }

            if (readCount == 0)
            {
                //Return silence
                Array.Clear(buffer, 0, buffer.Length);
                return count;
            }

            return readCount;
        }

        public void AddEncodedPacket(long sequence, byte[] data, IVoiceCodec codec)
        {
            if (_codec == null)
                _codec = codec;
            else if (_codec != null && _codec != codec)
                ChangeCodec(codec);

            //If the next seq we expect to decode comes after this packet we've already missed our opportunity!
            if (_nextSequenceToDecode > sequence)
                return;

            _encodedBuffer.Add(new BufferPacket {
                Data = data,
                Sequence = sequence
            });
        }

        private void ChangeCodec(IVoiceCodec codec)
        {
            //Decode all buffered packets using current codec
            while (_encodedBuffer.Count > 0)
                FillBuffer();

            _codec = codec;
        }

        private BufferPacket? GetNextEncodedData()
        {
            if (_encodedBuffer.Count == 0)
                return null;

            int minIndex = 0;
            for (int i = 1; i < _encodedBuffer.Count; i++)
                minIndex = _encodedBuffer[minIndex].Sequence < _encodedBuffer[i].Sequence ? minIndex : i;

            var packet = _encodedBuffer[minIndex];
            _encodedBuffer.RemoveAt(minIndex);

            return packet;
        }

        private int ReadFromBuffer(byte[] dst, int offset, int count)
        {
            int readCount = Math.Min(count, _decodedCount);
            Array.Copy(_decodedBuffer, _decodedOffset, dst, offset, readCount);
            _decodedCount -= readCount;
            _decodedOffset += readCount;

            //When the buffer is emptied, put the start offset back to index 0
            if (_decodedCount == 0)
                _decodedOffset = 0;

            return readCount;
        }

        private bool FillBuffer()
        {
            var packet = GetNextEncodedData();
            if (!packet.HasValue)
                return false;

            ////todo: _nextSequenceToDecode calculation is wrong, which causes this to happen for almost every packet!
            ////Decode a null to indicate a dropped packet
            //if (packet.Value.Sequence != _nextSequenceToDecode)
            //    _codec.Decode(null);

            var d = _codec.Decode(packet.Value.Data);
            _nextSequenceToDecode = packet.Value.Sequence + d.Length / Constants.FRAME_SIZE;

            Array.Copy(d, 0, _decodedBuffer, _decodedOffset, d.Length);
            _decodedCount += d.Length;
            return true;
        }

        private struct BufferPacket
        {
            public byte[] Data;
            public long Sequence;
        }
    }
}
