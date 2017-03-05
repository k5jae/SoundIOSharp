//
// SoundIODevices.cs
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
		// devices

		[DllImport (dllName)]
		private static extern int soundio_input_device_count(IntPtr soundio);
		public int InputDeviceCount()
		{
			if (disposed) {
				throw new ObjectDisposedException ("SoundIO");
			}

			return soundio_input_device_count (soundIOStructNativePtr);
		}

		[DllImport (dllName)]
		private static extern int soundio_output_device_count(IntPtr soundio);
		public int OutputDeviceCount()
		{
			if (disposed) {
				throw new ObjectDisposedException ("SoundIO");
			}

			if (CurrentBackend != Backend.None) {
				return soundio_output_device_count (soundIOStructNativePtr);
			}
			else
				return -1;
		}

		[DllImport (dllName)]
		private static extern IntPtr soundio_get_input_device(IntPtr soundio, int index);
		public Device GetInputDevice(int index)
		{
			if (disposed) {
				throw new ObjectDisposedException ("SoundIO");
			}

			var soundIoDevicePtr = soundio_get_input_device(soundIOStructNativePtr, index);
			var soundIoDevice = new Device (this, soundIoDevicePtr);

			return soundIoDevice;
		}

		[DllImport (dllName)]
		private static extern IntPtr soundio_get_output_device(IntPtr soundio, int index);
		public Device GetOutputDevice(int index)
		{
			if (disposed) {
				throw new ObjectDisposedException ("SoundIO");
			}

			var soundIoDevicePtr = soundio_get_output_device(soundIOStructNativePtr, index);
			var soundIoDevice = new Device (this, soundIoDevicePtr);

			return soundIoDevice;
		}

		[DllImport (dllName)]
		private static extern int soundio_default_input_device_index(IntPtr soundio);
		public int DefaultInputDeviceIndex()
		{
			if (disposed) {
				throw new ObjectDisposedException ("SoundIO");
			}

			return soundio_default_input_device_index (soundIOStructNativePtr);
		}


		[DllImport (dllName)]
		private static extern int soundio_default_output_device_index(IntPtr soundio);
		public int DefaultOutputDeviceIndex()
		{
			if (disposed) {
				throw new ObjectDisposedException ("SoundIO");
			}

			if (CurrentBackend != Backend.None) {
				return soundio_default_output_device_index (soundIOStructNativePtr);
			}
			else
				return -1;			
		}

		[DllImport (dllName)]
		private static extern bool soundio_device_equal(IntPtr a, IntPtr b);
		public void DeviceEqual(Device a, Device b)
		{
			if (disposed) {
				throw new ObjectDisposedException ("SoundIO");
			}

			soundio_device_equal(a.nativePtr, b.nativePtr);
		}

		[DllImport (dllName)]
		private static extern void soundio_device_sort_channel_layouts(IntPtr device);
		public void DeviceSortChannelLayouts(Device device)
		{
			if (disposed) {
				throw new ObjectDisposedException ("SoundIO");
			}

			soundio_device_sort_channel_layouts(device.nativePtr);
		}

		[DllImport (dllName)]
		private static extern bool soundio_device_supports_format(IntPtr device, Format format);
		public bool DeviceSupportsFormat(Device device, Format format)
		{
			if (disposed) {
				throw new ObjectDisposedException ("SoundIO");
			}

			return soundio_device_supports_format(device.nativePtr, format);
		}

		[DllImport (dllName)]
		private static extern bool soundio_device_supports_layout(IntPtr device, ChannelLayout layout);
		public bool DeviceSupportsLayout(Device device, ChannelLayout layout)
		{
			if (disposed) {
				throw new ObjectDisposedException ("SoundIO");
			}

			return soundio_device_supports_layout(device.nativePtr, layout);
		}

		[DllImport (dllName)]
		private static extern bool soundio_device_supports_sample_rate(IntPtr device, int sample_rate);
		public bool DeviceSupportsSampleRate(Device device, int sampleRate)
		{
			if (disposed) {
				throw new ObjectDisposedException ("SoundIO");
			}

			return soundio_device_supports_sample_rate(device.nativePtr, sampleRate);
		}

		[DllImport (dllName)]
		private static extern int soundio_device_nearest_sample_rate(IntPtr device,	int sample_rate);
		public int DeviceNearestSampleRate(Device device, int sampleRate)
		{
			if (disposed) {
				throw new ObjectDisposedException ("SoundIO");
			}

			return soundio_device_nearest_sample_rate(device.nativePtr, sampleRate);
		}
	}
}

