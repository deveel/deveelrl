// TabCompleteEventArgs.cs
//  
// Author:
//       Antonello Provenzano <antonello@deveel.com>
//  
// 
//  Copyright (C) 2009 Deveel
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
// 

using System;

namespace Deveel {
	public delegate void TabCompleteEventHandler(object sender, TabCompleteEventArgs e);

	public class TabCompleteEventArgs : EventArgs {
		#region ctor
		internal TabCompleteEventArgs(string text, int state) {
			this.text = text;
			insert = null;
			alternatives = null;
			this.state = state;
		}
		#endregion

		#region Fields
		// Internal state.
		private readonly string text;
		private readonly int state;
		private string insert;
		private string[] alternatives;
		private bool error;
		#endregion

		#region Properties
		/// <summary>
		/// Get the text before the last space to the current position.
		/// </summary>
		public string Text {
			get { return text; }
		}

		/// <summary>
		/// Get or set the extra string to be inserted into the line.
		/// </summary>
		public string Insert {
			get { return insert; }
			set {
				if (value != null)
					insert = value;
			}
		}

		public int State {
			get { return state; }
		}

		/// <summary>
		/// Gets or sets the text that will be added to the current position
		/// of the command line.
		/// </summary>
		public string Output {
			get { return (insert == null ? text : text + insert); }
			set {
				if (value == null) {
					insert = value;
				} else {
					if (value.Length < text.Length)
						return;

					string s = value.Substring(0, text.Length);
					if (String.Compare(text, s, true) != 0)
						throw new ArgumentException();
					insert = value.Substring(text.Length);
				}
			}
		}

		/// <summary>
		/// Get or set the list of strings to be displayed as alternatives.
		/// </summary>
		public String[] Alternatives {
			get { return alternatives; }
			set { alternatives = value; }
		}

		public bool Error {
			get { return error; }
			set { error = value; }
		}
		#endregion
	}
}