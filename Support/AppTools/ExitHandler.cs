//
// ExitHandler.cs
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

namespace AppTools
{
	public abstract class ExitHandler : IDisposable
	{
		public delegate void ProcessExitedDelegate ();

		public event ProcessExitedDelegate OnProcessExited;

		static internal bool IsWindows {
			get {
				int p = (int)Environment.OSVersion.Platform;
				if ((p == 4) || (p == 6) || (p == 128)) {
					//Console.WriteLine ("Running on Unix");
					return false;
				} else {
					//Console.WriteLine ("NOT running on Unix");
					return true;
				}
			}
		}

		public static ExitHandler CreateExitHandler (bool hideConsole)
		{
			if (IsWindows) {
				return new WindowsExitHandler (hideConsole);
			} else {
				return new PosixExitHandler ();
			}
		}

		internal ExitHandler ()
		{
		}

		protected void ExitProcess ()
		{
			if (OnProcessExited != null) {
				OnProcessExited ();
			}
		}

		public virtual void Dispose ()
		{
		}
	}
}
