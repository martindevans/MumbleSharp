using System;

namespace MumbleSharp.Codecs.Opus
{
    public class OpusCodec
        : IVoiceCodec
    {
        readonly OpusDecoder _decoder = new OpusDecoder(48000, 1) { EnableForwardErrorCorrection = true };

        public byte[] Decode(byte[] encodedData)
        {
            int samples = _decoder.GetSamples(encodedData, 0, encodedData.Length, 48000);
            if (samples < 1)
                return null;

            byte[] dst = new byte[samples * sizeof(ushort)];
            int length = _decoder.Decode(encodedData, 0, encodedData.Length, dst, 0);
            if (dst.Length != length)
                Array.Resize(ref dst, length);
            return dst;
        }

        public byte[] Encode(byte[] pcm)
        {
            throw new NotImplementedException();
        }
    }
}
