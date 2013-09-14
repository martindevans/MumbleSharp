using System;
using System.IO;

namespace MumbleSharp
{
    public class UdpPacketReader
        :IDisposable
    {
        readonly Stream _inner;

        public UdpPacketReader(Stream innerStream)
        {
            _inner = innerStream;
        }

        public void Dispose()
        {
            _inner.Dispose();
        }

        public byte ReadByte()
        {
            return (byte)_inner.ReadByte();
        }

        public byte[] ReadBytes(int length)
        {
            byte[] buffer = new byte[length];
            var read = _inner.Read(buffer, 0, length);
            if (length != read)
                throw new ArgumentException("Invalid Length");

            return buffer;
        }

        public long ReadVarInt64()
        {
            byte b = ReadByte();

            int leadingOnes = LeadingOnes(b);

            switch (leadingOnes)
            {
                case 0:
                    return b & 127;
                case 1:
                    //10xxxxxx + 1 byte
                    break;
                case 2:
                    //110xxxxx + 2 bytes
                    break;
                case 3:
                    //1110xxxx + 3 bytes
                    break;
                case 4:
                    //111100 + int (4 bytes)
                    //111101 + long (8 bytes)
                    break;
                case 5:
                    //111110 + varint (negative)
                    break;
                case 6:
                    //111111xx Negative two byte number (-xx)
                    break;
            }

            throw new NotImplementedException();
        }

        private const int LEADING1 = 1 << 8;    //0b10000000
        private static int LeadingOnes(byte value)
        {
            int counter = 0;
            while ((value & LEADING1) == 1)
            {
                value <<= 1;
                counter++;
            }
            return counter;
        }
    }
}
