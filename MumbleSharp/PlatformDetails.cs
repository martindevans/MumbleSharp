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

namespace MumbleSharp
{
    /// <summary>
    /// Provides access to platform details.
    /// </summary>
    public class PlatformDetails
    {
        static PlatformDetails()
        {
            if (Directory.Exists("/Applications")
                && Directory.Exists("/System")
                && Directory.Exists("/Users")
                && Directory.Exists("/Volumes"))
                IsMac = true;
			if (Environment.OSVersion.Platform == PlatformID.Win32NT ||
				Environment.OSVersion.Platform == PlatformID.Win32Windows)
				IsWindows = true;
        }

        /// <summary>
        /// Gets if the current system is a Mac OSX.
        /// </summary>
        public static bool IsMac { get; private set; }

		/// <summary>
		/// Gets if the current system is windows.
		/// </summary>
		/// <value><c>true</c> if is windows; otherwise, <c>false</c>.</value>
		public static bool IsWindows { get; private set; }
    }
}