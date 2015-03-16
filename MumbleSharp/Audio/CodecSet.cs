using MumbleSharp.Audio.Codecs;
using MumbleSharp.Audio.Codecs.CeltAlpha;
using MumbleSharp.Audio.Codecs.CeltBeta;
using MumbleSharp.Audio.Codecs.Opus;
using MumbleSharp.Audio.Codecs.Speex;
using System;

namespace MumbleSharp.Audio
{
    public class CodecSet
    {
        private readonly Lazy<CeltAlphaCodec> _alpha = new Lazy<CeltAlphaCodec>();
        private readonly Lazy<CeltBetaCodec> _beta = new Lazy<CeltBetaCodec>();
        private readonly Lazy<SpeexCodec> _speex = new Lazy<SpeexCodec>();
        private readonly Lazy<OpusCodec> _opus = new Lazy<OpusCodec>();

        protected internal IVoiceCodec GetCodec(SpeechCodecs codec)
        {
            switch (codec)
            {
                case SpeechCodecs.CeltAlpha:
                    return _alpha.Value;
                case SpeechCodecs.Speex:
                    return _speex.Value;
                case SpeechCodecs.CeltBeta:
                    return _beta.Value;
                case SpeechCodecs.Opus:
                    return _opus.Value;
                default:
                    throw new ArgumentOutOfRangeException("codec");
            }
        }
    }
}
