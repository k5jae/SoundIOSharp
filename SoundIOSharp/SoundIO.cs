//
// SoundIO.cs
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
using SoundIOSharp.Utils;


namespace SoundIOSharp
{
	public partial class SoundIO : IDisposable
	{
		internal const string dllName = "libsoundio.dll";
		static SoundIO() {
			// take care of 32/64 bit native windows dll
			Library.LoadLibrary(dllName);
		}
	
		volatile bool connected = false;
		bool disposed = false;

		IntPtr soundIOStructNativePtr;
		SoundIONative soundIOStructNative;

		internal event EventHandler ShutdownSoundIO;


		/// <summary>
		/// Gets the current backend.
		/// </summary>
		/// <value>The current backend.</value>
		public Backend CurrentBackend {
			get {
				return soundIOStructNative.current_backend;
			}
		}

		[DllImport (dllName)]
		//[return: MarshalAs(UnmanagedType.LPStr)]
		private static extern IntPtr soundio_version_string();
		/// <summary>
		/// Gets the version.
		/// </summary>
		/// <value>The version.</value>
		public static Version NativeVersion {
			get {
				var ptr = soundio_version_string ();
				return new Version(Marshal.PtrToStringAnsi(ptr));
			}
		}

		public static Version ManagedVersion {
			get {
				return Library.GetVersion();
			}
		}

		[DllImport (dllName)]
		private static extern int soundio_version_major();
		/// <summary>
		/// Gets the version major.
		/// </summary>
		/// <value>The version major.</value>
		public static int VersionMajor {
			get {
				return soundio_version_major ();
			}
		}

		[DllImport (dllName)]
		private static extern int soundio_version_minor();
		/// <summary>
		/// Gets the version minor.
		/// </summary>
		/// <value>The version minor.</value>
		public static int VersionMinor {
			get {
				return soundio_version_minor ();
			}
		}

		[DllImport (dllName)]
		private static extern int soundio_version_patch();
		/// <summary>
		/// Gets the version patch.
		/// </summary>
		/// <value>The version patch.</value>
		public static int VersionPatch {
			get {
				return soundio_version_patch ();
			}
		}

		/// Create a SoundIO context. You may create multiple instances of this to connect to multiple backends. Sets all fields to defaults.
		/// throws OutOfMemoryException if memory could not be allocated.
		public SoundIO ()
		{
			soundIOStructNative = Create ();
		}

		[DllImport (dllName, EntryPoint = "soundio_create")]
		private static extern IntPtr soundio_create();

		private SoundIONative Create() {
			soundIOStructNativePtr = soundio_create();

			if (soundIOStructNativePtr == IntPtr.Zero) {
				throw new OutOfMemoryException ();
			}

			SoundIONative sound = (SoundIONative) Marshal.PtrToStructure(soundIOStructNativePtr, typeof(SoundIONative));
			sound.on_devices_change = new on_soundio_delegate_native (on_devices_change_native);
			sound.on_backend_disconnect = new on_backend_disconnect_delegate_native (on_backend_disconnect_native);
			sound.on_events_signal = new on_soundio_delegate_native (on_event_signal_native);

			Marshal.StructureToPtr<SoundIONative> (sound, soundIOStructNativePtr, false);

			sound = (SoundIONative) Marshal.PtrToStructure(soundIOStructNativePtr, typeof(SoundIONative));

			return sound;
		}

		private void Dispose(bool disposing)
		{
			if(!this.disposed)
			{
				if(disposing)
				{
					// Dispose here any managed resources
				}

				if (ShutdownSoundIO != null) {
					ShutdownSoundIO (this, new EventArgs ());
				}

				Disconnect ();
				// Dispose here any unmanaged resources
				Destroy ();
			}
			disposed = true;         
		}

		/// <summary>
		/// Disposes hardware resources.
		/// </summary>
		/// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="SoundIOSharp.SoundIO"/>. The
		/// <see cref="Dispose"/> method leaves the <see cref="SoundIOSharp.SoundIO"/> in an unusable state. After calling
		/// <see cref="Dispose"/>, you must release all references to the <see cref="SoundIOSharp.SoundIO"/> so the garbage
		/// collector can reclaim the memory that the <see cref="SoundIOSharp.SoundIO"/> was occupying.</remarks>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		~SoundIO() {
			Dispose(false);
		}

