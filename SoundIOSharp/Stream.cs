//
// Stream.cs
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
	public abstract class Stream : IDisposable
	{
		protected bool disposed;

		protected abstract void Destroy();

		private void Dispose(bool disposing)
		{
			if(!this.disposed)
			{
				if(disposing)
				{
					// Dispose here any managed resources
				}

				Destroy ();

			}
			disposed = true;         
		}

		/// <summary>
		/// Disposes hardware resources.
		/// </summary>
		/// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="SoundIOSharp.SoundIO"/>. The
		/// <see cref="Dispose"/> method leaves the <see cref="SoundIOSharp.SoundIO"/> in an unusable state. After calling
		/// <see cref="Dispose"/>, you must release all references to the <see cref="SoundIOSharp.SoundIO"/> so the garbage
		/// collector can reclaim the memory that the <see cref="SoundIOSharp.SoundIO"/> was occupying.</remarks>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		~Stream() {
			Dispose(false);
		}
	}
}

