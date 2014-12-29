using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MumbleSharp.Audio
{
    public class AudioEncodingBuffer
    {
        private readonly IMumbleProtocol _protocol;

        public AudioEncodingBuffer(IMumbleProtocol protocol)
        {
            _protocol = protocol;
        }
    }
}
