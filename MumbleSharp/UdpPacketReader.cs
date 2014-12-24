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
                return null;

            return buffer;
        }

        public long ReadVarInt64()
        {
            ////My implementation, neater (imo) but broken
            //byte b = ReadByte();
            //int leadingOnes = LeadingOnes(b);
            //switch (leadingOnes)
            //{
            //    case 0:
            //        return b & 127;
            //    case 1:
            //        //10xxxxxx + 1 byte
            //        return ((b & 63) << 8) | ReadByte();
            //    case 2:
            //        //110xxxxx + 2 bytes
            //        return ((b & 31) << 16) | ReadByte() << 8 | ReadByte();
            //    case 3:
            //        //1110xxxx + 3 bytes
            //        return ((b & 15) << 24) | ReadByte() << 16 | ReadByte() << 8 | ReadByte();
            //    case 4:
            //        // Either:
            //        //  > 111100__ + int (4 bytes)
            //        //  > 111101__ + long (8 bytes)
            //        if ((b & 4) == 4)
            //        {
            //            //111101__ + long (8 bytes)
            //            return ReadByte() << 56 | ReadByte() << 48 | ReadByte() << 40 | ReadByte() << 32 | ReadByte() << 24 | ReadByte() << 16 | ReadByte() << 8 | ReadByte();
            //        }
            //        else
            //        {
            //            //111100__ + int (4 bytes)
            //            return ReadByte() << 24 | ReadByte() << 16 | ReadByte() << 8 | ReadByte();
            //        }
            //    case 5:
            //        //111110 + varint (negative)
            //        return -ReadVarInt64();
            //    case 6:
            //        //111111xx Byte-inverted negative two bit number (~xx)
            //        return ~(b & 3);
            //    default:
            //        throw new InvalidDataException("Invalid varint encoding");
            //}

            //Copied from https://github.com/pcgod/mumble-android/blob/master/src/org/pcgod/mumbleclient/service/PacketDataStream.java
            long i = 0;
		    long v = ReadByte();

		    if ((v & 0x80) == 0x00) {
			    i = v & 0x7F;
		    } else if ((v & 0xC0) == 0x80) {
			    i = (v & 0x3F) << 8 | ReadByte();
		    } else if ((v & 0xF0) == 0xF0) {
			    int tmp = (int) (v & 0xFC);
			    switch (tmp) {
			    case 0xF0:
                        i = ReadByte() << 24 | ReadByte() << 16 | ReadByte() << 8 | ReadByte();
				    break;
			    case 0xF4:
                    i = ReadByte() << 56 | ReadByte() << 48 | ReadByte() << 40 | ReadByte() << 32 |
                        ReadByte() << 24 | ReadByte() << 16 | ReadByte() << 8 | ReadByte();
				    break;
			    case 0xF8:
				    i = ReadVarInt64();
				    i = ~i;
				    break;
			    case 0xFC:
				    i = v & 0x03;
				    i = ~i;
				    break;
			    default:
                    throw new InvalidDataException("Invalid varint encoding");
			    }
		    } else if ((v & 0xF0) == 0xE0) {
                i = (v & 0x0F) << 24 | ReadByte() << 16 | ReadByte() << 8 | ReadByte();
		    } else if ((v & 0xE0) == 0xC0) {
                i = (v & 0x1F) << 16 | ReadByte() << 8 | ReadByte();
		    }
		    return i;
        }

        public static int LeadingOnes(byte value)
        {
            int counter = 0;
            while ((value & 128) == 128)
            {
                value <<= 1;
                counter++;
            }
            return counter;
        }
    }
}
