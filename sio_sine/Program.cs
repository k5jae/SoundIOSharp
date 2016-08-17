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
using SoundIOSharp;
using AppTools;

namespace sio_sine
{
	class MainClass
	{
		static volatile bool running = true;
		static SoundIO soundIO;
		static bool wantPause = false;

		private static void PrintUsage()
		{
			Console.WriteLine("Usage: [options]");
			Console.WriteLine("Options:");
			Console.WriteLine("  [--backend dummy|alsa|pulseaudio|jack|coreaudio|wasapi]");
			Console.WriteLine("  [--target \"name of sound device\"");
		}

		public static int Main (string[] args)
		{
			Backend backend = Backend.None;
			string targetDevice = string.Empty;

			try {
				for (int i = 0; i < args.Length; i++) {
					switch (args[i]) {
					case "--backend":
						i++;
						if (args[i].Equals("dummy")) {
							backend = Backend.Dummy;
						} else if (args[i].Equals("alsa")) {
							backend = Backend.Alsa;
						} else if (args[i].Equals("pulseaudio")) {
							backend = Backend.PulseAudio;
						} else if (args[i].Equals("jack")) {
							backend = Backend.Jack;
						} else if (args[i].Equals("coreaudio")) {
							backend = Backend.CoreAudio;
						} else if (args[i].Equals("wasapi")) {
							backend = Backend.Wasapi;
						} else {
							Console.WriteLine("Invalid backend: {0}", args[i]);
							return 1;
						}
						break;
					case "--target":
						i++;
						targetDevice = args[i];
						break;

					case "--help":
						PrintUsage();
						return 1;
					}
				}
			} catch (IndexOutOfRangeException) {
				PrintUsage();
				return 1;
			}

			using (var exitHandler = ExitHandler.CreateExitHandler (false)) {
				exitHandler.OnProcessExited += exitHandler_OnProcessExited;

				// setup the CTRL+C handler to exit by signal or console
				Console.CancelKeyPress += delegate(object sender, ConsoleCancelEventArgs e) {
					e.Cancel = true;
					// call methods to clean up
					//Console.WriteLine ("CTRL+C, Exiting...");
				};

				Console.WriteLine("SoundIO Native Library Version: {0}", SoundIO.NativeVersion);
				Console.WriteLine("SoundIO Managed Library Version: {0}", SoundIO.ManagedVersion);

				int sampleRate = 48000;
				int channels = 1;
				Error err;

				using (soundIO = new SoundIO ()) {

					soundIO.OnDevicesChanged += SoundIo_OnDevicesChange;
					soundIO.OnEvents += SoundIo_OnEventsSignal;
					soundIO.OnBackendDisconnected += SoundIo_OnBackendDisconnect;

					if (backend == Backend.None) {
						soundIO.Connect();
					} else {
						soundIO.ConnectBackend(backend);
					}

					Console.WriteLine ("Current Backend: {0}", soundIO.BackendName (soundIO.CurrentBackend));

					soundIO.FlushEvents ();

					var device = soundIO.FindOutputDevice(targetDevice);
					if (device == null) {
						if (targetDevice != null && targetDevice != string.Empty) {
							Console.WriteLine ("Output device not found: {0}, trying default...", targetDevice);
						}
						var selectedDeviceIndex = soundIO.DefaultOutputDeviceIndex();

						if (selectedDeviceIndex < 0) {
							Console.WriteLine("Output device not found");
							return 1;
						}

						device = soundIO.GetOutputDevice(selectedDeviceIndex);
						if (device == null) {
							Console.WriteLine("out of memory");
							return 1;
						}
					}

					if (device.ProbeError != Error.None) {
						Console.WriteLine("Cannot probe device: {0}", device.ProbeErrorStr);
						return 1;
					}

					using (device) 
					using (var outstream = device.OutStreamCreate()) {
						outstream.OnWrite = WriteCallback;
						outstream.OnUnderflow = UnderflowCallback;
						outstream.Name = "sio_sine";
						outstream.SampleRate = sampleRate;

						// look for correct channel layout
						foreach (var layout in device.Layouts) {
							if (layout.ChannelCount == channels) {
								outstream.Layout = layout;
								break;
							}
						}

						if (soundIO.DeviceSupportsFormat (device, Format.Float32LE)) {
							outstream.Format = Format.Float32LE;
						} else if (soundIO.DeviceSupportsFormat (device, Format.Float64LE)) {
							outstream.Format = Format.Float64LE;
						} else if (soundIO.DeviceSupportsFormat (device, Format.S32LE)) {
							outstream.Format = Format.S32LE;
						} else if (soundIO.DeviceSupportsFormat (device, Format.S16LE)) {
							outstream.Format = Format.S16LE;
						} else {
							Console.WriteLine ("No suitable device format available.");
							return 1;
						}

						err = outstream.Open ();
						if (err != Error.None) {
							Console.WriteLine ("unable to open device: {0}", SoundIO.ErrorString (err));
							return 1;
						}

						Console.WriteLine ("Software latency: {0}", outstream.SoftwareLatency);

						if (outstream.LayoutError != Error.None)
							Console.WriteLine ("unable to set channel layout: {0}", SoundIO.ErrorString (outstream.LayoutError));

						outstream.Start ();

						while (running) {
							System.Threading.Thread.Sleep (100);
							if (Console.KeyAvailable)  
							{  
								ConsoleKeyInfo key = Console.ReadKey(true);
								Console.WriteLine (key.KeyChar);
								switch (key.Key)  
								{  
								case ConsoleKey.P:  
									wantPause = !wantPause;
									if (!wantPause)
										outstream.Pause (wantPause);
									break;

								case ConsoleKey.Q:
									outstream.Pause (true);
									running = false;
									break;  

								default:  
									break;  
								}  
							}  

							if (running && soundIO != null)
								soundIO.FlushEvents ();
						}

						// on Windows this will cause the stream to wait for pause before completing Dispose()
						//outstream.Pause(true);
					}
				}
			}
			Console.WriteLine("End Program");
			return 0;
		}

