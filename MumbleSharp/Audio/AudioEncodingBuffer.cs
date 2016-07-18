using MumbleSharp.Audio.Codecs;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace MumbleSharp.Audio
{
    public class AudioEncodingBuffer
    {
        private readonly BlockingCollection<TargettedSpeech> _unencodedBuffer = new BlockingCollection<TargettedSpeech>(new ConcurrentQueue<TargettedSpeech>());

        private readonly CodecSet _codecs = new CodecSet();

        private SpeechTarget _target;
        private uint _targetId;
        private readonly DynamicCircularBuffer _pcmBuffer = new DynamicCircularBuffer();

        private TargettedSpeech? _unencodedItem;

        /// <summary>
        /// Add some raw PCM data to the buffer to send
        /// </summary>
        /// <param name="pcm"></param>
        /// <param name="target"></param>
        /// <param name="targetId"></param>
        public void Add(ArraySegment<byte> pcm, SpeechTarget target, uint targetId)
        {
            _unencodedBuffer.Add(new TargettedSpeech(pcm, target, targetId));
        }

        public void Stop()
        {
            _unencodedBuffer.Add(new TargettedSpeech(stop: true));
        }

        public byte[] Encode(SpeechCodecs codec)
        {
            //Get the codec
            var codecInstance = _codecs.GetCodec(codec);

            //How many bytes can we fit into the larget frame?
            var maxBytes = codecInstance.PermittedEncodingFrameSizes.Max() * sizeof(ushort);

            bool stopped = false;

            //If we have an unencoded item stored here it's because a previous iteration pulled from the queue and discovered it could not process this packet (different target)
            if (_unencodedItem.HasValue && TryAddToEncodingBuffer(_unencodedItem.Value, out stopped))
            {
                _unencodedItem = null;
            }

            if (stopped)
            {
                //remove stop packet
                TargettedSpeech item;
                _unencodedBuffer.TryTake(out item, TimeSpan.FromMilliseconds(1));
                _unencodedItem = null;
            }

            //Accumulate as many bytes as we can stuff into a single frame
            while (_pcmBuffer.Count < maxBytes && !stopped)
            {
                TargettedSpeech item;
                if (!_unencodedBuffer.TryTake(out item, TimeSpan.FromMilliseconds(1)))
                    break;

                //Add this packet to the encoding buffer, or stop accumulating bytes
                if (!TryAddToEncodingBuffer(item, out stopped))
                {
                    _unencodedItem = item;
                    break;
                }
            }

            //Nothing to encode, early exit
            if (_pcmBuffer.Count == 0)
                return null;

            if (stopped)
            {
                //User has stopped talking, pad buffer up to next buffer size with silence
                var frameBytes = codecInstance.PermittedEncodingFrameSizes.Select(f => f * sizeof(ushort)).Where(f => f >= _pcmBuffer.Count).Min();
                byte[] b = new byte[frameBytes];
                int read = _pcmBuffer.Read(new ArraySegment<byte>(b));

                return codecInstance.Encode(new ArraySegment<byte>(b, 0, read));
            }
            else
            {
                //We have a load of bytes of PCM data, let's encode them
                var frameBytesList = codecInstance.PermittedEncodingFrameSizes.Select(f => f * sizeof(ushort)).Where(f => f <= _pcmBuffer.Count);
                if (frameBytesList.Count() > 0)
                {
                    var frameBytes = frameBytesList.Max();
                    byte[] b = new byte[frameBytes];
                    int read = _pcmBuffer.Read(new ArraySegment<byte>(b));

                    return codecInstance.Encode(new ArraySegment<byte>(b, 0, read));
                }
                else return null;
            }
        }

        private bool TryAddToEncodingBuffer(TargettedSpeech t, out bool stopped)
        {
            if (t.IsStop)
            {
                stopped = true;
                return false;
            }
            stopped = false;

            if (!(_pcmBuffer.Count == 0 || (_target == t.Target && _targetId == t.TargetId)))
                return false;

            _pcmBuffer.Write(t.Pcm);

            _target = t.Target;
            _targetId = t.TargetId;

            return true;
        }

        /// <summary>
        /// PCM data targetted at a specific person
        /// </summary>
        private struct TargettedSpeech
        {
            public readonly ArraySegment<byte> Pcm;
            public readonly SpeechTarget Target;
            public readonly uint TargetId;

            public readonly bool IsStop;

            public TargettedSpeech(ArraySegment<byte> pcm, SpeechTarget target, uint targetId)
            {
                TargetId = targetId;
                Target = target;
                Pcm = pcm;

                IsStop = false;
            }

            public TargettedSpeech(bool stop = true)
            {
                IsStop = stop;

                Pcm = default(ArraySegment<byte>);
                Target = SpeechTarget.Normal;
                TargetId = 0;
            }
        }
    }
}
