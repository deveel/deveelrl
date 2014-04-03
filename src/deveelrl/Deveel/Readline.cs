using System;
using System.Collections;
using System.IO;
using System.Text;

namespace Deveel {
	public static class Readline {
		static Readline() {
			ControlDIsEOF = true;
			ControlZIsEOF = (Path.DirectorySeparatorChar == '\\');
			controlCInterrupts = (Path.DirectorySeparatorChar == '/');
			Console.TreatControlCAsInput = !controlCInterrupts;
		}

		#region Fields
		// Internal state.
		private static bool controlCInterrupts;

		// Line input buffer.
		private static readonly char[] buffer = new char[256];
		private static readonly byte[] widths = new byte[256];
		private static int posn, length, column, lastColumn;
		private static bool overwrite;
		private static int historyPosn;
		private static string historySave;
		private static string yankedString;

		private static char[] wordBreakChars = new char[] { ' ', '\n' };
		#endregion

		#region Events
		/// <summary>
		/// Event that is emitted to allow for tab completion.
		/// </summary>
		/// <remarks>
		/// If there are no attached handlers, then the Tab key will do 
		/// normal tabbing.
		/// </remarks>
		public static event TabCompleteEventHandler TabComplete;
		
		/// <summary>
		/// Event that is emitted to inform of the Ctrl+C command.
		/// </summary>
		public static event EventHandler Interrupt;
		
		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets a flag that indicates if pressing the "Enter" key on 
		/// an empty line causes the most recent history line to be duplicated.
		/// </summary>
		public static bool EnterIsDuplicate { get; set; }

		/// <summary>
		/// Gets or sets a flag that indicates if CTRL-D is an EOF indication
		/// or the "delete character" key.
		/// </summary>
		/// <remarks>
		/// The default is true (i.e. EOF).
		/// </remarks>
		public static bool ControlDIsEOF { get; set; }

		/// <summary>
		/// Gets or sets a flag that indicates if CTRL-Z is an EOF indication.
		/// </summary>
		/// <remarks>
		/// The default is true on Windows system, false otherwise.
		/// </remarks>
		public static bool ControlZIsEOF { get; set; }

		/// <summary>
		/// Gets or sets a flag that indicates if CTRL-C is an EOF indication.
		/// </summary>
		/// <remarks>
		/// The default is true on Unix system, false otherwise.
		/// </remarks>
		public static bool ControlCInterrupts {
			get { return controlCInterrupts; }
			set {
				Console.TreatControlCAsInput = !value;
				controlCInterrupts = value;
			}
		}


		public static string LineBuffer {
			get { return new string(buffer, 0, length); }
		}

		public static char[] WordBreakCharacters {
			get { return wordBreakChars; }
			set {
				if (value == null || value.Length == 0)
					throw new ArgumentNullException("value");
				wordBreakChars = value;
			}
		}

		#endregion

		#region Private Static Methods
		
		/// <summary>
		/// Makes room for one more character in the input buffer.
		/// </summary>
		private static void MakeRoom() {
			if (length >= buffer.Length) {
				char[] newBuffer = new char[buffer.Length * 2];
				byte[] newWidths = new byte[buffer.Length * 2];
				Array.Copy(buffer, 0, newBuffer, 0, buffer.Length);
				Array.Copy(widths, 0, newWidths, 0, buffer.Length);
			}
		}

