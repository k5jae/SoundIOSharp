//
// WindowsExitHandler.cs
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
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace AppTools
{
	/// <summary>
	/// Description of WindowsExitHandler.
	/// </summary>
	public class WindowsExitHandler : ExitHandler
	{
		public WindowsExitHandler (bool hideConsole)
		{
			if (hideConsole) {
				HideConsole ();
			}
			Console.CancelKeyPress += Console_CancelKeyPress;
		}

		private void HideConsole ()
		{
			var builder = new StringBuilder (255);
			GetConsoleTitle (builder, 255);
			
			var consoleTitle = builder.ToString ();
			var uri = new UriBuilder (Assembly.GetEntryAssembly ().CodeBase);
			var appPath = Path.GetFullPath (Uri.UnescapeDataString (uri.Path));
			
			if (consoleTitle.Equals (appPath)) {
				//var ret = AllocConsole();
				HideConsoleWindow ();
			}
		}
		
		// If using Windows, this handler is executed...
		private void Console_CancelKeyPress (object sender, ConsoleCancelEventArgs e)
		{
			if (e.SpecialKey == ConsoleSpecialKey.ControlC) {
				Console.WriteLine ("Received Ctrl+C...");
				e.Cancel = true;
			} else {
				Console.WriteLine ("Received Ctrl+Break...");
			}

			ExitProcess ();
		}

		public static void ShowConsoleWindow ()
		{
			var handle = GetConsoleWindow ();
	
			if (handle != IntPtr.Zero) {
				ShowWindow (handle, SW_SHOW);
			}
		}

		public static void HideConsoleWindow ()
		{
			var handle = GetConsoleWindow ();
	
			if (handle != IntPtr.Zero) {
				ShowWindow (handle, SW_HIDE);
			}
		}

		[DllImport ("kernel32.dll", SetLastError = true)]
		static extern bool AllocConsole ();

		[DllImport ("kernel32.dll")]
		static extern IntPtr GetConsoleWindow ();

		[DllImport ("user32.dll")]
		static extern bool ShowWindow (IntPtr hWnd, int nCmdShow);

		const int SW_HIDE = 0;
		const int SW_SHOW = 5;

		[DllImport ("kernel32.dll", SetLastError = true)]
		static extern uint GetConsoleTitle (
			[Out] StringBuilder lpConsoleTitle,
			uint nSize
		);
	}
}
