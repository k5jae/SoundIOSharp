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
using System.IO;
using SoundIOSharp;
using AppTools;
using NAudio.Wave;

namespace sio_rec
{
	class MainClass
	{
		static volatile bool running = true;
		static SoundIO soundIO;
		static bool wantPause = false;
		static WaveFileWriter waveFile;
		static double latencySeconds;

		private static void PrintUsage()
		{
			Console.WriteLine("Usage: [options] filename (wav format)");
			Console.WriteLine("Options:");
			Console.WriteLine("  [--backend dummy|alsa|pulseaudio|jack|coreaudio|wasapi]");
			Console.WriteLine("  [--target \"name of sound device\"");
		}

		public static int Main (string[] args)
		{
			Backend backend = Backend.None;
			string filename = string.Empty;
			string targetDevice = string.Empty;

			try {
				if (args.Length < 1) {
					PrintUsage();
					return 1;
				}
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

					default:
						// throws exception if filename not valid
						Path.GetFileName(args[i]);
						filename = args[i];
						break;
					}
				}
			} catch (Exception) {
				PrintUsage();
				return 1;
			}

			using (var exitHandler = ExitHandler.CreateExitHandler (false)) {
				exitHandler.OnProcessExited += OnProcessExited;

				// setup the CTRL+C handler to exit by signal or console
				Console.CancelKeyPress += delegate(object sender, ConsoleCancelEventArgs e) {
					e.Cancel = true;
					// call methods to clean up
					Console.WriteLine ("CTRL+C, Exiting...");
				};

				Console.WriteLine ("SoundIO Native Library Version: {0}", SoundIO.NativeVersion);
				Console.WriteLine ("SoundIO Managed Library Version: {0}", SoundIO.ManagedVersion);

				//var targetDevice = "PCM2904 Audio Codec";
				var channels = 2;
				var sampleRate = 44100; //48000;

				// create 32-bit Float wave format
				var waveFormat = WaveFormat.CreateIeeeFloatWaveFormat (sampleRate, channels);

				// create 16-bit PCM wave format
				//var waveFormat = new WaveFormat (sampleRate, channels);

				using (soundIO = new SoundIO ())
				using (waveFile = new WaveFileWriter (filename, waveFormat)) {
			
					if (backend == Backend.None) {
						soundIO.Connect();
					} else {
						soundIO.ConnectBackend(backend);
					}

					Console.WriteLine ("Current Backend: {0}", soundIO.BackendName (soundIO.CurrentBackend));

					var err = soundIO.FlushEvents ();
					var device = soundIO.FindInputDevice (targetDevice);
					if (device == null) {
						if (targetDevice != null && targetDevice != string.Empty) {
							Console.WriteLine ("Input device not found: {0}, trying default...", targetDevice);
						}
						var selectedDeviceIndex = soundIO.DefaultInputDeviceIndex ();

						if (selectedDeviceIndex < 0) {
							Console.WriteLine ("Input device not found");
							return 1;
						}

						device = soundIO.GetInputDevice (selectedDeviceIndex);
						if (device == null) {
							Console.WriteLine ("out of memory");
							return 1;
						}
					}

					using (device)
					using (var stream = device.InStreamCreate()) {
						Console.WriteLine ("Input device: {0}", device.Name);

						if (device.ProbeError != Error.None) {
							Console.WriteLine ("Cannot probe device: {0}", device.ProbeErrorStr);
							return 1;
						}
							
						stream.OnRead = ReadCallback;
						stream.OnOverflow = OverflowCallback;
						stream.Name = "sio_rec";
						stream.SampleRate = sampleRate;

						// look for correct channel layout
						var foundLayout = false;
						foreach (var layout in device.Layouts) {
							if (layout.ChannelCount == channels) {
								stream.Layout = layout;
								foundLayout = true;
								break;
							}
						}
						// if no suitable layout found
						if (!foundLayout) {
							Console.WriteLine ("No suitable channel layout found, desired channel count: {0}", channels);
						}

						if (soundIO.DeviceSupportsFormat (device, Format.Float32LE)) {
							stream.Format = Format.Float32LE;
						} else if (soundIO.DeviceSupportsFormat (device, Format.Float64LE)) {
							stream.Format = Format.Float64LE;
						} else if (soundIO.DeviceSupportsFormat (device, Format.S32LE)) {
							stream.Format = Format.S32LE;
						} else if (soundIO.DeviceSupportsFormat (device, Format.S16LE)) {
							stream.Format = Format.S16LE;
						} else {
							Console.WriteLine ("No suitable device format available.");
							return 1;
						}

						err = stream.Open ();
						if (err != Error.None) {
							Console.WriteLine ("unable to open device: {0}, with sample rate: {1}",
								SoundIO.ErrorString (err), stream.SampleRate);
							return 1;
						}

						latencySeconds = stream.SoftwareLatency;
						Console.WriteLine ("Software latency: {0}", stream.SoftwareLatency);

						if (stream.LayoutError != Error.None)
							Console.WriteLine ("unable to set channel layout: {0}", SoundIO.ErrorString (stream.LayoutError));

						stream.Start ();

						soundIO.OnDevicesChanged += SoundIo_OnDevicesChanged;
						soundIO.OnEvents += SoundIo_OnEvents;
						soundIO.OnBackendDisconnected += SoundIo_OnBackendDisconnected;

						while (running) {
							System.Threading.Thread.Sleep (100);
							if (Console.KeyAvailable) {  
								ConsoleKeyInfo key = Console.ReadKey (true);
								Console.WriteLine (key.KeyChar);
								switch (key.Key) {  
								case ConsoleKey.P:  
									wantPause = !wantPause;
									if (!wantPause)
										stream.Pause (wantPause);
									break;  
								case ConsoleKey.Q:
									Console.WriteLine ("Quit...");
									stream.Pause (true);
									Exit ();
									break;  

								default:  
									break;  
								}  
							}  

							if (running && soundIO != null)
								soundIO.FlushEvents ();
						}

					// using block...
					// Disposes stream
					// Disposes device
					}

				// using block... 
				// Flushes wave file before closing..
				// disposes SoundIO
				}
			}

