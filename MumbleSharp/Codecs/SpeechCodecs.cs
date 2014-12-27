
using System;
using MumbleSharp.Codecs.Opus;

namespace MumbleSharp.Codecs
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>See part way down https://github.com/mumble-voip/mumble/blob/master/src/Message.h for the equivalent declaration in the official mumble repo</remarks>
    public enum SpeechCodecs
    {
        CeltAlpha = 0,
        Speex = 2,
        CeltBeta = 3,
        Opus = 4
    }

    public static class SpeechCodecsExtensions
    {
        [ThreadStatic] private static IVoiceCodec[] _codecs;

        public static IVoiceCodec GetCodec(this SpeechCodecs codec)
        {
            // Codecs array is threadstatic, this means it will be unique per thread.
            // First time codecs is accessed (by a particular thread) it will be null, we initialise it then
            if (_codecs == null)
            {
                _codecs = new IVoiceCodec[]
                {
                    new CeltAlphaCodec(),   //CeltAlpha
                    null,                   //Nothing!
                    new SpeexCodec(),       //Speex
                    new CeltBetaCodec(),    //CeltBeta
                    new OpusCodec(),        //Opus
                };
            }

            if (!Enum.IsDefined(typeof(SpeechCodecs), codec))
                throw new ArgumentException("codec");

            return _codecs[(int)codec];
        }
    }
}
