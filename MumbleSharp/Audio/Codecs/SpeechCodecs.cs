namespace MumbleSharp.Audio.Codecs
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
}
