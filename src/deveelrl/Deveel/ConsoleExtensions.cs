// Copyright (c) 2009-2012, Deveel
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
//     * Redistributions of source code must retain the above copyright
//       notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright
//       notice, this list of conditions and the following disclaimer in the
//       documentation and/or other materials provided with the distribution.
//     * Neither the name of the <organization> nor the
//       names of its contributors may be used to endorse or promote products
//       derived from this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL <COPYRIGHT HOLDER> BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

using System;

namespace Deveel {
	static class ConsoleExtensions {
		// Event that is emitted when the console window size changes.
		public static event EventHandler SizeChanged;

		// Event that is emitted when the program is resumed after a suspend.
		public static event EventHandler Resumed;

		// Read a key while processing window resizes and process resumption.
		public static ConsoleKeyInfo ReadKey() {
			return ReadKey(false);
		}

		public static ConsoleKeyInfo ReadKey(bool intercept) {
			ConsoleKeyInfo key = Console.ReadKey(intercept);
			if (key.Key == (ConsoleKey)0x1200) {
				// "SizeChanged" key indication.
				if (SizeChanged != null) {
					SizeChanged(null, EventArgs.Empty);
				}
			} else if (key.Key == (ConsoleKey)0x1201) {
				// "Resumed" key indication.
				if (Resumed != null) {
					Resumed(null, EventArgs.Empty);
				}
			}
			return key;
		}
	}
}