//
// Program.cs
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
using SoundIOSharp;

namespace sio_list_devices
{
	class MainClass
	{
		static bool shortOutput = false;

		private static void PrintUsage()
		{
			Console.WriteLine ("Usage:  [options]");
			Console.WriteLine ("Options:");
			Console.WriteLine ("  [--watch]");
			Console.WriteLine ("  [--backend dummy|alsa|pulseaudio|jack|coreaudio|wasapi]");
			Console.WriteLine ("  [--short]");		
		}

		public static int Main (string[] args)
		{
			bool watch = false;
			Backend backend = Backend.None;

			for (int i = 0; i < args.Length; i++) {
				switch (args [i]) {
				case "--watch":
					watch = true;
					break;

				case "--short":
					shortOutput = true;
					break;

				case "--backend":
					i++;
					if (args [i].Equals ("dummy")) {
						backend = Backend.Dummy;
					} else if (args [i].Equals ("alsa")) {
						backend = Backend.Alsa;
					} else if (args [i].Equals ("pulseaudio")) {
						backend = Backend.PulseAudio;
					} else if (args [i].Equals ("jack")) {
						backend = Backend.Jack;
					} else if (args [i].Equals ("coreaudio")) {
						backend = Backend.CoreAudio;
					} else if (args [i].Equals ("wasapi")) {
						backend = Backend.Wasapi;
					} else {
						Console.WriteLine ("Invalid backend: {0}", args [i]);
						return 1;
					}
					break;

				default:
					PrintUsage ();
					return 1;
				}
			}

			using (var soundIo = new SoundIO ()) {

				if (backend == Backend.None) {
					soundIo.Connect ();
				} else {
					soundIo.ConnectBackend (backend);
				}

				soundIo.FlushEvents ();
				ListDevices (soundIo);

				soundIo.OnDevicesChanged += SoundIo_OnDevicesChange;
				//soundIo.OnEventsSignal += SoundIo_OnEventsSignal;
				//soundIo.OnBackendDisconnect += SoundIo_OnBackendDisconnect;

				while (watch) {
					System.Threading.Thread.Sleep (100);
					soundIo.FlushEvents ();
				}
			}

			return 0;
		}

		static void SoundIo_OnDevicesChange (object sender, EventArgs e)
		{
			Console.WriteLine ("OnDevicesChange");
			ListDevices ((SoundIO)sender);
		}

		static void SoundIo_OnBackendDisconnect (object sender, BackendDisconnectEventArgs eventArgs)
		{
			Console.WriteLine ("OnBackendDisconnect");
		}

		static void SoundIo_OnEventsSignal (object sender, EventArgs e)
		{
			Console.WriteLine ("OnEventsSignal");
		}

		private static void ListDevices(SoundIO soundIo)
		{
			int output_count = soundIo.OutputDeviceCount ();
			int input_count = soundIo.InputDeviceCount ();

			int default_output = soundIo.DefaultOutputDeviceIndex();
			int default_input = soundIo.DefaultInputDeviceIndex();

			var inputDeviceNameList = new Dictionary<string, string> ();
			var outputDeviceNameList = new Dictionary<string, string> ();

			Console.WriteLine("--------Input Devices--------");
			for (int i = 0; i < input_count; i += 1) {
				using (Device device = soundIo.GetInputDevice (i)) {
					int count = 1;
					var name = device.Name;
					while (inputDeviceNameList.ContainsKey(name)) {
						count++;
						name = string.Format ("{0}, #{1}", device.Name, count);
					}

					inputDeviceNameList.Add(name, device.Id);
					PrintDevice (name, device, default_input == i);
				}
			}

			Console.WriteLine("\n--------Output Devices--------");
			for (int i = 0; i < output_count; i += 1) {
				using (Device device = soundIo.GetOutputDevice (i)) {
					int count = 1;
					var name = device.Name;
					while (outputDeviceNameList.ContainsKey(name)) {
						count++;
						name = string.Format ("{0}, #{1}", device.Name, count);
					}

					outputDeviceNameList.Add(name, device.Id);
					PrintDevice (name, device, default_output == i);
				}
			}
 		}

		private static void PrintDevice(string nameId, Device device, bool is_default)
		{
			string default_str = is_default ? " (default)" : "";
			string raw_str = device.IsRaw ? " (raw)" : "";

			Console.WriteLine("{0}{1}{2}", nameId, default_str, raw_str);
			if (shortOutput)
				return;
			Console.WriteLine("  id: {0}", device.Id);

			if (device.ProbeError != Error.None) {
				Console.WriteLine("  probe error: {0}", device.ProbeErrorStr);
			} else {
				Console.WriteLine("  channel layouts:");
				foreach (var layout in device.Layouts) {
					Console.Write("    ");
					PrintChannelLayout(layout);
					Console.WriteLine ();
				}
				if (device.CurrentLayout.ChannelCount > 0) {
					Console.Write("  current layout: ");
					PrintChannelLayout(device.CurrentLayout);
					Console.WriteLine ();
				}

				Console.WriteLine("  sample rates:");
				foreach (var samplerate in device.SampleRates) {
					Console.WriteLine("    {0} - {1}", samplerate.Min, samplerate.Max);

				}
				if (device.CurrentSampleRate > 0)
					Console.WriteLine("  current sample rate: {0}", device.CurrentSampleRate);
				Console.Write("  formats: ");
				for (int i = 0; i < device.Formats.Length; i += 1) {
					string comma = (i == device.Formats.Length - 1) ? "" : ", ";
					Console.Write("{0}{1}", SoundIO.FormatString(device.Formats[i]), comma);
				}
				Console.WriteLine ();
				if (device.CurrentFormat != Format.Invalid)
					Console.WriteLine("  current format: {0}", SoundIO.FormatString(device.CurrentFormat));

				Console.WriteLine("  min software latency: {0:0.00000000} sec", device.SoftwareLatencyMin);
				Console.WriteLine("  max software latency: {0:0.00000000} sec", device.SoftwareLatencyMax);
				if (device.SoftwareLatencyCurrent != 0.0)
					Console.WriteLine("  current software latency: {0:0.00000000} sec\n", device.SoftwareLatencyCurrent);

			}
			
			Console.WriteLine ();
		}

		static void PrintChannelLayout(ChannelLayout layout)
		{
			if (layout.Name != null && layout.Name != string.Empty) {
				Console.Write(layout.Name);
			} else {
				Console.Write("{0}", SoundIO.GetChannelName(layout.Channels[0]));
				for (int i = 1; i < layout.ChannelCount; i += 1) {
					Console.Write(", {0}", SoundIO.GetChannelName(layout.Channels[i]));
				}
			}
		}
	}
}
