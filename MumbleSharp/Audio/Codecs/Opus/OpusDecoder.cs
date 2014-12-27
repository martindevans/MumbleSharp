//  
//  Author: John Carruthers (johnc@frag-labs.com)
//  
//  Copyright (C) 2013 John Carruthers
//  
//  Permission is hereby granted, free of charge, to any person obtaining
//  a copy of this software and associated documentation files (the
//  "Software"), to deal in the Software without restriction, including
//  without limitation the rights to use, copy, modify, merge, publish,
//  distribute, sublicense, and/or sell copies of the Software, and to
//  permit persons to whom the Software is furnished to do so, subject to
//  the following conditions:
//   
//  The above copyright notice and this permission notice shall be
//  included in all copies or substantial portions of the Software.
//   
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//  EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//  MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
//  NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
//  LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
//  OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
//  WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//  

using System;

namespace MumbleSharp.Audio.Codecs.Opus
{
    /// <summary>
    /// Opus decoder.
    /// </summary>
    public class OpusDecoder
        : IDisposable
    {
        /// <summary>
        /// Opus decoder.
        /// </summary>
        private IntPtr _decoder;

        /// <summary>
        /// Size of a sample, in bytes.
        /// </summary>
        private readonly int _sampleSize;

        /// <summary>
        /// Gets or sets if Forward Error Correction decoding is enabled.
        /// </summary>
        public bool EnableForwardErrorCorrection { get; set; }

        public OpusDecoder(int outputSampleRate, int outputChannelCount)
        {
            if (outputSampleRate != 8000 &&
                outputSampleRate != 12000 &&
                outputSampleRate != 16000 &&
                outputSampleRate != 24000 &&
                outputSampleRate != 48000)
                throw new ArgumentOutOfRangeException("outputSampleRate");
            if (outputChannelCount != 1 && outputChannelCount != 2)
                throw new ArgumentOutOfRangeException("outputChannelCount");

            IntPtr error;
            _decoder = NativeMethods.opus_decoder_create(outputSampleRate, outputChannelCount, out error);
            if ((NativeMethods.OpusErrors)error != NativeMethods.OpusErrors.Ok)
                throw new Exception(string.Format("Exception occured while creating decoder, {0}", ((NativeMethods.OpusErrors)error)));
            _sampleSize = sizeof(ushort) * outputChannelCount;
        }

        ~OpusDecoder()
        {
            Dispose();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            if (_decoder != IntPtr.Zero)
            {
                NativeMethods.opus_decoder_destroy(_decoder);
                _decoder = IntPtr.Zero;
            }
        }

        /// <summary>
        /// Decodes audio samples.
        /// </summary>
        /// <param name="srcEncodedBuffer">Encoded data.</param>
        /// <param name="srcOffset">The zero-based byte offset in srcEncodedBuffer at which to begin reading encoded data.</param>
        /// <param name="srcLength">The number of bytes to read from srcEncodedBuffer.</param>
        /// <param name="dstBuffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values starting at offset replaced with audio samples.</param>
        /// <param name="dstOffset">The zero-based byte offset in dstBuffer at which to begin writing decoded audio samples.</param>
        /// <returns>The number of bytes decoded and written to dstBuffer.</returns>
        /// <remarks>Set srcEncodedBuffer to null to instruct the decoder that a packet was dropped.</remarks>
        public unsafe int Decode(byte[] srcEncodedBuffer, int srcOffset, int srcLength, byte[] dstBuffer, int dstOffset)
        {
            var availableBytes = dstBuffer.Length - dstOffset;
            var frameCount = availableBytes / _sampleSize;
            int length;
            fixed (byte* bdec = dstBuffer)
            {
                var decodedPtr = IntPtr.Add(new IntPtr(bdec), dstOffset);
                if (srcEncodedBuffer != null)
                {
                    fixed (byte* bsrc = srcEncodedBuffer)
                    {
                        var srcPtr = IntPtr.Add(new IntPtr(bsrc), srcOffset);
                        length = NativeMethods.opus_decode(_decoder, srcPtr, srcLength, decodedPtr, frameCount, 0);
                    }
                }
                else
                {
                    length = NativeMethods.opus_decode(_decoder, IntPtr.Zero, 0, decodedPtr, frameCount, Convert.ToInt32(EnableForwardErrorCorrection));
                }
            }
            if (length < 0)
                throw new Exception("Decoding failed - " + ((NativeMethods.OpusErrors)length));
            return length * _sampleSize;
        }

        public static unsafe int GetSamples(byte[] srcEncodedBuffer, int srcOffset, int srcLength, int sampleRate)
        {
            fixed (byte* bsrc = srcEncodedBuffer)
            {
                var srcPtr = IntPtr.Add(new IntPtr(bsrc), srcOffset);
                return NativeMethods.opus_packet_get_nb_samples(srcPtr, srcLength, sampleRate);
            }
        }

        public static unsafe int GetChannels(byte[] srcEncodedBuffer, int srcOffset)
        {
            fixed (byte* bsrc = srcEncodedBuffer)
            {
                var srcPtr = IntPtr.Add(new IntPtr(bsrc), srcOffset);
                return NativeMethods.opus_packet_get_nb_channels(srcPtr);
            }
        }
    }
}