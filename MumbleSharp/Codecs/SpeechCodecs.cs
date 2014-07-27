
using System;

namespace MumbleSharp.Codecs
{
    public enum SpeechCodecs
    {
        CeltAlpha = 0,
        Speex = 2,
        CeltBeta = 3
    }

    public static class SpeechCodecsExtensions
    {
        [ThreadStatic] private static IVoiceCodec[] _codecs;

        public static IVoiceCodec GetCodec(this SpeechCodecs codec)
        {
            if (_codecs == null)
            {
                _codecs = new IVoiceCodec[]
                {
                    new CeltAlphaCodec(),   //CeltAlpha
                    null,                   //Nothing!
                    new SpeexCodec(),       //Speex
                    new CeltBetaCodec(),    //CeltBeta
                };
            }

            if (!Enum.IsDefined(typeof(SpeechCodecs), codec))
                throw new ArgumentException("codec");

            return _codecs[(int)codec];
        }
    }
}
