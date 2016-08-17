//
// PosixExitHandler.cs
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
using System.Reflection;
using System.Threading;

namespace AppTools
{
	/// <summary>
	/// Description of PosixExitHandler.
	/// </summary>
	public class PosixExitHandler : ExitHandler
	{
		private const int waitTimeout = 1000;

		private bool running = true;
		private Thread thread;
		private Type unixSignalType;
		private Type signumType;

		public PosixExitHandler ()
		{
			// use reflection since we don't want to "hard link" this lib
			AssemblyName name = new AssemblyName ("Mono.Posix, Version=2.0.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756");
			Assembly assem = Assembly.Load (name);
			Type[] types = assem.GetExportedTypes ();
			foreach (Type type in types) {
				if (type.FullName == "Mono.Unix.UnixSignal") {
					unixSignalType = type;
					break;
				}
			}
			foreach (Type type in types) {
				if (type.FullName == "Mono.Unix.Native.Signum") {
					signumType = type;
					break;
				}
			}
			
			// start the process exit thread
			thread = new Thread (new ThreadStart (ExitProcessThread));
			thread.Start ();
		}
		
		// If using Linux, this Signal Thread is used...
		private void ExitProcessThread ()
		{
			object sigIntEnum = Enum.Parse (signumType, "SIGINT");
			object sigTermEnum = Enum.Parse (signumType, "SIGTERM");
			object unixSignalSigInt = Activator.CreateInstance (unixSignalType, sigIntEnum);
			object unixSignalSigTerm = Activator.CreateInstance (unixSignalType, sigTermEnum);
						
			MethodInfo waitAnyMethodInfo = null;
			MethodInfo[] methodInfos = unixSignalType.GetMethods (BindingFlags.Public | BindingFlags.Static);
			foreach (MethodInfo methodInfo in methodInfos) {
				ParameterInfo[] parameterInfos = methodInfo.GetParameters ();
				if (parameterInfos.Length == 2 && parameterInfos [1].ParameterType.Equals (typeof(int))) {
					waitAnyMethodInfo = methodInfo;
					break;
				}
			}
			
			// create the array with the two UnixSignal(s)
			Array signalArray = Array.CreateInstance (unixSignalType, 2);
			signalArray.SetValue (unixSignalSigInt, 0);
			signalArray.SetValue (unixSignalSigTerm, 1);
			
			while (running) {
				// wait on either SIGINT or SIGTERM, or timeout
				int which = (int)waitAnyMethodInfo.Invoke (null, new object[] { signalArray, waitTimeout });
	
				switch (which) {
				// SIGINT received
				case 0:
					Console.WriteLine ("{0}Received SIGINT...", System.Environment.NewLine);
					ExitProcess ();
					break;
					
				// SIGTERM received
				case 1:
					Console.WriteLine ("{0}Received SIGTERM...", System.Environment.NewLine);
					ExitProcess ();
						
						// Force exit...
					Environment.Exit (130);
					break;
					
				// timeout reached
				default:
					break;
				}
			}
		}

		public override void Dispose ()
		{
			// exit the thread if this is running the unix signal version
			running = false;
			
			base.Dispose ();
		}
	}
}
