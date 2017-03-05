//
// OutStream.cs
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
	internal class OutStreamNative {

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

	public class OutStream : Stream
	{

		internal IntPtr nativePtr;
		internal OutStreamNative nativeStruct;

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
		/// Delegate that gets called when the stream is ready for writing.
		/// </summary>
		public OnWriteDelegate OnWrite;

		/// <summary>
		/// Delegate that gets called when the stream is underflowing hardware.
		/// </summary>
		public OnUnderFlowDelegate OnUnderflow;

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
		internal OutStream(IntPtr nativePtr, OutStreamNative nativeStruct)
		{
			this.nativePtr = nativePtr;
			this.nativeStruct = nativeStruct;

			this.nativeStruct.on_readorwrite = OnReadWriteNative;
			this.nativeStruct.on_underflow = OnUnderflowNative;
			this.nativeStruct.on_error = OnErrorNative;
		}

		// internal managed wrapper around native callback
		internal void OnReadWriteNative(IntPtr stream, int frame_count_min, int frame_count_max)
		{
			if (OnWrite != null) {
				OnWrite (this, frame_count_min, frame_count_max);
			}
		}

		// internal managed wrapper around native callback
		internal void OnUnderflowNative(IntPtr stream)
		{
			if (OnUnderflow != null) {
				OnUnderflow (this);
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
		/// Copies from buffer array to ChannelArea.
		/// </summary>
		/// <param name="buffer">Buffer.</param>
		/// <param name="bufferPos">Buffer position.</param>
		/// <param name="area">ChannelArea</param>
		/// <param name="bufferCount">Number of buffer samples</param>
		public void CopyTo(float[] buffer, int bufferPos, ChannelArea area, int bufferCount)
		{
			Marshal.Copy (buffer, bufferPos, area.NativeBufferPtr, bufferCount);
		}

		public static void WriteSampleS16NE(double inSample, out short outSample) {
			int range = Int16.MaxValue - Int16.MinValue;
			outSample = (short)(inSample * range / 2.0);
		}

		public static void WriteSampleS32NE(double inSample, out Int32 outSample) {
			long range = (long)Int32.MaxValue - (long)Int32.MinValue;
			outSample = (Int32)(inSample * range / 2.0);
		}

		public static void WriteSampleFloat32NE(double inSample, out Single outSample) {
			outSample = (Single)inSample;
		}

		public static void WriteSampleFloat64NE(double inSample, out Double outSample) {
			outSample = (Double)inSample;
		}

		[DllImport (SoundIO.dllName)]
		private static extern void soundio_outstream_destroy(IntPtr outstream);
		protected override void Destroy()
		{
			if (disposed) {
				throw new ObjectDisposedException ("OutStream");
			}

			try
			{
				soundio_outstream_destroy(nativePtr);
			}
			// TODO: Native code throws this when Open() fails. Determine what needs to be done.
			catch (AccessViolationException) {}
			nativePtr = IntPtr.Zero;
			nativeStruct = null;
		}

		[DllImport (SoundIO.dllName)]
		private static extern Error soundio_outstream_open(IntPtr outstream);
		/// After you call this function, SoftwareLatency is set to
		/// the correct value.
		///
		/// The next thing to do is call Start().
		/// If this function returns an error, the outstream is in an invalid state and
		/// you must call Dispose() on it.
		///
		/// Possible errors:
		/// * #SoundIoErrorInvalid
		///   * SoundIoDevice::aim is not #SoundIoDeviceAimOutput
		///   * SoundIoOutStream::format is not valid
		///   * SoundIoOutStream::channel_count is greater than #SOUNDIO_MAX_CHANNELS
		/// * #SoundIoErrorNoMem
		/// * #SoundIoErrorOpeningDevice
		/// * #SoundIoErrorBackendDisconnected
		/// * #SoundIoErrorSystemResources
		/// * #SoundIoErrorNoSuchClient - when JACK returns `JackNoSuchClient`
		/// * #SoundIoErrorOpeningDevice
		/// * #SoundIoErrorIncompatibleBackend - SoundIoOutStream::channel_count is
		///   greater than the number of channels the backend can handle.
		/// * #SoundIoErrorIncompatibleDevice - stream parameters requested are not
		///   compatible with the chosen device.
		public Error Open()
		{
			if (disposed) {
				throw new ObjectDisposedException ("OutStream");
			}

			Marshal.StructureToPtr<OutStreamNative> (nativeStruct, nativePtr, false);

			var ret = soundio_outstream_open (nativePtr);

			var soundIoInOutStreamNative = (OutStreamNative) Marshal.PtrToStructure(nativePtr, typeof(OutStreamNative));
			nativeStruct = soundIoInOutStreamNative;

			return ret;
		}

		[DllImport (SoundIO.dllName)]
		private static extern Error soundio_outstream_start(IntPtr outstream);
		/// After you call this function, OnWrite(...) will be called.
		///
		/// This function might directly call OnWrite(...).
		///
		/// Possible errors:
		/// * Error.Streaming
		/// * Error.NoMem
		/// * Error.SystemResources
		/// * Error.BackendDisconnected
		public Error Start()
		{
			if (disposed) {
				throw new ObjectDisposedException ("OutStream");
			}

			Marshal.StructureToPtr<OutStreamNative> (nativeStruct, nativePtr, false);

			return soundio_outstream_start (nativePtr);
		}

		[DllImport (SoundIO.dllName)]
		private static extern Error soundio_outstream_begin_write(IntPtr outstream, out IntPtr areas, ref int frame_count);
		/// Call this function when you are ready to begin writing to the device buffer.
		///  * `area` - (out) The ChannelArrea and memory addresses you can write data to, one per
		///    channel. It is OK to modify the pointers if that helps you iterate.
		///  * `frameCount` - (in/out) Provide the number of frames you want to write.
		///    Returned will be the number of frames you can actually write, which is
		///    also the number of frames that will be written when you call
		///    EndWrite(). The value returned will always be less
		///    than or equal to the value provided.
		/// It is your responsibility to call this function exactly as many times as
		/// necessary to meet the `frameCountMin` and `frameCountMax` criteria from
		/// OnWrite(...).
		/// You must call this function only from the OnWrite(...) thread context.
		/// After calling this function, write data to ChannelArea and then call
		/// EndWrite().
		/// If this function returns an error, do not call EndWrite().
		///
		/// Possible errors:
		/// * Error.Invalid
		///   * `frameCount` <= 0
		///   * `frameCount` < `frameCountMin` or `frameCount` > `frameCountMax`
		///   * function called too many times without respecting `frameCountMax`
		/// * Error.Streaming
		/// * Error.Underflow - an underflow caused this call to fail. You might
		///   also get an OnUnderflow(...), and you might not get
		///   this error code when an underflow occurs. Unlike Error.Streaming,
		///   the outstream is still in a valid state and streaming can continue.
		/// * Error.IncompatibleDevice - in rare cases it might just now
		///   be discovered that the device uses non-byte-aligned access, in which
		///   case this error code is returned.
		public Error BeginWrite(out ChannelArea area, ref int frameCount)
		{
			if (disposed) {
				throw new ObjectDisposedException ("OutStream");
			}

			IntPtr ptr;

			var ret = soundio_outstream_begin_write(nativePtr, out ptr, ref frameCount);

			var soundIoChannelAreaNative = (ChannelAreaNative) Marshal.PtrToStructure(ptr, typeof(ChannelAreaNative));

			area = new ChannelArea (ptr, soundIoChannelAreaNative);


			//Marshal.StructureToPtr<SoundIoInOutStreamNative> (stream.nativeStruct, stream.nativePtr, false);

			return ret;
		}

		[DllImport (SoundIO.dllName)]
		private static extern Error soundio_outstream_end_write(IntPtr outstream);
		/// Commits the write that you began with BeginWrite(...).
		/// You must call this function only from the OnWrite(...) thread context.
		///
		/// Possible errors:
		/// * Error.Streaming
		/// * Error.Underflow - an underflow caused this call to fail. You might
		///   also get an OnUnderflow(), and you might not get
		///   this error code when an underflow occurs. Unlike Error.Streaming,
		///   the outstream is still in a valid state and streaming can continue.
		public Error EndWrite()
		{
			if (disposed) {
				throw new ObjectDisposedException ("OutStream");
			}

			return soundio_outstream_end_write (nativePtr);
		}

		[DllImport (SoundIO.dllName)]
		private static extern Error soundio_outstream_clear_buffer(IntPtr outstream);
		/// Clears the output stream buffer.
		/// This function can be called from any thread.
		/// This function can be called regardless of whether the OutStream is paused
		/// or not.
		/// Some backends do not support clearing the buffer. On these backends this
		/// function will return Error.IncompatibleBackend.
		/// Some devices do not support clearing the buffer. On these devices this
		/// function might return Error.IncompatibleDevice.
		/// Possible errors:
		///
		/// * Error.Streaming
		/// * Error.IncompatibleBackend
		/// * Error.IncompatibleDevice
		public Error ClearBuffer()
		{
			if (disposed) {
				throw new ObjectDisposedException ("OutStream");
			}

			return soundio_outstream_clear_buffer (nativePtr);
		}

		[DllImport (SoundIO.dllName)]
		private static extern Error soundio_outstream_pause(IntPtr outstream, bool pause);
		/// If the underlying backend and device support pausing, this pauses the
		/// stream. OnWrite(...) may be called a few more times if
		/// the buffer is not full.
		/// Pausing might put the hardware into a low power state which is ideal if your
		/// software is silent for some time.
		/// This function may be called from any thread context, including OnWrite(...)
		/// Pausing when already paused or unpausing when already unpaused has no
		/// effect and returns Error.None.
		///
		/// Possible errors:
		/// * Error.BackendDisconnected
		/// * Error.Streaming
		/// * Error.IncompatibleDevice - device does not support
		///   pausing/unpausing. This error code might not be returned even if the
		///   device does not support pausing/unpausing.
		/// * Error.IncompatibleBackend - backend does not support
		///   pausing/unpausing.
		/// * Error.Invalid - outstream not opened and started
		public Error Pause(bool pause)
		{
			if (disposed) {
				throw new ObjectDisposedException ("OutStream");
			}

			return soundio_outstream_pause (nativePtr, pause);
		}

		[DllImport (SoundIO.dllName)]
		private static extern Error soundio_outstream_get_latency(IntPtr outstream, out double out_latency);
		/// Obtain the total number of seconds that the next frame written after the
		/// last frame written with EndWrite() will take to become
		/// audible. This includes both software and hardware latency. In other words,
		/// if you call this function directly after calling EndWrite(),
		/// this gives you the number of seconds that the next frame written will take
		/// to become audible.
		///
		/// This function must be called only from within OnWrite(...).
		///
		/// Possible errors:
		/// Error.Streaming
		public Error GetLatency(out double latency)
		{
			if (disposed) {
				throw new ObjectDisposedException ("OutStream");
			}

			return soundio_outstream_get_latency (nativePtr, out latency);
		}
	}
}

