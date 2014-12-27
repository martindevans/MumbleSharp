using System;

namespace MumbleSharp.Audio.Codecs.Opus
{
    public class OpusCodec
        : IVoiceCodec
    {
        readonly OpusDecoder _decoder = new OpusDecoder((int)Constants.SAMPLE_RATE, 1) { EnableForwardErrorCorrection = true };

        public byte[] Decode(byte[] encodedData)
        {
            if (encodedData == null)
            {
                _decoder.Decode(null, 0, 0, new byte[Constants.FRAME_SIZE], 0);
                return null;
            }

            int samples = OpusDecoder.GetSamples(encodedData, 0, encodedData.Length, 48000);
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