		[DllImport (dllName)]
		private static extern void soundio_destroy(IntPtr soundio);
		private void Destroy() {
			soundio_destroy(soundIOStructNativePtr);
			soundIOStructNativePtr = IntPtr.Zero;
			soundIOStructNative = null;
		}

		[DllImport (dllName)]
		private static extern IntPtr soundio_strerror(Error error);
		/// Get a string representation of an Error
		public static string ErrorString(Error error)
		{
			return Marshal.PtrToStringAnsi(soundio_strerror(error));
		}

		[DllImport (dllName)]
		private static extern Error soundio_connect(IntPtr soundio);
		/// Tries ConnectBackend() on all available backends in order.
		/// Possible errors:
		/// * Error.Invalid - already connected
		/// * Error.NoMem
		/// * Error.SystemResources
		/// * Error.NoSuchClient - when JACK returns `JackNoSuchClient`
		/// See also Disconnect()
		public Error Connect()
		{
			var ret = soundio_connect (soundIOStructNativePtr);

			if (ret == Error.None) {
				connected = true;
				soundIOStructNative = (SoundIONative)Marshal.PtrToStructure (soundIOStructNativePtr, typeof(SoundIONative));
			}

			return ret;
		}

		[DllImport (dllName)]
		private static extern Error soundio_connect_backend(IntPtr soundio, Backend backend);
		/// Instead of calling Connect() you may call this function to try a
		/// specific backend.
		/// Possible errors:
		/// * Error.Invalid - already connected or invalid backend parameter
		/// * Error.NoMem
		/// * Error.BackendUnavailable - backend was not compiled in
		/// * Error.SystemResources
		/// * Error.NoSuchClient - when JACK returns `JackNoSuchClient`
		/// * Error.InitAudioBackend - requested `backend` is not active
		/// * Error.BackendDisconnected - backend disconnected while connecting
		/// See also Disconnect()
		public Error ConnectBackend(Backend backend)
		{
			if (disposed) {
				throw new ObjectDisposedException ("SoundIO");
			}

			var ret = soundio_connect_backend (soundIOStructNativePtr, backend);

			if (ret == Error.None) {
				connected = true;
				soundIOStructNative = (SoundIONative)Marshal.PtrToStructure (soundIOStructNativePtr, typeof(SoundIONative));
			}

			return ret;
		}

		[DllImport (dllName)]
		private static extern void soundio_disconnect(IntPtr soundio);
		/// Disconnects from a connected backend.
		public void Disconnect()
		{
			if (disposed) {
				throw new ObjectDisposedException ("SoundIO");
			}

			if (connected) {
				if (soundIOStructNativePtr != IntPtr.Zero)
					try {
						soundio_disconnect(soundIOStructNativePtr);
					} catch (SEHException) {
					}
				connected = false;
			}
		}

		[DllImport (dllName)]
		private static extern IntPtr soundio_backend_name(Backend backend);
		/// Get a string representation of a Backend
		public string BackendName(Backend backend)
		{
			if (disposed) {
				throw new ObjectDisposedException ("SoundIO");
			}

			return Marshal.PtrToStringAnsi(soundio_backend_name(backend));
		}

		[DllImport (dllName)]
		private static extern int soundio_backend_count(IntPtr soundio);
		/// Returns the number of available backends.
		public int BackendCount {
			get {
				if (disposed) {
					throw new ObjectDisposedException ("SoundIO");
				}

				return soundio_backend_count (soundIOStructNativePtr);
			}
		}

		[DllImport (dllName)]
		private static extern Backend soundio_get_backend(IntPtr soundio, int index);
		/// get the available backend at the specified index (0 <= index < BackendCount)
		public Backend GetBackend(int index)
		{
			if (disposed) {
				throw new ObjectDisposedException ("SoundIO");
			}

			return soundio_get_backend(soundIOStructNativePtr, index);
		}

