
namespace MumbleSharp.Codecs
{
    public interface IVoiceCodec
    {
        byte[] Decode(byte[] encodedData);

        byte[] Encode(byte[] pcm);
    }
}
