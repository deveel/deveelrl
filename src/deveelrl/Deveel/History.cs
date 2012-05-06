using System;
using System.Collections;
using System.IO;
using System.Text;

namespace Deveel {
	/// <summary>
	/// The handler for the lines stored in the history of the
	/// console.
	/// </summary>
	public static class History {
		private static int maxHistorySize = 0;
		private static readonly ArrayList history = new ArrayList();

		/// <summary>
		/// Gets or sets the maximum history list size.
		/// </summary>
		/// <remarks>
		/// If this the history should have no limit, use 0.
		/// </remarks>
		/// <exception cref="ArgumentOutOfRangeException">
		/// If the given value is smaller than 0.
		/// </exception>
		public static int MaximumHistorySize {
			get { return maxHistorySize; }
			set {
				if (value < 0)
					throw new ArgumentOutOfRangeException();
				maxHistorySize = value;
			}
		}

		/// <summary>
		/// Gets the number of items currently in the history.
		/// </summary>
		public static int Count {
			get { return history.Count; }
		}

		/// <summary>
		/// Add a line of input to the scroll-back history.
		/// </summary>
		/// <param name="line">The line string to add to the history.</param>
		public static void AddHistory(string line) {
			if (line == null)
				line = String.Empty;

			if (maxHistorySize != 0 && history.Count == maxHistorySize)
				// Remove the oldest entry, to preserve the maximum size.
				history.RemoveAt(0);
			history.Add(line);
		}

		/// <summary>
		/// Adds a line of input to the scroll-back history, if it is
		/// different from the most recent line that is present.
		/// </summary>
		/// <param name="line"></param>
		public static void AddHistoryUnique(String line) {
			if (line == null)
				line = String.Empty;
			if (history.Count == 0 ||
				((string)(history[history.Count - 1])) != line)
				AddHistory(line);
		}

		/// <summary>
		/// Clear the scroll-back history.
		/// </summary>
		public static void ClearHistory() {
			history.Clear();
		}

		/// <summary>
		/// Get a particular history item.  Zero is the most recent.
		/// </summary>
		/// <param name="index"></param>
		/// <returns>
		/// Returns the line at the given <paramref name="index"/> if
		/// found, otherwise an empty string.
		/// </returns>
		public static String GetHistory(int index) {
			if (index >= 0 && index < history.Count)
				return (string)(history[history.Count - index - 1]);
			return String.Empty;
		}

		/// <summary>
		/// Set a particular history item at the given index.
		/// </summary>
		/// <param name="index"></param>
		/// <param name="line"></param>
		public static void SetHistory(int index, string line) {
			if (line == null)
				line = String.Empty;
			if (index >= 0 && index < history.Count)
				history[history.Count - index - 1] = line;
		}

		/// <summary>
		/// Loads a set of history lines from a given file.
		/// </summary>
		/// <param name="file">The path to the file containing the history
		/// lines to load.</param>
		public static void Load(string file) {
			if (!File.Exists(file))
				throw new InvalidOperationException();

			FileStream fileStream = null;
			try {
				fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
				StreamReader reader = new StreamReader(fileStream, Encoding.UTF8);

				ClearHistory();

				string line;
				while ((line = reader.ReadLine()) != null)
					AddHistory(line);
			} finally {
				if (fileStream != null)
					fileStream.Close();
			}
		}

		/// <summary>
		/// Saves the history lines stored in the <see cref="History"/>
		/// into the given file.
		/// </summary>
		/// <param name="file">The path to the file where to store the
		/// history lines.</param>
		public static void Save(string file) {
			if (File.Exists(file))
				File.Delete(file);

			FileStream fileStream = null;
			try {
				fileStream = new FileStream(file, FileMode.CreateNew, FileAccess.Write, FileShare.None);

				StreamWriter writer = new StreamWriter(fileStream, Encoding.UTF8);
				for (int i = history.Count - 1; i >= 0; i--) {
					string line = (string) history[i];
					writer.WriteLine(line);
				}

				writer.Flush();
			} finally {
				if (fileStream != null)
					fileStream.Close();
			}
		}
	}
}