		/// <summary>
		/// Repaint the line starting at the current character.
		/// </summary>
		/// <param name="step"></param>
		/// <param name="moveToEnd"></param>
		private static void Repaint(bool step, bool moveToEnd) {
			int posn = Readline.posn;
			int column = Readline.column;
			int width;

			// Paint the characters in the line.
			while (posn < length) {
				if (buffer[posn] == '\t') {
					width = 8 - (column % 8);
					widths[posn] = (byte)width;
					while (width > 0) {
						Console.Write(' ');
						--width;
						++column;
					}
				} else if (buffer[posn] < 0x20) {
					Console.Write('^');
					Console.Write((char)(buffer[posn] + 0x40));
					widths[posn] = 2;
					column += 2;
				} else if (buffer[posn] == '\u007F') {
					Console.Write('^');
					Console.Write('?');
					widths[posn] = 2;
					column += 2;
				} else {
					Console.Write(buffer[posn]);
					widths[posn] = 1;
					++column;
				}
				++posn;
			}

			// Adjust the position of the last column.
			if (column > lastColumn) {
				lastColumn = column;
			} else if (column < lastColumn) {
				// We need to clear some characters beyond this point.
				width = lastColumn - column;
				lastColumn = column;
				while (width > 0) {
					Console.Write(' ');
					--width;
					++column;
				}
			}

			// Backspace to the initial cursor position.
			if (moveToEnd) {
				width = column - lastColumn;
				Readline.posn = length;
			} else if (step) {
				width = column - (Readline.column + widths[Readline.posn]);
				Readline.column += widths[Readline.posn];
				++(Readline.posn);
			} else {
				width = column - Readline.column;
			}
			while (width > 0) {
				Console.Write('\u0008');
				--width;
			}
		}

		/// <summary>
		/// Add a character to the input buffer.
		/// </summary>
		/// <param name="ch"></param>
		private static void AddChar(char ch) {
			if (overwrite && posn < length) {
				buffer[posn] = ch;
				Repaint(true, false);
			} else {
				MakeRoom();
				if (posn < length) {
					Array.Copy(buffer, posn, buffer, posn + 1, length - posn);
				}
				buffer[posn] = ch;
				++length;
				Repaint(true, false);
			}
			
			if (Array.IndexOf(wordBreakChars, ch) != -1) {
				CollectLastWord(ch);
			}
		}
		
		private static void CollectLastWord(char ch) {
			ArrayList chars = new ArrayList();
			for (int i = length - 1; i >= 0; i--) {
				char c = buffer[i];
				if (ch != '\0' && c == ch)
					break;
				else if (Array.IndexOf(wordBreakChars, c) != -1)
					break;
				
				chars.Add(c);
			}
			chars.Reverse();
			lastWord.Length = 0;
			lastWord.Append((char[]) chars.ToArray(typeof (char)));
		}

		// Go back a specific number of characters.
		private static void GoBack(int num) {
			int width;
			while (num > 0) {
				--posn;
				width = widths[posn];
				column -= width;
				while (width > 0) {
					Console.Write('\u0008');
					--width;
				}
				--num;
			}
		}

		// Backspace one character.
		private static void Backspace() {
			if (posn > 0) {
				GoBack(1);
				Delete();
			}
		}

		// Delete the character under the cursor.
		private static void Delete() {
			/*
			if (posn < length) {
				Array.Copy(buffer, posn + 1, buffer, posn, length - posn - 1);
				--length;
				Repaint(false, false);
			}
			*/
			Delete(1);
		}

		// Delete a number of characters under the cursor.
		private static void Delete(int num) {
			Array.Copy(buffer, posn + num, buffer, posn, length - posn - num);
			length -= num;
			Repaint(false, false);
			//TODO: check...
			ArrayList chars = new ArrayList();
			for (int i = length - 1; i >= 0; i--) {
				char c = buffer[i];
				if (Array.IndexOf(wordBreakChars, c) != -1)
					break;
				chars.Add(c);
			}
			chars.Reverse();
			lastWord.Length = 0;
			lastWord.Append((char[])chars.ToArray(typeof(char)));
		}