		static double secondsOffset = 0.0;

		public static void WriteCallback(OutStream stream, int frameCountMin, int frameCountMax)
		{
			double sampleRate = stream.SampleRate;
			double secondsPerFrame = 1.0 / sampleRate;

			Error err;

			int framesRemaining = frameCountMax;

			while (framesRemaining > 0) {
				ChannelArea area;
				int frameCount = framesRemaining;

				if ((err = stream.BeginWrite(out area, ref frameCount)) != Error.None) {
					Console.WriteLine("unrecoverable stream error: {0}", SoundIO.ErrorString(err));
					running = false;
				}

				if (frameCount == 0)
					break;

				ChannelLayout layout = stream.Layout;

				double pitch = 440.0;
				double radiansPerSecond = pitch * 2.0 * Math.PI;

				float[] buffer = new float[frameCount*layout.ChannelCount];

				for (int frame = 0; frame < frameCount; frame += 1) {
					float sample = (float)Math.Sin ((secondsOffset + frame * secondsPerFrame) * radiansPerSecond);
					for (int channel = 0; channel < layout.ChannelCount; channel += 1) {
						buffer [(frame * layout.ChannelCount) + channel] = sample;
					}
				}

				stream.CopyTo(buffer, 0, area, buffer.Length);

				secondsOffset += secondsPerFrame * frameCount;

				if ((err = stream.EndWrite()) != Error.None) {
					if (err == Error.Underflow)
						return;
					Console.WriteLine("unrecoverable stream error: {0}", SoundIO.ErrorString(err));
					running = false;
				}

				framesRemaining -= frameCount;
			}

			stream.Pause(wantPause);
		}

		public static void UnderflowCallback(OutStream stream)
		{
			Console.WriteLine ("Underflow");
		}

		static void SoundIo_OnDevicesChange (object sender, EventArgs e)
		{
			Console.WriteLine ("OnDevicesChange");
		}

		static void SoundIo_OnBackendDisconnect (object sender, BackendDisconnectEventArgs eventArgs)
		{
			Console.WriteLine ("OnBackendDisconnect");
		}

		static void SoundIo_OnEventsSignal (object sender, EventArgs e)
		{
			Console.WriteLine ("OnEventsSignal");
		}

		static void exitHandler_OnProcessExited()
		{
			running = false;
		}
	}
}
