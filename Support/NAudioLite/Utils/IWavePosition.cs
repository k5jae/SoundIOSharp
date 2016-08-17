using System;
using System.Linq;
using NAudio.Wave;

namespace NAudio.Utils
{
	/// <summary>
	/// Interface for IWavePlayers that can report position
	/// </summary>
	public interface IWavePosition
	{
		/// <summary>
		/// Position (in terms of bytes played - does not necessarily)
		/// </summary>
		/// <returns>Position in bytes</returns>
		long GetPosition();

		/// <summary>
		/// Gets a <see cref="Wave.WaveFormat"/> instance indicating the format the hardware is using.
		/// </summary>
		WaveFormat OutputWaveFormat { get; }
	}
}