		// Print a list of alternatives for tab completion.
		private static void PrintAlternatives(String[] list) {
			int width, maxWidth;
			int columns, column, posn;
			String str;

			// Determine the maximum string length, for formatting.
			maxWidth = 0;
			foreach (String a in list) {
				if (a != null) {
					width = a.Length;
					if (width > maxWidth) {
						maxWidth = width;
					}
				}
			}

			// Determine the number of columns.
			width = Console.WindowWidth;
			if (maxWidth > (width - 7)) {
				columns = 1;
			} else {
				columns = width / (maxWidth + 7);
			}

			// Print the strings.
			column = 0;
			for (posn = 0; posn < list.Length; ++posn) {
				str = list[posn];
				if (str != null) {
					Console.Write(str);
					width = str.Length;
				} else {
					width = 0;
				}
				++column;
				if (column < columns) {
					while (width < maxWidth) {
						Console.Write(' ');
						++width;
					}
					Console.Write("       ");
				} else {
					Console.Write("\r\n");
					column = 0;
				}
			}
			if (column != 0) {
				Console.Write("\r\n");
			}
		}

		private static State state = State.None;
		private static string prefix;
		private static readonly StringBuilder lastWord = new StringBuilder();
		private static int savePosn;
		private static int tabCount = -1;
		private static int insertedCount;

		private static void ResetComplete(State newState) {
			if (state == State.Completing) {
				tabCount = -1;
				savePosn = -1;
			}

			state = newState;
		}

		// Tab across to the next stop, or perform tab completion.
		private static void Tab(String prompt) {
			if (TabComplete == null) {
				// Add the TAB character and repaint the line.
				AddChar('\t');
			} else {
				if (state != State.Completing) {
					CollectLastWord('\0');
					state = State.Completing;
				}

				// Perform tab completion and insert the results.
				TabCompleteEventArgs e = new TabCompleteEventArgs(lastWord.ToString(), ++tabCount);
				TabComplete(null, e);
				if (e.Insert != null) {
					if (tabCount > 0) {
						GoBack(insertedCount);
						Delete(insertedCount);
					}

					insertedCount = e.Insert.Length;
					savePosn = posn;
					// Insert the value that we found.
					bool saveOverwrite = overwrite;
					overwrite = false;
					savePosn = e.Insert.Length;

					state = State.Completing;
					foreach (char ch in e.Insert) {
						AddChar(ch);
					}
					overwrite = saveOverwrite;
				} else if (e.Alternatives != null && e.Alternatives.Length > 0) {
					// Print the alternatives for the user.
					savePosn = posn;
					EndLine();
					PrintAlternatives(e.Alternatives);
					if (prompt != null) {
						Console.Write(prompt);
					}
					posn = savePosn;
					state = State.Completing;
					Redraw();
				} else {
					if (e.Error)
						ResetComplete(State.MoreInput);

					// No alternatives, or alternatives not supplied yet.
					Console.Beep();
				}
			}
		}

		// End the current line.
		private static void EndLine() {
			// Repaint the line and move to the end.
			Repaint(false, true);

			// Output the line terminator to the terminal.
			Console.Write("\r\n");
		}

		// Move left one character.
		private static void MoveLeft() {
			if (posn > 0) {
				GoBack(1);
			}
		}

		// Move right one character.
		private static void MoveRight() {
			if (posn < length) {
				Repaint(true, false);
			}
		}

		// Set the current buffer contents to a historical string.
		private static void SetCurrent(String line) {
			if (line == null) {
				line = String.Empty;
			}
			Clear();
			foreach (char ch in line) {
				AddChar(ch);
			}
		}

		// Move up one line in the history.
		private static void MoveUp() {
			if (historyPosn == -1) {
				if (History.Count > 0) {
					historySave = new String(buffer, 0, length);
					historyPosn = 0;
					SetCurrent(History.GetHistory(historyPosn));
				}
			} else if ((historyPosn + 1) < History.Count) {
				++historyPosn;
				SetCurrent(History.GetHistory(historyPosn));
			} else {
				Console.Beep();
			}
		}

