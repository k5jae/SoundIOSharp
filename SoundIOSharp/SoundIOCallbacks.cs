//
// SoundIOCallbacks.cs
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
	public partial class SoundIO : IDisposable
	{
		public event EventHandler OnDevicesChanged;
		public event OnBackendDisconnectDelegate OnBackendDisconnected;
		public event EventHandler OnEvents;

		private void on_devices_change_native(IntPtr soundio)
		{
			//var back =  this.soundIOStructNative.current_backend;

			if (OnDevicesChanged != null) {
				OnDevicesChanged (this, new EventArgs ());
			}
			//Console.WriteLine ("OnDevicesChange");
		}

		private void on_backend_disconnect_native(IntPtr soundio, int err)
		{
			if (OnBackendDisconnected != null) {
				OnBackendDisconnected (this, new BackendDisconnectEventArgs(err));
			}
			//Console.WriteLine ("OnBackendDisconnect");
		}

		private void on_event_signal_native(IntPtr soundio)
		{
			if (OnEvents != null) {
				OnEvents (this, new EventArgs ());
			}
			//Console.WriteLine ("OnEventsSignal");
		}
	}

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate void on_soundio_delegate_native(IntPtr soundio);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate void on_backend_disconnect_delegate_native(IntPtr soundio, int err);

	public class BackendDisconnectEventArgs : EventArgs
	{
		private int err;
		public int Err {
			get {
				return err;
			}
		}

		public BackendDisconnectEventArgs(int err)
		{
			this.err = err;
		}
	}

	public delegate void OnBackendDisconnectDelegate(object sender, BackendDisconnectEventArgs eventArgs);

	// streams

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate void on_readorwrite_delegate_native(IntPtr soundio_in_outstream, int frame_count_min, int frame_count_max);
	public delegate void OnWriteDelegate(OutStream stream, int frameCountMin, int frameCountMax);
	public delegate void OnReadDelegate(InStream stream, int frameCountMin, int frameCountMax);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate void on_underoverflow_delegate_native(IntPtr soundio_in_outstream);
	public delegate void OnUnderFlowDelegate(OutStream stream);
	public delegate void OnOverFlowDelegate(InStream stream);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate void on_error_delegate_native(IntPtr soundio_in_outstream, int err);
	public delegate void OnErrorDelegate(Stream stream, int err);
}

