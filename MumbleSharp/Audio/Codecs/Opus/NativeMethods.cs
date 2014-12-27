// 
// Author: John Carruthers (johnc@frag-labs.com)
// 
// Copyright (C) 2013 John Carruthers
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//  
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//  
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// 

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace MumbleSharp.Audio.Codecs.Opus
{
    /// <summary>
    /// Wraps the Opus API.
    /// </summary>
    internal class NativeMethods
    {
        static NativeMethods()
        {
            IntPtr image;
            if (PlatformDetails.IsMac)
            {
                image = LibraryLoader.Load(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Audio", "Codecs", "Opus", "Libs", "32bit", "libopus.dylib"));
            }
            else if (PlatformDetails.IsWindows)
            {
                if (!Environment.Is64BitProcess)
                    image = LibraryLoader.Load(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Audio", "Codecs", "Opus", "Libs", "32bit", "opus.dll"));
                else
                    image = LibraryLoader.Load(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Audio", "Codecs", "Opus", "Libs", "64bit", "opus.dll"));
            }
            else
            {
                image = LibraryLoader.Load("libopus.so.0");
				if (image.Equals(IntPtr.Zero))
                    image = LibraryLoader.Load(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Audio", "Codecs", "Opus", "Libs", "libopus.so"));
            }

            if (image != IntPtr.Zero)
            {
                var type = typeof(NativeMethods);
                foreach (var member in type.GetFields(BindingFlags.Static | BindingFlags.NonPublic))
                {
                    var methodName = member.Name;
                    if (methodName == "opus_encoder_ctl_out") methodName = "opus_encoder_ctl";
                    var fieldType = member.FieldType;
                    var ptr = LibraryLoader.ResolveSymbol(image, methodName);
                    if (ptr == IntPtr.Zero)
                        throw new Exception(string.Format("Could not resolve symbol \"{0}\"", methodName));
                    member.SetValue(null, Marshal.GetDelegateForFunctionPointer(ptr, fieldType));
                }
            }
        }

        // ReSharper disable InconsistentNaming
        // ReSharper disable UnassignedField.Compiler

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate IntPtr opus_encoder_create_delegate(int sampleRate, int channelCount, int application, out IntPtr error);
        internal static opus_encoder_create_delegate opus_encoder_create;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void opus_encoder_destroy_delegate(IntPtr encoder);
        internal static opus_encoder_destroy_delegate opus_encoder_destroy;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int opus_encode_delegate(IntPtr encoder, IntPtr pcm, int frameSize, IntPtr data, int maxDataBytes);
        internal static opus_encode_delegate opus_encode;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate IntPtr opus_decoder_create_delegate(int sampleRate, int channelCount, out IntPtr error);
        internal static opus_decoder_create_delegate opus_decoder_create;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void opus_decoder_destroy_delegate(IntPtr decoder);
        internal static opus_decoder_destroy_delegate opus_decoder_destroy;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int opus_decode_delegate(IntPtr decoder, IntPtr data, int len, IntPtr pcm, int frameSize, int decodeFec);
        internal static opus_decode_delegate opus_decode;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int opus_packet_get_nb_channels_delegate(IntPtr data);
        internal static opus_packet_get_nb_channels_delegate opus_packet_get_nb_channels;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int opus_packet_get_nb_samples_delegate(IntPtr data, int len, int sampleRate);
        internal static opus_packet_get_nb_samples_delegate opus_packet_get_nb_samples;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int opus_encoder_ctl_delegate(IntPtr encoder, Ctl request, int value);
        internal static opus_encoder_ctl_delegate opus_encoder_ctl;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int opus_encoder_ctl_out_delegate(IntPtr encoder, Ctl request, out int value);
        internal static opus_encoder_ctl_out_delegate opus_encoder_ctl_out;

        // ReSharper restore UnassignedField.Compiler
        // ReSharper restore InconsistentNaming

        public enum Ctl
        {
            SetBitrateRequest = 4002,
            GetBitrateRequest = 4003,
            SetInbandFecRequest = 4012,
            GetInbandFecRequest = 4013
        }

        public enum OpusErrors
        {
            Ok = 0,
            BadArgument = -1,
            BufferToSmall = -2,
            InternalError = -3,
            InvalidPacket = -4,
            NotImplemented = -5,
            InvalidState = -6,
            AllocFail = -7
        }
    }
}