		// Move down one line in the history.
		private static void MoveDown() {
			if (historyPosn == 0) {
				historyPosn = -1;
				SetCurrent(historySave);
			} else if (historyPosn > 0) {
				--historyPosn;
				SetCurrent(History.GetHistory(historyPosn));
			} else {
				Console.Beep();
			}
		}

		// Move to the beginning of the current line.
		private static void MoveHome() {
			GoBack(posn);
		}

		// Move to the end of the current line.
		private static void MoveEnd() {
			Repaint(false, true);
		}

		// Clear the entire line.
		private static void Clear() {
			GoBack(posn);
			length = 0;
			Repaint(false, false);
		}

		// Cancel the current line and start afresh with a new prompt.
		private static void CancelLine(String prompt) {
			EndLine();
			if (prompt != null) {
				Console.Write(prompt);
			}
			posn = 0;
			length = 0;
			column = 0;
			lastColumn = 0;
			historyPosn = -1;
		}

		// Redraw the current line.
		private static void Redraw() {
			String str = new String(buffer, 0, length);
			int savePosn = posn;
			posn = 0;
			length = 0;
			column = 0;
			lastColumn = 0;
			foreach (char ch in str) {
				AddChar(ch);
			}
			GoBack(length - savePosn);
		}

		// Erase all characters until the start of the current line.
		private static void EraseToStart() {
			if (posn > 0) {
				int savePosn = posn;
				yankedString = new String(buffer, 0, posn);
				GoBack(savePosn);
				Delete(savePosn);
			}
		}

		// Erase all characters until the end of the current line.
		private static void EraseToEnd() {
			yankedString = new String(buffer, posn, length - posn);
			length = posn;
			Repaint(false, false);
			lastWord.Length = 0;
		}

		// Erase the previous word on the current line (delimited by whitespace).
		private static void EraseWord() {
			int temp = posn;
			while (temp > 0 && Char.IsWhiteSpace(buffer[temp - 1])) {
				--temp;
			}
			while (temp > 0 && !Char.IsWhiteSpace(buffer[temp - 1])) {
				--temp;
			}
			if (temp < posn) {
				temp = posn - temp;
				GoBack(temp);
				yankedString = new String(buffer, posn, temp);
				Delete(temp);
			}

			if (state != State.Completing)
				lastWord.Length = 0;
		}

		// Determine if a character is a "word character" (letter or digit).
		private static bool IsWordCharacter(char ch) {
			return Char.IsLetterOrDigit(ch);
		}

		// Erase to the end of the current word.
		private static void EraseToEndWord() {
			int temp = posn;
			while (temp < length && !IsWordCharacter(buffer[temp])) {
				++temp;
			}
			while (temp < length && IsWordCharacter(buffer[temp])) {
				++temp;
			}
			if (temp > posn) {
				temp -= posn;
				yankedString = new String(buffer, posn, temp);
				Delete(temp);
			}
		}

		// Erase to the start of the current word.
		private static void EraseToStartWord() {
			int temp = posn;
			while (temp > 0 && !IsWordCharacter(buffer[temp - 1])) {
				--temp;
			}
			while (temp > 0 && IsWordCharacter(buffer[temp - 1])) {
				--temp;
			}
			if (temp < posn) {
				temp = posn - temp;
				GoBack(temp);
				yankedString = new String(buffer, posn, temp);
				Delete(temp);
			}
		}

		// Move forward one word in the input line.
		private static void MoveForwardWord() {
			while (posn < length && !IsWordCharacter(buffer[posn])) {
				MoveRight();
			}
			while (posn < length && IsWordCharacter(buffer[posn])) {
				MoveRight();
			}
		}

		// Move backward one word in the input line.
		private static void MoveBackwardWord() {
			while (posn > 0 && !IsWordCharacter(buffer[posn - 1])) {
				MoveLeft();
			}
			while (posn > 0 && IsWordCharacter(buffer[posn - 1])) {
				MoveLeft();
			}
		}
		#endregion

		#region Public Static Methods
		public static string ReadLine(string prompt) {
			return ReadLine(prompt, false);
		}

