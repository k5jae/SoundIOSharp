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
using System.Runtime.InteropServices;


namespace SoundIOSharp
{
	public partial class SoundIO : IDisposable
	{
		public Device FindOutputDevice(string name)
		{
			return FindOutputDevice (name, false);
		}

		public Device FindOutputDevice(string name, bool raw)
		{
			if (name == null || name == string.Empty)
				return null;
			
			var count = OutputDeviceCount ();
			for (int i = 0; i < count; i += 1) {

				Device device = null;
				bool keepDevice = false;
				try {
					device = GetOutputDevice(i);

					if (device.Name.Contains(name) && device.IsRaw == raw) {
						//Console.WriteLine ("{0}: {1}", device.Id, device.Name);
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

		public Device FindInputDevice(string name)
		{
			return FindInputDevice (name, false);
		}

		public Device FindInputDevice(string name, bool raw)
		{
			if (name == null || name == string.Empty)
				return null;
			
			var count = InputDeviceCount ();
			for (int i = 0; i < count; i += 1) {

				Device device = null;
				bool keepDevice = false;
				try {
					device = GetInputDevice(i);

					if (device.Name.Contains(name) && device.IsRaw == raw) {
						//Console.WriteLine ("{0}: {1}", device.Id, device.Name);
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


	}
}

