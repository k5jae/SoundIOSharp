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
using NAudio.Wave;

namespace sio_play
{
	class MainClass
	{
		static SoundIO soundIO;
		static WaveFileReader waveFile;
		static ISampleProvider sampleProvider;
		static int channels;
		static int sampleRate;
		static bool fileDone;
		static bool startSilence;
		static double latencySeconds;
		static int silentSamplesRemaining;
		static int silentSamplesAlreadySent;
		static DateTime callbackTime;

		private static void PrintUsage()
		{
			Console.WriteLine("Usage: [options] filename (wav format)");
			Console.WriteLine("Options:");
			Console.WriteLine("  [--backend dummy|alsa|pulseaudio|jack|coreaudio|wasapi]");
			Console.WriteLine("  [--target \"name of sound device\"");
		}

		public static int Main(string[] args)
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
							if (File.Exists(args[i])) {
								filename = args[i];
							} else {
								PrintUsage();
								return 1;
							}
							break;
					}
				}
			} catch (IndexOutOfRangeException) {
				PrintUsage();
				return 1;
			}

			// test...
			// targetDevice = "USB Audio CODEC";
			// targetDevice = "PCM2904 Audio Codec";

			Console.WriteLine("SoundIO Native Library Version: {0}", SoundIO.NativeVersion);
			Console.WriteLine("SoundIO Managed Library Version: {0}", SoundIO.ManagedVersion);

			using (soundIO = new SoundIO())
			using (waveFile = new WaveFileReader(filename)) {
				channels = waveFile.WaveFormat.Channels;
				sampleRate = waveFile.WaveFormat.SampleRate;

				if (backend == Backend.None) {
					soundIO.Connect();
				} else {
					soundIO.ConnectBackend(backend);
				}
					
				Console.WriteLine("Current Backend: {0}", soundIO.BackendName(soundIO.CurrentBackend));

				var err = soundIO.FlushEvents();
				var devices = soundIO.FindOutputDevices(targetDevice);
				Device device = null;

				if (devices != null && devices.Length > 0) {
					device = devices [0];
				}

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

				using (device) {
					Console.WriteLine("Output device: {0}", device.Name);

					if (device.ProbeError != Error.None) {
						Console.WriteLine("Cannot probe device: {0}", device.ProbeErrorStr);
						return 1;
					}

					var outstream = device.OutStreamCreate();
					outstream.OnWrite = WriteCallback;
					outstream.OnUnderflow = UnderflowCallback;
					outstream.Name = "sio_play";
					outstream.SampleRate = sampleRate;

					// look for maching layout for wav file...
					var foundLayout = false;
					foreach (var layout in device.Layouts) {
						if (layout.ChannelCount == channels) {
							outstream.Layout = layout;
							foundLayout = true;
							break;
						}
					}

					// TODO: may need to look at endian issues and other formats...
					// when paired with NAudioLite, ISampleProvider the conversion to Float32 is automatic.
					if (soundIO.DeviceSupportsFormat(device, Format.Float32LE)) {
						outstream.Format = Format.Float32LE;
					} else if (soundIO.DeviceSupportsFormat(device, Format.Float64LE)) {
						outstream.Format = Format.Float64LE;
					} else if (soundIO.DeviceSupportsFormat(device, Format.S32LE)) {
						outstream.Format = Format.S32LE;
					} else if (soundIO.DeviceSupportsFormat(device, Format.S16LE)) {
						outstream.Format = Format.S16LE;
					} else {
						Console.WriteLine("No suitable device format available.");
						return 1;
					}

					Console.WriteLine();
					Console.WriteLine("Playing file: {0}, Format: {1}", 
						Path.GetFullPath(filename), waveFile.WaveFormat);

					err = outstream.Open();
					if (err != Error.None) {
						Console.WriteLine("Unable to open device: {0}, with sample rate: {1}",
										   SoundIO.ErrorString(err), outstream.SampleRate);
						return 1;
					}

					if (outstream.LayoutError != Error.None)
						Console.WriteLine("Unable to set channel layout: {0}", SoundIO.ErrorString(outstream.LayoutError));

					// revisit layout...
					// if no suitable layout found
					if (!foundLayout) {
						Console.WriteLine("No native channel layout found, Device Channels: {0}, Wav File Channels: {1}, requires sampler...",
						                  outstream.Layout.ChannelCount, channels);
					}

					// get sample provider that matches outstream.Layout
					if (outstream.Layout.ChannelCount == 1) { // mono
						if (waveFile.WaveFormat.Channels == 1) {
							sampleProvider = waveFile.ToSampleProvider();
						} else {
							sampleProvider = waveFile.ToSampleProvider().ToMono();
						}
					} else if (outstream.Layout.ChannelCount == 2) { //stereo
						if (waveFile.WaveFormat.Channels == 1) {
							sampleProvider = waveFile.ToSampleProvider().ToStereo();
						} else {
							sampleProvider = waveFile.ToSampleProvider();
						}
					}

					outstream.Start();

					//soundIO.OnDevicesChanged += SoundIo_OnDevicesChanged;
					//soundIO.OnEvents += SoundIo_OnEvents;
					soundIO.OnBackendDisconnected += SoundIo_OnBackendDisconnected;

					while (!fileDone) {
						System.Threading.Thread.Sleep(100);
						soundIO.FlushEvents();
					}

					System.Threading.Thread.Sleep(500);
					if (fileDone && outstream != null) {
						Console.WriteLine("Cleanup stream...");
						outstream.Dispose();
						outstream = null;
					}

					Console.WriteLine("End Program");
					return 0;
				}
			}
		}

		public static void WriteCallback(OutStream stream, int frameCountMin, int frameCountMax)
		{
			Error err;
			int framesRemaining = frameCountMax;

			// loop to utilize as much as the frameCountMax as possible
			while (framesRemaining > 0) {
				ChannelArea area;

				int frameCount = framesRemaining;
				if ((err = stream.BeginWrite(out area, ref frameCount)) != Error.None) {
					Console.WriteLine("unrecoverable stream error: {0}", SoundIO.ErrorString(err));
					fileDone = true;
				}

				if (frameCount == 0)
					break;

				var bufferCount = frameCount * stream.Layout.ChannelCount;

				// if buffer is done add silence based on latency to allow stream to complete through
				// audio path before stream is Disposed()
				if (waveFile.Position >= waveFile.Length) {
					int silentBufferSize = 0;

					if (startSilence) {
						// windows latency is a little higher (using DateTime to determine the callback time delay)
						// and needs to be accoutned for...
						latencySeconds -= (DateTime.Now - callbackTime).TotalMilliseconds / 1000.0;
						silentSamplesRemaining = (int)((stream.SampleRate * stream.Layout.ChannelCount) * latencySeconds);
						silentSamplesRemaining -= silentSamplesAlreadySent;
						startSilence = false;
					}

					if (silentSamplesRemaining > bufferCount) {
						silentBufferSize = bufferCount;
						silentSamplesRemaining -= bufferCount;
					} else {
						silentBufferSize = silentSamplesRemaining;
						silentSamplesRemaining = 0;
					}

					if (silentBufferSize > 0) {
						// create a new buffer initialized to 0 and copy to native buffer
						var silenceBuffer = new float[silentBufferSize];
						stream.CopyTo (silenceBuffer, 0, area, silentBufferSize);
					}
					if (silentSamplesRemaining == 0) {
						fileDone = true;
						stream.EndWrite();
						stream.Pause(true);
						Console.WriteLine("Stream Done...");
						return;
					}
					// if the remaining audioBuffer will only partially fill the frameCount
					// copy the remaining amount and set the startSilence flag to allow
					// stream to play to the end.

				} else if (waveFile.Position + (frameCount * waveFile.WaveFormat.Channels) >= waveFile.Length) {
					float[] audioBuffer = new float[bufferCount];
					var actualSamplesRead = sampleProvider.Read(audioBuffer, 0, bufferCount);
					stream.CopyTo (audioBuffer, 0, area, bufferCount);

					silentSamplesAlreadySent = bufferCount - actualSamplesRead;
					latencySeconds = 0.0;
					startSilence = true;
				} else {
					// copy audioBuffer data to native buffer and advance the bufferPos
					float[] audioBuffer = new float[bufferCount];
					var actualSamplesRead = sampleProvider.Read(audioBuffer, 0, bufferCount);
					stream.CopyTo (audioBuffer, 0, area, actualSamplesRead);

					if (waveFile.Position >= waveFile.Length) {
						latencySeconds = 0.0;
						startSilence = true;
					}
				}

				if ((err = stream.EndWrite()) != Error.None) {
					if (err == Error.Underflow)
						return;
					Console.WriteLine("Unrecoverable stream error: {0}", SoundIO.ErrorString(err));
					fileDone = true;
				}

				if (startSilence) {
					// get actual latency in order to compute number of silent frames
					stream.GetLatency(out latencySeconds);
					callbackTime = DateTime.Now;
				}

				// loop until frameCountMax is used up
				framesRemaining -= frameCount;
			}
			return;
		}


		public static void UnderflowCallback(OutStream stream)
		{
			Console.WriteLine("Underflow");
		}

		static void SoundIo_OnDevicesChanged(object sender, EventArgs e)
		{
			Console.WriteLine("OnDevicesChange");
		}

		static void SoundIo_OnBackendDisconnected(object sender, BackendDisconnectEventArgs eventArgs)
		{
			Console.WriteLine("OnBackendDisconnect");
		}

		static void SoundIo_OnEvents(object sender, EventArgs e)
		{
			Console.WriteLine("OnEventsSignal");
		}

	}
}
