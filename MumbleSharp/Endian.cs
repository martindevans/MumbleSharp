using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace MumbleSharp
{
    static class Endian
    {
        public static short FromHostToLittleEndian(short value)
        {
            short big = IPAddress.HostToNetworkOrder(value);
            return SwapEndian(big);
        }

        public static short FromLittleEndianToHost(short value)
        {
            short big = SwapEndian(value);
            return IPAddress.NetworkToHostOrder(big);
        }

        public static short SwapEndian(short value)
        {
            return (short)((value << 8 & ushort.MaxValue) | value >> 8);
        }

        public static int SwapEndian(int value)
        {
            return BitConverter.ToInt32(BitConverter.GetBytes(value).Reverse().ToArray(), 0);
        }

        public static uint SwapEndian(uint value)
        {
            return BitConverter.ToUInt32(BitConverter.GetBytes(value).Reverse().ToArray(), 0);
        }
    }
}