		// Read the next line of input using line editing.  Returns "null"
		// if an EOF indication is encountered in the input.
		public static string ReadLine(string prompt, bool password) {
			if (password)
				throw new NotSupportedException();

			ConsoleKeyInfo key;
			char ch;

			// Output the prompt.
			if (prompt != null) {
				Console.Write(prompt);
			}

			// Enter the main character input loop.
			posn = 0;
			length = 0;
			column = 0;
			lastColumn = 0;
			overwrite = false;
			historyPosn = -1;
			bool ctrlv = false;
			state = State.MoreInput;
			do {
				key = ConsoleExtensions.ReadKey(true);
				ch = key.KeyChar;
				if (ctrlv) {
					ctrlv = false;
					if ((ch >= 0x0001 && ch <= 0x001F) || ch == 0x007F) {
						// Insert a control character into the buffer.
						AddChar(ch);
						continue;
					}
				}
				if (ch != '\0') {
					switch (ch) {
						case '\u0001': {
								// CTRL-A: move to the home position.
								MoveHome();
							}
							break;

						case '\u0002': {
								// CTRL-B: go back one character.
								MoveLeft();
							}
							break;

						case '\u0003': {
								// CTRL-C encountered in "raw" mode.
								if (controlCInterrupts) {
									EndLine();
									if (Interrupt != null)
										Interrupt(null, EventArgs.Empty);
									return null;
								} else {
									CancelLine(prompt);
									lastWord.Length = 0;
								}
							}
							break;

						case '\u0004': {
								// CTRL-D: EOF or delete the current character.
								if (ControlDIsEOF) {
									lastWord.Length = 0;
									// Signal an EOF if the buffer is empty.
									if (length == 0) {
										EndLine();
										return null;
									}
								} else {
									Delete();
									ResetComplete(State.MoreInput);
								}
							}
							break;

						case '\u0005': {
								// CTRL-E: move to the end position.
								MoveEnd();
							}
							break;

						case '\u0006': {
								// CTRL-F: go forward one character.
								MoveRight();
							}
							break;

						case '\u0007': {
								// CTRL-G: ring the terminal bell.
								Console.Beep();
							}
							break;

						case '\u0008':
						case '\u007F': {
								if (key.Key == ConsoleKey.Delete) {
									// Delete the character under the cursor.
									Delete();
								} else {
									// Delete the character before the cursor.
									Backspace();
								}
								ResetComplete(State.MoreInput);
							}
							break;

						case '\u0009': {
								// Process a tab.
								Tab(prompt);
							}
							break;

						case '\u000A':
						case '\u000D': {
								// Line termination.
								EndLine();
								ResetComplete(State.Done);
								lastWord.Length = 0;
							}
							break;

						case '\u000B': {
								// CTRL-K: erase until the end of the line.
								EraseToEnd();
							}
							break;

						case '\u000C': {
								// CTRL-L: clear screen and redraw.
								Console.Clear();
								Console.Write(prompt);
								Redraw();
							}
							break;

						case '\u000E': {
								// CTRL-N: move down in the history.
								MoveDown();
							}
							break;

						case '\u0010': {
								// CTRL-P: move up in the history.
								MoveUp();
							}
							break;

						case '\u0015': {
								// CTRL-U: erase to the start of the line.
								EraseToStart();
								ResetComplete(State.None);
							}
							break;

						case '\u0016': {
								// CTRL-V: prefix a control character.
								ctrlv = true;
							}
							break;

						case '\u0017': {
								// CTRL-W: erase the previous word.
								EraseWord();
								ResetComplete(State.MoreInput);
							}
							break;

						case '\u0019': {
								// CTRL-Y: yank the last erased string.
								if (yankedString != null) {
									foreach (char ch2 in yankedString) {
										AddChar(ch2);
									}
								}
							}
							break;

						case '\u001A': {
								// CTRL-Z: Windows end of file indication.
								if (ControlZIsEOF && length == 0) {
									EndLine();
									return null;
								}
							}
							break;

						case '\u001B': {
								// Escape is "clear line".
								Clear();
								ResetComplete(State.MoreInput);
							}
							break;

						default: {
								if (ch >= ' ') {
									// Ordinary character.
									AddChar(ch);
									ResetComplete(State.MoreInput);
								}
							}
							break;
					}
				} else if (key.Modifiers == (ConsoleModifiers)0) {
					switch (key.Key) {
						case ConsoleKey.Backspace: {
								// Delete the character before the cursor.
								Backspace();
								ResetComplete(State.MoreInput);
							}
							break;

						case ConsoleKey.Delete: {
								// Delete the character under the cursor.
								Delete();
								ResetComplete(State.MoreInput);
							}
							break;

						case ConsoleKey.Enter: {
								// Line termination.
								EndLine();
								ResetComplete(State.Done);
							}
							break;

						case ConsoleKey.Escape: {
								// Clear the current line.
								Clear();
								ResetComplete(State.None);
							}
							break;

						case ConsoleKey.Tab: {
								// Process a tab.
								Tab(prompt);
							}
							break;

						case ConsoleKey.LeftArrow: {
								// Move left one character.
								MoveLeft();
							}
							break;

						case ConsoleKey.RightArrow: {
								// Move right one character.
								MoveRight();
							}
							break;

						case ConsoleKey.UpArrow: {
								// Move up one line in the history.
								MoveUp();
							}
							break;

						case ConsoleKey.DownArrow: {
								// Move down one line in the history.
								MoveDown();
							}
							break;

						case ConsoleKey.Home: {
								// Move to the beginning of the line.
								MoveHome();
							}
							break;

						case ConsoleKey.End: {
								// Move to the end of the line.
								MoveEnd();
							}
							break;

						case ConsoleKey.Insert: {
								// Toggle insert/overwrite mode.
								overwrite = !overwrite;
							}
							break;
					}
				} else if ((key.Modifiers & ConsoleModifiers.Alt) != 0) {
					switch (key.Key) {
						case ConsoleKey.F: {
								// ALT-F: move forward a word.
								MoveForwardWord();
							}
							break;

						case ConsoleKey.B: {
								// ALT-B: move backward a word.
								MoveBackwardWord();
							}
							break;

						case ConsoleKey.D: {
								// ALT-D: erase until the end of the word.
								EraseToEndWord();
							}
							break;

						case ConsoleKey.Backspace:
						case ConsoleKey.Delete: {
								// ALT-DEL: erase until the start of the word.
								EraseToStartWord();
							}
							break;
					}
				}
			}
			while (state != State.Done);
			if (length == 0 && EnterIsDuplicate) {
				if (History.Count > 0) {
					return History.GetHistory(0);
				}
			}
			return new String(buffer, 0, length);
		}

		public static string ReadPassword(string prompt) {
			// Output the prompt.
			if (prompt != null)
				Console.Write(prompt);

			Stack pass = new Stack();

			for (ConsoleKeyInfo consKeyInfo = Console.ReadKey(true);
			  consKeyInfo.Key != ConsoleKey.Enter; consKeyInfo = Console.ReadKey(true)) {
				if (consKeyInfo.Key == ConsoleKey.Backspace) {
					try {
						Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
						Console.Write(" ");
						Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
						pass.Pop();
					} catch (InvalidOperationException) {
						Console.SetCursorPosition(Console.CursorLeft + 1, Console.CursorTop);
					}
				} else {
					Console.Write("*");
					pass.Push(consKeyInfo.KeyChar.ToString());
				}
			}
			object[] chars = pass.ToArray();
			string[] password = new string[chars.Length];
			Array.Copy(chars, password, chars.Length);
			Array.Reverse(password);
			return string.Join(string.Empty, password);

		}
		#endregion
		
	}
}