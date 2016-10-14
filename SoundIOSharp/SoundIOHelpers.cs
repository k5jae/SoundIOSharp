//
// SoundIOHelpers.cs
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
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SoundIOSharp
{
	public partial class SoundIO : IDisposable
	{
		public Device GetOutputDevice(string id)
		{
			if (id == null || id == string.Empty)
				return null;

			var count = OutputDeviceCount ();
			for (int i = 0; i < count; i += 1) {

				Device device = null;
				bool keepDevice = false;
				try {
					device = GetOutputDevice(i);

					if (device.Id == id) {
						keepDevice = true;
						return device;
					}
				}
				finally {
					if (!keepDevice)
						device.Dispose ();
				}
			}
			return null;
		}

		public Device[] FindOutputDevices(string name, bool raw = false)
		{
			if (name == null || name == string.Empty)
				return null;

			var devices = new List<Device> ();
			var count = OutputDeviceCount ();

			for (int i = 0; i < count; i += 1) {

				Device device = null;
				bool keepDevice = false;
				try {
					device = GetOutputDevice(i);

					if (device.Name.Contains(name) && device.IsRaw == raw) {
						keepDevice = true;
						devices.Add(device);
						device = null;
					}
				}
				finally {
					if (!keepDevice && device != null)
						device.Dispose ();
				}
			}
			return devices.ToArray();
		}

		public Device GetInputDevice(string id)
		{
			if (id == null || id == string.Empty)
				return null;

			var count = InputDeviceCount ();
			for (int i = 0; i < count; i += 1) {

				Device device = null;
				bool keepDevice = false;
				try {
					device = GetInputDevice(i);

					if (device.Id == id) {
						keepDevice = true;
						return device;
					}
				}
				finally {
					if (!keepDevice)
						device.Dispose ();
				}
			}
			return null;
		}

		public Device[] FindInputDevices(string name, bool raw = false)
		{
			if (name == null || name == string.Empty)
				return null;

			var devices = new List<Device> ();
			var count = InputDeviceCount ();

			for (int i = 0; i < count; i += 1) {

				Device device = null;
				bool keepDevice = false;
				try {
					device = GetInputDevice(i);

					if (device.Name.Contains(name) && device.IsRaw == raw) {
						keepDevice = true;
						devices.Add(device);
						device = null;
					}
				}
				finally {
					if (!keepDevice && device != null)
						device.Dispose ();
				}
			}
			return devices.ToArray();
		}
	}
}

