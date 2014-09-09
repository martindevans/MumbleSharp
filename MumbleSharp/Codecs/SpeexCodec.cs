using System;

namespace MumbleSharp.Codecs
{
    public class SpeexCodec
        : IVoiceCodec
    {
        public byte[] Decode(byte[] encodedData)
        {
            NSpeex.SpeexDecoder d = new NSpeex.SpeexDecoder(NSpeex.BandMode.Wide, false);
            NSpeex.SpeexJitterBuffer b = new NSpeex.SpeexJitterBuffer(d);

            b.Put(encodedData);

            short[] decoded = new short[1024];
            b.Get(decoded);

            return null;
        }

        public byte[] Encode(byte[] pcm)
        {
            throw new NotImplementedException();
        }
    }
}