		[DllImport (dllName)]
		private static extern bool soundio_have_backend(Backend backend);
		/// Returns whether libsoundio was compiled with backend.
		public bool HaveBackend(Backend backend)
		{
			if (disposed) {
				throw new ObjectDisposedException ("SoundIO");
			}

			return soundio_have_backend(backend);
		}


		[DllImport (dllName)]
		private static extern void soundio_flush_events(IntPtr soundio);
		/// <summary>
		/// <para>
		/// Atomically update information for all connected devices. Note that calling
		/// this function merely flips a pointer; the actual work of collecting device
		/// information is done elsewhere. It is performant to call this function many
		/// times per second. </para>
		/// <para></para>
		/// <para>When you call this, the following callbacks might be called:</para>
		/// <para>* OnDevicesChange</para>
		/// <para>* OnBackendDisconnect</para>
		/// <para>This is the only time those callbacks can be called.</para>
		/// <para></para>
		/// <para>This must be called from the same thread as the thread in which you call
		/// these functions:</para>
		/// <para>* InputDeviceCount</para>
		/// <para>* OutputDeviceCount</para>
		/// <para>* GetInputDevice</para>
		/// <para>* GetOutputDevice</para>
		/// <para>* DefaultInputDeviceIndex</para>
		/// <para>* DefaultOutputDeviceIndex</para>
		/// <para></para>
		/// <para>Note that if you do not care about learning about updated devices, you
		/// might call this function only once ever and never call WaitEvents.</para>
		/// </summary>
		/// <returns>Error Code.</returns>
		public Error FlushEvents()
		{
			if (disposed) {
				throw new ObjectDisposedException ("SoundIO");
			}

			if (CurrentBackend != Backend.None) {
				soundio_flush_events (soundIOStructNativePtr);
				return Error.None;
			}
			else
				return Error.BackendDisconnected;
		}

		[DllImport (dllName)]
		private static extern void soundio_wait_events(IntPtr soundio);
		/// This function calls FlushEvents() then blocks until another event
		/// is ready or you call Wakeup(). Be ready for spurious wakeups.
		public void WaitEvents()
		{
			if (disposed) {
				throw new ObjectDisposedException ("SoundIO");
			}

			soundio_wait_events(soundIOStructNativePtr);
		}

		[DllImport (dllName)]
		private static extern void soundio_wakeup(IntPtr soundio);
		/// Makes WaitEvents(...) stop blocking.
		public void Wakeup()
		{
			if (disposed) {
				throw new ObjectDisposedException ("SoundIO");
			}

			soundio_wakeup(soundIOStructNativePtr);
		}

		[DllImport (dllName)]
		private static extern void soundio_force_device_scan(IntPtr soundio);
		/// If necessary you can manually trigger a device rescan. Normally you will
		/// not ever have to call this function, as libsoundio listens to system events
		/// for device changes and responds to them by rescanning devices and preparing
		/// the new device information for you to be atomically replaced when you call
		/// FlushEvents. However you might run into cases where you want to
		/// force trigger a device rescan, for example if an ALSA device has a
		/// ProbeError.
		///
		/// After you call this you still have to use FlushEvents or WaitEvents
		/// and then wait for the OnDevicesChanged.
		///
		/// This can be called from any thread context except for
		/// OnWrite(...) and OnRead(...)
		public void ForceDeviceScan()
		{
			if (disposed) {
				throw new ObjectDisposedException ("SoundIO");
			}

			soundio_force_device_scan(soundIOStructNativePtr);
		}

		// channel layouts
		[DllImport (dllName)]
		private static extern bool soundio_channel_layout_equal(
			ChannelLayout a,
			ChannelLayout b);
		/// Returns whether the channel count field and each channel id matches in
		/// the supplied channel layouts.
		public bool ChannelLayoutEqual(ChannelLayout a, ChannelLayout b)
		{
			if (disposed) {
				throw new ObjectDisposedException ("SoundIO");
			}

			return soundio_channel_layout_equal(a, b);
		}

		[DllImport (dllName)]
		private static extern IntPtr soundio_get_channel_name(Channels id);
		/// Returns channel name for Channel id
		public static string GetChannelName(Channels id)
		{
			return Marshal.PtrToStringAnsi(soundio_get_channel_name(id));
		}

