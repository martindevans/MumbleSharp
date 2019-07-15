using System;
using System.Collections.Generic;

namespace MumbleSharp.Audio.Codecs.Opus
{
    public class OpusCodec
        : IVoiceCodec
    {
        private readonly OpusDecoder _decoder;
        private readonly OpusEncoder _encoder;
        private readonly int _sampleRate;
        private readonly float _frameSize;

        public OpusCodec(int sampleRate = Constants.DEFAULT_AUDIO_SAMPLE_RATE, byte sampleBits = Constants.DEFAULT_AUDIO_SAMPLE_BITS, byte channels = Constants.DEFAULT_AUDIO_SAMPLE_CHANNELS, float frameSize = Constants.DEFAULT_AUDIO_FRAME_SIZE)
        {
            _sampleRate = sampleRate;
            _frameSize = frameSize;
            _decoder = new OpusDecoder(sampleRate, channels) { EnableForwardErrorCorrection = true };
            _encoder = new OpusEncoder(sampleRate, channels) { EnableForwardErrorCorrection = true };
        }

        public byte[] Decode(byte[] encodedData)
        {
            if (encodedData == null)
            {
                _decoder.Decode(null, 0, 0, new byte[(int)(_sampleRate / _frameSize)], 0);
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
