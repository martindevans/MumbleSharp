using System;

namespace MumbleSharp.Audio.Codecs.CeltAlpha
{
    public class CeltBetaCodec
        : IVoiceCodec
    {
        public byte[] Decode(byte[] encodedData)
        {
            throw new NotImplementedException();
        }

        public System.Collections.Generic.IEnumerable<int> PermittedEncodingFrameSizes
        {
            get { throw new NotImplementedException(); }
        }

        public byte[] Encode(ArraySegment<byte> pcm)
        {
            throw new NotImplementedException();
        }
    }
}