		[DllImport (dllName, CharSet = CharSet.Unicode)]
		private static extern Channels soundio_parse_channel_id(string str, int str_len);
		/// Given UTF-8 encoded text which is the name of a channel such as
		/// "Front Left", "FL", or "front-left", return the corresponding
		/// Channels. Returns Channels.Invalid for no match.
		public Channels ParseChannelId(string str)
		{
			return soundio_parse_channel_id(str, str.Length);
		}

		[DllImport (dllName)]
		private static extern int soundio_channel_layout_builtin_count();
		/// Returns the number of builtin channel layouts.
		public int ChannelLayoutBuiltinCount()
		{
			return soundio_channel_layout_builtin_count ();
		}

		[DllImport (dllName)]
		private static extern ChannelLayout soundio_channel_layout_get_builtin(int index);
		/// Returns a builtin channel layout. 0 <= `index` < ChannelLayoutBuiltinCount
		///
		/// Although `index` is of type `int`, it should be a valid
		/// ChannelLayouts enum value.
		public ChannelLayout ChannelLayoutGetBuiltin(int index)
		{
			return soundio_channel_layout_get_builtin (index);
		}

		[DllImport (dllName)]
		private static extern ChannelLayout soundio_channel_layout_get_default(int channel_count);
		/// Get the default builtin channel layout for the given number of channels.
		public ChannelLayout ChannelLayoutGetDefault(int channelCount)
		{
			return soundio_channel_layout_get_default (channelCount);
		}

		[DllImport (dllName)]
		private static extern int soundio_channel_layout_find_channel(ChannelLayout layout, Channels channel);
		/// Return the index of `channel` in `layout`, or `-1` if not found.
		public int ChannelLayoutFindChannel(ChannelLayout layout, Channels channel)
		{
			return soundio_channel_layout_find_channel (layout, channel);
		}

		[DllImport (dllName)]
		private static extern bool soundio_channel_layout_detect_builtin(ChannelLayout layout);
		/// Populates the name field of layout if it matches a builtin one.
		/// returns whether it found a match
		public bool ChannelLayoutDetectBuiltin(ChannelLayout layout)
		{
			return soundio_channel_layout_detect_builtin (layout);
		}

		[DllImport (dllName)]
		private static extern ChannelLayout soundio_best_matching_channel_layout(
			ChannelLayout[] preferred_layouts, int preferred_layout_count,
			ChannelLayout[] available_layouts, int available_layout_count);
		/// Iterates over preferred_layouts. Returns the first channel layout in
		/// preferred_layouts which matches one of the channel layouts in
		/// available_layouts. Returns NULL if none matches.
		public ChannelLayout BestMatchingChannelLayout(ChannelLayout[] preferred_layouts, ChannelLayout[] available_layouts)
		{
			return soundio_best_matching_channel_layout (preferred_layouts, preferred_layouts.Length, available_layouts, available_layouts.Length);
		}		

		[DllImport (dllName)]
		private static extern void soundio_sort_channel_layouts(ChannelLayout[] layouts, int layout_count);
		/// Sorts by channel count, descending.
		public void SortChannelLayouts(ChannelLayout[] layouts)
		{
			soundio_sort_channel_layouts (layouts, layouts.Length);
		}		

		// sample formats

		[DllImport (dllName)]
		private static extern int soundio_get_bytes_per_sample(Format format);
		/// Returns the number of bytes per sample.
		public int GetBytesPerSample(Format format)
		{
			return soundio_get_bytes_per_sample (format);
		}

		/// A frame is one sample per channel.
		public int GetBytesPerFrame(Format format, int channelCount)
		{
			return GetBytesPerSample(format) * channelCount;
		}

		/// Sample rate is the number of frames per second.
		public int GetBytesPerSecond(Format format,	int channelCount, int sampleRate)
		{
			return GetBytesPerFrame(format, channelCount) * sampleRate;
		}

		[DllImport (dllName)]
		private static extern IntPtr soundio_format_string(Format format);
		/// Returns string representation of format.
		public static string FormatString(Format format)
		{
			return Marshal.PtrToStringAnsi(soundio_format_string(format));
		}

	}
}

