//
// Enums.cs
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
	public enum Error {
		None,
		/// Out of memory.
		NoMemory,
		/// The backend does not appear to be active or running.
		InitAudioBackend,
		/// A system resource other than memory was not available.
		SystemResources,
		/// Attempted to open a device and failed.
		OpeningDevice,
		NoSuchDevice,
		/// The programmer did not comply with the API.
		Invalid,
		/// libsoundio was compiled without support for that backend.
		BackendUnavailable,
		/// An open stream had an error that can only be recovered from by
		/// destroying the stream and creating it again.
		ErrorStreaming,
		/// Attempted to use a device with parameters it cannot support.
		IncompatibleDevice,
		/// When JACK returns `JackNoSuchClient`
		NoSuchClient,
		/// Attempted to use parameters that the backend cannot support.
		IncompatibleBackend,
		/// Backend server shutdown or became inactive.
		BackendDisconnected,
		Interrupted,
		/// Buffer underrun occurred.
		Underflow,
		/// Unable to convert to or from UTF-8 to the native string format.
		EncodingString,
	};

	/// Specifies where a channel is physically located.
	public enum Channels {
		Invalid,

		FrontLeft, //< First of the more commonly supported ids.
		FrontRight,
		FrontCenter,
		Lfe,
		BackLeft,
		BackRight,
		FrontLeftCenter,
		FrontRightCenter,
		BackCenter,
		SideLeft,
		SideRight,
		TopCenter,
		TopFrontLeft,
		TopFrontCenter,
		TopFrontRight,
		TopBackLeft,
		TopBackCenter,
		TopBackRight, //< Last of the more commonly supported ids.

		BackLeftCenter, //< First of the less commonly supported ids.
		BackRightCenter,
		FrontLeftWide,
		FrontRightWide,
		FrontLeftHigh,
		FrontCenterHigh,
		FrontRightHigh,
		TopFrontLeftCenter,
		TopFrontRightCenter,
		TopSideLeft,
		TopSideRight,
		LeftLfe,
		RightLfe,
		Lfe2,
		BottomCenter,
		BottomLeftCenter,
		BottomRightCenter,

		// Mid/side recording
		MsMid,
		MsSide,

		// first order ambisonic channels
		AmbisonicW,
		AmbisonicX,
		AmbisonicY,
		AmbisonicZ,

		// X-Y Recording
		XyX,
		XyY,

		HeadphonesLeft, //< First of the "other" channel ids
		HeadphonesRight,
		ClickTrack,
		ForeignLanguage,
		HearingImpaired,
		Narration,
		Haptic,
		DialogCentricMix, //< Last of the "other" channel ids

		Aux,
		Aux0,
		Aux1,
		Aux2,
		Aux3,
		Aux4,
		Aux5,
		Aux6,
		Aux7,
		Aux8,
		Aux9,
		Aux10,
		Aux11,
		Aux12,
		Aux13,
		Aux14,
		Aux15,
	};

	/// Built-in channel layouts for convenience.
	public enum ChannelLayouts {
		IdMono,
		IdStereo,
		Id2Point1,
		Id3Point0,
		Id3Point0Back,
		Id3Point1,
		Id4Point0,
		IdQuad,
		IdQuadSide,
		Id4Point1,
		Id5Point0Back,
		Id5Point0Side,
		Id5Point1,
		Id5Point1Back,
		Id6Point0Side,
		Id6Point0Front,
		IdHexagonal,
		Id6Point1,
		Id6Point1Back,
		Id6Point1Front,
		Id7Point0,
		Id7Point0Front,
		Id7Point1,
		Id7Point1Wide,
		Id7Point1WideBack,
		IdOctagonal,
	};

	public enum Backend {
		None,
		Jack,
		PulseAudio,
		Alsa,
		CoreAudio,
		Wasapi,
		Dummy,
	};

	public enum DeviceAim {
		/// capture / recording
		Input,  
		/// playback
		Output, 
	};

	/// For your convenience, Native Endian and Foreign Endian constants are defined
	/// which point to the respective SoundIoFormat values.
	public enum Format {
		Invalid, 
		/// Signed 8 bit
		S8,      
		/// Unsigned 8 bit  
		U8,      
		/// Signed 16 bit Little Endian  
		S16LE,   
		/// Signed 16 bit Big Endian  
		S16BE,   
		/// Unsigned 16 bit Little Endian  
		U16LE,   
		/// Unsigned 16 bit Little Endian  
		U16BE,   
		/// Signed 24 bit Little Endian using low three bytes in 32-bit word  
		S24LE,   
		/// Signed 24 bit Big Endian using low three bytes in 32-bit word  
		S24BE,   
		/// Unsigned 24 bit Little Endian using low three bytes in 32-bit word  
		U24LE,   
		/// Unsigned 24 bit Big Endian using low three bytes in 32-bit word  
		U24BE,   
		/// Signed 32 bit Little Endian  
		S32LE,   
		/// Signed 32 bit Big Endian  
		S32BE,   
		/// Unsigned 32 bit Little Endian  
		U32LE,   
		/// Unsigned 32 bit Big Endian  
		U32BE,   
		/// Float 32 bit Little Endian, Range -1.0 to 1.0  
		Float32LE,
		/// Float 32 bit Big Endian, Range -1.0 to 1.0, 
		Float32BE,
		/// Float 64 bit Little Endian, Range -1.0 to 1.0, 
		Float64LE,
		/// Float 64 bit Big Endian, Range -1.0 to 1.0                           , 
		Float64BE, 
	};
}

