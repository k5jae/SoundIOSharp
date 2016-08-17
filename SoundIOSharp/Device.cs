//
// Device.cs
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
	internal class DeviceNative {
		IntPtr soundio;

		[MarshalAs (UnmanagedType.LPStr)]
		internal string id;

		[MarshalAs (UnmanagedType.LPStr)]
		internal string name;

		internal DeviceAim aim;

		internal IntPtr layouts;
		internal int layout_count;

		internal ChannelLayout current_layout;

		internal IntPtr formats;
		internal int format_count;

		internal Format current_format;

		internal IntPtr sample_rates;
		internal int sample_rate_count;

		internal int sample_rate_current;

		internal double software_latency_min;
		internal double software_latency_max;
		internal double software_latency_current;

		internal bool is_raw;

		internal int ref_count;

		internal Error probe_error;
	};

	public class Device : IDisposable
	{
		internal IntPtr nativePtr;
		internal DeviceNative nativeStruct;

		public string Id {
			get {
				return nativeStruct.id;
			}
		}

		public string Name {
			get {
				return nativeStruct.name;
			}
		}

		public DeviceAim Aim {
			get {
				return nativeStruct.aim;
			}
		}

		public ChannelLayout[] Layouts;

		public ChannelLayout CurrentLayout {
			get {
				return nativeStruct.current_layout;
			}
			set {
				nativeStruct.current_layout = value;
			}
		}

		public Format[] Formats;

		public Format CurrentFormat {
			get {
				return nativeStruct.current_format;
			}
			set {
				nativeStruct.current_format = value;
			}
		}

		public SampleRateRange[] SampleRates;

		public int CurrentSampleRate {
			get {
				return nativeStruct.sample_rate_current;
			}
		}

		public double SoftwareLatencyMin {
			get {
				return nativeStruct.software_latency_min;
			}
		}

		public double SoftwareLatencyMax {
			get {
				return nativeStruct.software_latency_max;
			}
		}

		public double SoftwareLatencyCurrent {
			get {
				return nativeStruct.software_latency_current;
			}
		}

		public bool IsRaw {
			get {
				return nativeStruct.is_raw;
			}
		}

		public Error ProbeError {
			get {
				return nativeStruct.probe_error;
			}
		}

		public string ProbeErrorStr {
			get {
				return SoundIO.ErrorString (ProbeError);
			}
		}

		internal Device(IntPtr nativePtr, DeviceNative nativeStruct)
		{
			this.nativePtr = nativePtr;
			this.nativeStruct = nativeStruct;

			IntPtr value = nativeStruct.layouts;
			Layouts = new ChannelLayout[nativeStruct.layout_count];
			for (int i = 0; i < Layouts.Length; i++)
			{
				Layouts[i] = (ChannelLayout) Marshal.PtrToStructure(value, typeof(ChannelLayout));
				value += Marshal.SizeOf<ChannelLayout>();
			}

			value = nativeStruct.formats;
			Formats = new Format[nativeStruct.format_count];
			for (int i = 0; i < Formats.Length; i++)
			{
				Formats[i] = (Format) Marshal.ReadInt32(value);
				value += sizeof(Format);
			}

			value = nativeStruct.sample_rates;
			SampleRates = new SampleRateRange[nativeStruct.sample_rate_count];
			for (int i = 0; i < SampleRates.Length; i++)
			{
				SampleRates[i] = (SampleRateRange) Marshal.PtrToStructure(value, typeof(SampleRateRange));
				value += Marshal.SizeOf<SampleRateRange>();
			}
		}

		private bool disposed = false;

		private void Dispose(bool disposing)
		{
			if(!this.disposed)
			{
				if(disposing)
				{
					// Dispose here any managed resources
				}

				// Dispose here any unmanaged resources
				DeviceUnref();
			}
			disposed = true;         
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		~Device() {
			Dispose(false);
		}

		[DllImport (SoundIO.dllName)]
		private static extern void soundio_device_ref(IntPtr device);
		internal void DeviceRef()
		{
			soundio_device_ref (nativePtr);
			nativeStruct = (DeviceNative) Marshal.PtrToStructure(nativePtr, typeof(DeviceNative));
		}

		[DllImport (SoundIO.dllName)]
		private static extern void soundio_device_unref(IntPtr device);
		internal void DeviceUnref()
		{
			nativeStruct = (DeviceNative)Marshal.PtrToStructure(nativePtr, typeof(DeviceNative)); 
			soundio_device_unref(nativePtr);
			if (nativeStruct.ref_count > 1) {
				nativeStruct = (DeviceNative)Marshal.PtrToStructure(nativePtr, typeof(DeviceNative));
			} else {
				nativeStruct = null;
				nativePtr = IntPtr.Zero;
			}
		}

		[DllImport (SoundIO.dllName)]
		private static extern IntPtr soundio_outstream_create(IntPtr device);
		public OutStream OutStreamCreate()
		{
			var streamPtr = soundio_outstream_create (nativePtr);
			var outStreamNative = (OutStreamNative) Marshal.PtrToStructure(streamPtr, typeof(OutStreamNative));
			var outStream = new OutStream (streamPtr, outStreamNative);

			//Marshal.StructureToPtr<SoundIoInOutStreamNative> (soundIoInOutStreamNative, streamPtr, false);

			return outStream;
		}

		[DllImport (SoundIO.dllName)]
		private static extern IntPtr soundio_instream_create(IntPtr device);
		public InStream InStreamCreate()
		{
			var streamPtr = soundio_instream_create (nativePtr);
			var inStreamNative = (InStreamNative) Marshal.PtrToStructure(streamPtr, typeof(InStreamNative));
			var inStream = new InStream (streamPtr, inStreamNative);

			return inStream;
		}

	};

}

