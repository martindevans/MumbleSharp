using System;
using System.Collections.Generic;

namespace MumbleSharp.Audio.Codecs.Opus
{
    public class OpusCodec
        : IVoiceCodec
    {
        readonly OpusDecoder _decoder;
        readonly OpusEncoder _encoder;
        readonly ushort _sampleRate;

        public OpusCodec(ushort SampleRate = Constants.DEFAULT_AUDIO_SAMPLE_RATE, ushort SampleBits = Constants.DEFAULT_AUDIO_SAMPLE_BITS, ushort Channels = Constants.DEFAULT_AUDIO_SAMPLE_CHANNELS)
        {
            _sampleRate = SampleRate;
            _decoder = new OpusDecoder(SampleRate, Channels) { EnableForwardErrorCorrection = true };
            _encoder = new OpusEncoder(SampleRate, Channels) { EnableForwardErrorCorrection = true };
        }

        public byte[] Decode(byte[] encodedData)
        {
            if (encodedData == null)
            {
                _decoder.Decode(null, 0, 0, new byte[_sampleRate / 20], 0);
                return null;
            }

            int samples = OpusDecoder.GetSamples(encodedData, 0, encodedData.Length, _sampleRate);
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
