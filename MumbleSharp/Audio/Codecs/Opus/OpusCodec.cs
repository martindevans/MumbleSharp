using System;
using System.Collections.Generic;

namespace MumbleSharp.Audio.Codecs.Opus
{
    public class OpusCodec
        : IVoiceCodec
    {
        readonly OpusDecoder _decoder = new OpusDecoder(Constants.SAMPLE_RATE, Constants.CHANNELS) { EnableForwardErrorCorrection = true };
        readonly OpusEncoder _encoder = new OpusEncoder(Constants.SAMPLE_RATE, Constants.CHANNELS) { EnableForwardErrorCorrection = true };

        public byte[] Decode(byte[] encodedData)
        {
            if (encodedData == null)
            {
                _decoder.Decode(null, 0, 0, new byte[Constants.FRAME_SIZE], 0);
                return null;
            }

            int samples = OpusDecoder.GetSamples(encodedData, 0, encodedData.Length, Constants.SAMPLE_RATE);
            if (samples < 1)
                return null;

            byte[] dst = new byte[samples * sizeof(ushort)];
            int length = _decoder.Decode(encodedData, 0, encodedData.Length, dst, 0);
            if (dst.Length != length)
                Array.Resize(ref dst, length);
            return dst;
        }

        public IEnumerable<int> PermittedEncodingFrameSizes
        {
            get
            {
                return _encoder.PermittedFrameSizes;
            }
        }

        public byte[] Encode(ArraySegment<byte> pcm)
        {
            var samples = pcm.Count / sizeof(ushort);
            var numberOfBytes = _encoder.FrameSizeInBytes(samples);

            byte[] dst = new byte[numberOfBytes];
            int encodedBytes = _encoder.Encode(pcm.Array, pcm.Offset, dst, 0, samples);

            //without it packet will have huge zero-value-tale
            Array.Resize(ref dst, encodedBytes);

            return dst;
        }
    }
}
