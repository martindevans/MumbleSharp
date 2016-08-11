using System;
using System.Collections.Generic;

namespace MumbleSharp
{
    static class Var64
    {
        //This stuff is a partial duplicate of the varint64 stuff in UdpPacketReader!
        //Should write a UdpPacketWriter to mirror it

        public static int calculateVarint64(UInt64 value)
        {
	        UInt64 part0 = value;
	        UInt64 part1 = value >> 28;
	        UInt64 part2 = value >> 56;
	        if (part2 == 0) {
	            if (part1 == 0) {
	                if (part0 < (1 << 14))
	                    return part0 < (1 << 7) ? 1 : 2;
	                else
	                    return part0 < (1 << 21) ? 3 : 4;
	            } else {
	                if (part1 < (1 << 14))
	                    return part1 < (1 << 7) ? 5 : 6;
	                else
	                    return part1 < (1 << 21) ? 7 : 8;
	            }
	        } else
	            return part2 < (1 << 7) ? 9 : 10;
	    }

        public static byte[] writeVarint64(UInt64 value)
        {
            UInt64 part0 = value;
            UInt64 part1 = value >> 28;
            UInt64 part2 = value >> 56;
            int size = calculateVarint64(value);
            byte[] array = new byte[size];

            //var dst = new Uint8Array(this.array);
            switch (size)
            {
                case 10: array[9] = (byte)((part2 >> 7) | 0x80); goto case 9;
                case 9: array[8] = (byte)((part2) | 0x80); goto case 8;
                case 8: array[7] = (byte)((part1 >> 21) | 0x80); goto case 7;
                case 7: array[6] = (byte)((part1 >> 14) | 0x80); goto case 6;
                case 6: array[5] = (byte)((part1 >> 7) | 0x80); goto case 5;
                case 5: array[4] = (byte)((part1) | 0x80); goto case 4;
                case 4: array[3] = (byte)((part0 >> 21) | 0x80); goto case 3;
                case 3: array[2] = (byte)((part0 >> 14) | 0x80); goto case 2;
                case 2: array[1] = (byte)((part0 >> 7) | 0x80); goto case 1;
                case 1: array[0] = (byte)((part0) | 0x80); break;
            }
            array[size - 1] &= 0x7F;

            return array;
        }

        public static byte[] writeVarint64_alternative(UInt64 value)
        {
            UInt64 i = value;
            List<byte> byteList = new List<byte>();

            if (
                    ((i & 0x8000000000000000L) != 0) &&
                    (~i < 0x100000000L)
                )
            {
                // Signed number.
                i = ~i;
                if (i <= 0x3)
                {
                    // Shortcase for -1 to -4
                    byteList.Add((byte)(0xFC | i));
                    return byteList.ToArray();
                }
                else
                {
                    byteList.Add(0xF8);
                }
            }
            if (i < 0x80)
            {
                // Need top bit clear
                byteList.Add((byte)i);
            }
            else if (i < 0x4000)
            {
                // Need top two bits clear
                byteList.Add((byte)((i >> 8) | 0x80));
                byteList.Add((byte)(i & 0xFF));
            }
            else if (i < 0x200000)
            {
                // Need top three bits clear
                byteList.Add((byte)((i >> 16) | 0xC0));
                byteList.Add((byte)((i >> 8) & 0xFF));
                byteList.Add((byte)(i & 0xFF));
            }
            else if (i < 0x10000000)
            {
                // Need top four bits clear
                byteList.Add((byte)((i >> 24) | 0xE0));
                byteList.Add((byte)((i >> 16) & 0xFF));
                byteList.Add((byte)((i >> 8) & 0xFF));
                byteList.Add((byte)(i & 0xFF));
            }
            else if (i < 0x100000000L)
            {
                // It's a full 32-bit integer.
                byteList.Add(0xF0);
                byteList.Add((byte)((i >> 24) & 0xFF));
                byteList.Add((byte)((i >> 16) & 0xFF));
                byteList.Add((byte)((i >> 8) & 0xFF));
                byteList.Add((byte)(i & 0xFF));
            }
            else
            {
                // It's a 64-bit value.
                byteList.Add(0xF4);
                byteList.Add((byte)((i >> 56) & 0xFF));
                byteList.Add((byte)((i >> 48) & 0xFF));
                byteList.Add((byte)((i >> 40) & 0xFF));
                byteList.Add((byte)((i >> 32) & 0xFF));
                byteList.Add((byte)((i >> 24) & 0xFF));
                byteList.Add((byte)((i >> 16) & 0xFF));
                byteList.Add((byte)((i >> 8) & 0xFF));
                byteList.Add((byte)(i & 0xFF));
            }
            return byteList.ToArray();
        }
    }
}
