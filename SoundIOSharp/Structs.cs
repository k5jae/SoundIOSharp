//
// Structs.cs
//
// Author:
//       Jae Stutzman <jaebird@gmail.com>
//
// Copyright (c) 2016 Jae Stutzman
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Runtime.InteropServices;

namespace SoundIOSharp
{
	[StructLayout (LayoutKind.Sequential)]
	public class ChannelLayout {
		[MarshalAs (UnmanagedType.LPStr)]
		public string Name;
		public int ChannelCount;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst=24)]
		public Channels[] Channels;
	};

	[StructLayout (LayoutKind.Sequential)]
	public class SampleRateRange {
		public int Min;
		public int Max;
	};

	[StructLayout (LayoutKind.Sequential)]
	internal class ChannelAreaNative {
		/// Base address of buffer.
		internal IntPtr ptr;
		/// How many bytes it takes to get from the beginning of one sample to
		/// the beginning of the next sample.
		internal int step;
	};

	public class ChannelArea {
		IntPtr nativePtr;
		ChannelAreaNative nativeStruct;

		public IntPtr NativeBufferPtr{
			get {
				return nativeStruct.ptr;
			}
		}

		/// How many bytes it takes to get from the beginning of one sample to
		/// the beginning of the next sample.
		public int Step {
			get {
				return nativeStruct.step;
			}
		}

		internal ChannelArea(IntPtr nativePtr, ChannelAreaNative nativeStruct)
		{
			this.nativePtr = nativePtr;
			this.nativeStruct = nativeStruct;
		}
	};
		
	[StructLayout (LayoutKind.Sequential)]
	internal class SoundIONative {

		internal IntPtr userdata;

		[MarshalAs(UnmanagedType.FunctionPtr)]
		internal on_soundio_delegate_native on_devices_change;

		[MarshalAs(UnmanagedType.FunctionPtr)]
		internal on_backend_disconnect_delegate_native on_backend_disconnect;

		[MarshalAs(UnmanagedType.FunctionPtr)]
		internal on_soundio_delegate_native on_events_signal;

		internal Backend current_backend;

		[MarshalAs (UnmanagedType.LPStr)]
		internal string AppName;

		IntPtr emit_rtprio_warning;
		IntPtr jack_info_callback;
		IntPtr jack_error_callback;
	};



}

