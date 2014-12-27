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
            if (_inner.Position >= _inner.Length)
                throw new EndOfStreamException("Cannot read any more bytes from Udp Packet");

            return (byte)_inner.ReadByte();
        }

        public byte[] ReadBytes(int length)
        {
            byte[] buffer = new byte[length];
            var read = _inner.Read(buffer, 0, length);
            if (length != read)
                return null;

            return buffer;
        }

        public long ReadVarInt64()
        {
            //My implementation, neater (imo) but broken
            byte b = ReadByte();
            int leadingOnes = LeadingOnes(b);
            switch (leadingOnes)
            {
                case 0:
                    return b & 127;
                case 1:
                    //10xxxxxx + 1 byte
                    return ((b & 63) << 8) | ReadByte();
                case 2:
                    //110xxxxx + 2 bytes
                    return ((b & 31) << 16) | ReadByte() << 8 | ReadByte();
                case 3:
                    //1110xxxx + 3 bytes
                    return ((b & 15) << 24) | ReadByte() << 16 | ReadByte() << 8 | ReadByte();
                case 4:
                    // Either:
                    //  > 111100__ + int (4 bytes)
                    //  > 111101__ + long (8 bytes)
                    if ((b & 4) == 4)
                    {
                        //111101__ + long (8 bytes)
                        return ReadByte() << 56 | ReadByte() << 48 | ReadByte() << 40 | ReadByte() << 32 | ReadByte() << 24 | ReadByte() << 16 | ReadByte() << 8 | ReadByte();
                    }
                    else
                    {
                        //111100__ + int (4 bytes)
                        return ReadByte() << 24 | ReadByte() << 16 | ReadByte() << 8 | ReadByte();
                    }
                case 5:
                    //111110 + varint (negative)
                    return ~ReadVarInt64();
                case 6:
                case 7:
                case 8:
                    //111111xx Byte-inverted negative two bit number (~xx)

                    //We need three cases here because all the other leading parts are capped off by a zero, e.g. 11110xxx
                    //However in this case it's just 6 ones, and then the data (111111xx). Depending on the data, the leading count changes
                    return ~(b & 3);
                default:
                    throw new InvalidDataException("Invalid varint encoding");
            }
        }

        private static readonly byte[] _leadingOnesLookup = {
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
            3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3,
            4, 4, 4, 4, 4, 4, 4, 4, 5, 5, 5, 5, 6, 6, 7, 8,
        };

        public static int LeadingOnes(byte value)
        {
            return _leadingOnesLookup[value];
        }
    }
}
