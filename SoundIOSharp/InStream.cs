//
// InStream.cs
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
	internal class InStreamNative {

		internal IntPtr device;

		internal Format format;

		internal int sample_rate;

		internal ChannelLayout layout;

		internal double software_latency;

		internal IntPtr userdata;

		[MarshalAs(UnmanagedType.FunctionPtr)]
		internal on_readorwrite_delegate_native on_readorwrite;

		[MarshalAs(UnmanagedType.FunctionPtr)]
		internal on_underoverflow_delegate_native on_underflow;

		[MarshalAs(UnmanagedType.FunctionPtr)]
		internal on_error_delegate_native on_error;

		[MarshalAs (UnmanagedType.LPStr)]
		internal string name;

		internal bool non_terminal_hint;

		internal int bytes_per_frame;
		internal int bytes_per_sample;
		internal Error layout_error;
	};

	public class InStream : Stream
	{

		internal IntPtr nativePtr;
		internal InStreamNative nativeStruct;

		/// <summary>
		/// Gets or sets the format.
		/// </summary>
		/// <value>The format.</value>
		public Format Format {
			get {
				return nativeStruct.format;
			}
			set {
				nativeStruct.format = value;
			}
		}

		/// <summary>
		/// Gets or sets the sample rate.
		/// </summary>
		/// <value>The sample rate.</value>
		public int SampleRate {
			get {
				return nativeStruct.sample_rate;
			}
			set {
				nativeStruct.sample_rate = value;
			}
		}

		/// <summary>
		/// Gets or sets the layout.
		/// </summary>
		/// <value>The layout.</value>
		public ChannelLayout Layout {
			get {
				return nativeStruct.layout;
			}
			set {
				nativeStruct.layout = value;
			}
		}

		/// <summary>
		/// Gets or sets the software latency.
		/// </summary>
		/// <value>The software latency.</value>
		public double SoftwareLatency {
			get {
				return nativeStruct.software_latency;
			}
			set {
				nativeStruct.software_latency = value;
			}
		}

		/// <summary>
		/// The delegate that is called when there is data to read.
		/// </summary>
		public OnReadDelegate OnRead;

		/// <summary>
		/// The delegate that is called when data is overflowing the buffer
		/// </summary>
		public OnOverFlowDelegate OnOverflow;

		/// <summary>
		/// Delegate that gets called when an error occurs with the stream.
		/// </summary>
		public OnErrorDelegate OnError;

		/// <summary>
		/// Gets or sets the name.
		/// </summary>
		/// <value>The name.</value>
		public string Name {
			get {
				return nativeStruct.name;
			}
			set {
				nativeStruct.name = value;
			}
		}

		/// <summary>
		/// Gets the bytes per frame.
		/// </summary>
		/// <value>The bytes per frame.</value>
		public int BytesPerFrame {
			get {
				return nativeStruct.bytes_per_frame;
			}
		}

		/// <summary>
		/// Gets the bytes per sample.
		/// </summary>
		/// <value>The bytes per sample.</value>
		public int BytesPerSample {
			get {
				return nativeStruct.bytes_per_sample;
			}
		}

		/// <summary>
		/// Gets the layout error.
		/// </summary>
		/// <value>The layout error.</value>
		public Error LayoutError {
			get {
				return nativeStruct.layout_error;
			}
		}

		// internal ctor
		internal InStream(IntPtr nativePtr, InStreamNative nativeStruct)
		{
			this.nativePtr = nativePtr;
			this.nativeStruct = nativeStruct;

			this.nativeStruct.on_readorwrite = OnReadNative;
			this.nativeStruct.on_underflow = OnUnderflowNative;
			this.nativeStruct.on_error = OnErrorNative;
		}

		// internal managed wrapper around native callback
		internal void OnReadNative(IntPtr stream, int frame_count_min, int frame_count_max)
		{
			if (OnRead != null) {
				OnRead (this, frame_count_min, frame_count_max);
			}
		}

		// internal managed wrapper around native callback
		internal void OnUnderflowNative(IntPtr stream)
		{
			if (OnOverflow != null) {
				OnOverflow (this);
			}
		}

		// internal managed wrapper around native callback
		internal void OnErrorNative(IntPtr stream, int err)
		{
			if (OnError != null) {
				OnError (this, err);
			}
		}

		/// <summary>
		/// Copies from ChannelArea to existing buffer array.
		/// </summary>
		/// <param name="area">Area.</param>
		/// <param name="buffer">Buffer.</param>
		/// <param name="bufferPos">Buffer position.</param>
		/// <param name="bufferCount">Buffer count.</param>
		public void CopyFrom(ChannelArea area, float[] buffer, int bufferPos, int bufferCount)
		{
			Marshal.Copy(area.NativeBufferPtr, buffer, bufferPos, bufferCount);
		}

		[DllImport (SoundIO.dllName)]
		private static extern void soundio_instream_destroy(IntPtr stream);
		protected override void Destroy()
		{
			soundio_instream_destroy (nativePtr);
		}

		[DllImport (SoundIO.dllName)]
		private static extern Error soundio_instream_open(IntPtr stream);
		/// After you call this function, SoftwareLatency is set to the correct
		/// value.
		/// The next thing to do is call Start().
		/// If this function returns an error, the instream is in an invalid state and
		/// you must call Dispose().
		///
		/// Possible errors:
		/// * Error.Invalid
		///   	* device aim is not DeviceAim.Input
		///   	* format is not valid
		///   	* requested layout channel count > 24 Channels
		/// * Error.OpeningDevice
		/// * Error.NoMem
		/// * Error.BackendDisconnected
		/// * Error.SystemResources
		/// * Error.NoSuchClient
		/// * Error.IncompatibleBackend
		/// * Error.IncompatibleDevice
		public Error Open()
		{
			Marshal.StructureToPtr<InStreamNative> (nativeStruct, nativePtr, false);

			var ret = soundio_instream_open (nativePtr);

			var soundIoInOutStreamNative = (InStreamNative) Marshal.PtrToStructure(nativePtr, typeof(InStreamNative));
			nativeStruct = soundIoInOutStreamNative;

			return ret;
		}

		[DllImport (SoundIO.dllName)]
		private static extern Error soundio_instream_start(IntPtr stream);
		/// After you call this function, OnRead(...) will be called.
		///
		/// Possible errors:
		/// * Error.BackendDisconnected
		/// * Error.Streaming
		/// * Error.OpeningDevice
		/// * Error.SystemResources
		public Error Start()
		{
			Marshal.StructureToPtr<InStreamNative> (nativeStruct, nativePtr, false);

			return soundio_instream_start (nativePtr);
		}

		[DllImport (SoundIO.dllName)]
		private static extern Error soundio_instream_begin_read(IntPtr stream, out IntPtr areas, ref int frame_count);
		/// Call this function when you are ready to begin reading from the device
		/// buffer.
		/// * `area` - (out) The ChannelArrea andmemory addresses you can read data from. It is OK
		///   to modify the pointers if that helps you iterate. There might be a "hole"
		///   in the buffer. To indicate this, `areas` will be `NULL` and `frame_count`
		///   tells how big the hole is in frames. TODO: fix this on managed side.
		/// * `frameCount` - (in/out) - Provide the number of frames you want to read;
		///   returns the number of frames you can actually read. The returned value
		///   will always be less than or equal to the provided value. If the provided
		///   value is less than `frameCountMin` from OnRead(...) this function
		///   returns with Error.Invalid.
		/// It is your responsibility to call this function no more and no fewer than the
		/// correct number of times according to the `frameCountMin` and
		/// `frameCountMax` criteria from OnRead(...).
		/// You must call this function only from the OnRead(...) thread context.
		/// After calling this function, read data from `areas` and then use
		/// EndRead() to actually remove the data from the buffer
		/// and move the read index forward. EndRead() should not be
		/// called if the buffer is empty (frameCount == 0), but it should be called
		/// if there is a hole.
		///
		/// Possible errors:
		/// * Error.Invalid
		///   * `frameCount` < `frameCountMin` or `frameCount` > `frameCountMax`
		/// * Error.Streaming
		/// * Error.IncompatibleDevice - in rare cases it might just now
		///   be discovered that the device uses non-byte-aligned access, in which
		///   case this error code is returned.
		public Error BeginRead(out ChannelArea area, ref int frameCount)
		{
			IntPtr ptr;

			var ret = soundio_instream_begin_read(nativePtr, out ptr, ref frameCount);

			if (ptr != IntPtr.Zero) {
				var soundIoChannelAreaNative = (ChannelAreaNative)Marshal.PtrToStructure (ptr, typeof(ChannelAreaNative));
				area = new ChannelArea (ptr, soundIoChannelAreaNative);
			} else {
				area = null;
			}

			//Marshal.StructureToPtr<SoundIoInOutStreamNative> (stream.nativeStruct, stream.nativePtr, false);

			return ret;
		}

		[DllImport (SoundIO.dllName)]
		private static extern Error soundio_instream_end_read(IntPtr stream);
		/// This will drop all of the frames from when you called BeginRead().
		/// You must call this function only from the OnRead(...) thread context.
		/// You must call this function only after a successful call to
		/// BeginRead().
		///
		/// Possible errors:
		/// * Error.Streaming
		public Error EndRead()
		{
			return soundio_instream_end_read (nativePtr);
		}
			
		[DllImport (SoundIO.dllName)]
		private static extern Error soundio_instream_pause(IntPtr stream, bool pause);
		/// If the underyling device supports pausing, this pauses the stream and
		/// prevents OnRead(...) from being called. Otherwise this returns
		/// Error.IncompatibleDevice.
		/// This function may be called from any thread.
		/// Pausing when already paused or unpausing when already unpaused has no
		/// effect and always returns Error.None.
		///
		/// Possible errors:
		/// * Error.BackendDisconnected
		/// * Error.Streaming
		/// * Error.IncompatibleDevice - device does not support pausing/unpausing
		public Error Pause(bool pause)
		{
			return soundio_instream_pause (nativePtr, pause);
		}

		[DllImport (SoundIO.dllName)]
		private static extern Error soundio_instream_get_latency(IntPtr stream, out double out_latency);
		/// Obtain the number of seconds that the next frame of sound being
		/// captured will take to arrive in the buffer, plus the amount of time that is
		/// represented in the buffer. This includes both software and hardware latency.
		///
		/// This function must be called only from within OnRead(...).
		///
		/// Possible errors:
		/// * Error.Streaming
		public Error GetLatency(out double latency)
		{
			return soundio_instream_get_latency (nativePtr, out latency);
		}
	}
}