			Console.WriteLine ("End Program");
			return 0;
		}

		public static void ReadCallback(InStream stream, int frameCountMin, int frameCountMax)
		{
			Error err;
			int writeFrames = frameCountMax;
			int framesLeft = writeFrames;
			while (framesLeft > 0) {
				ChannelArea area;
				int frameCount = framesLeft;

				if ((err = stream.BeginRead(out area, ref frameCount)) != Error.None) {
					Console.WriteLine("begin read error: {0}", SoundIO.ErrorString(err));
					running = false;
					return;
				}

				if (frameCount == 0)
					break;

				if (area ==  null) {
					Console.WriteLine ("overflow hole...");
					// Due to an overflow there is a hole. Fill the ring buffer with
					// silence for the size of the hole.
					//memset(write_ptr, 0, frame_count * instream->bytes_per_frame);
				} else {
					ChannelLayout layout = stream.Layout;

					float[] buffer = new float[frameCount * layout.ChannelCount];
					stream.CopyFrom(area, buffer, 0, buffer.Length);
					if (waveFile != null) {
						waveFile.WriteSamples (buffer, 0, buffer.Length);
					}
				}

				if ((err = stream.EndRead()) != Error.None) {
					Console.WriteLine("end read error: {0}", SoundIO.ErrorString(err));
					running = false;
					return;
				}
										
				framesLeft -= frameCount;
			}
		}

		public static void OverflowCallback(InStream stream)
		{
			Console.WriteLine ("Underflow");
		}

		static void SoundIo_OnDevicesChanged (object sender, EventArgs e)
		{
			Console.WriteLine ("OnDevicesChange");
		}

		static void SoundIo_OnBackendDisconnected (object sender, BackendDisconnectEventArgs eventArgs)
		{
			Console.WriteLine ("OnBackendDisconnect");
		}

		static void SoundIo_OnEvents (object sender, EventArgs e)
		{
			Console.WriteLine ("OnEventsSignal");
		}

		static void Exit()
		{
			// wait for OnRead() latency to alow last bit to be written to file 
			System.Threading.Thread.Sleep ((int)(latencySeconds * 1000));

			running = false;
		}

		static void OnProcessExited()
		{
			Exit ();
		}
	}
}
