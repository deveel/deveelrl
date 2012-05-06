using System;

namespace Deveel {
	class Program {
		static void Main(string[] args) {
			Console.Out.WriteLine("Please enter your name:");
			string name = Readline.ReadLine("> ");
			Console.Out.WriteLine("Ciao {0}! I'm glad you're testing ReadLine!", name);
			Console.Out.WriteLine();

			Console.Out.WriteLine("Try writing something else... write 'exit' to close this application.");
			Console.Out.WriteLine();

			string prompt = String.Format("{0}> ", name);

			string echo;
			while ((echo = Readline.ReadLine(prompt)) != null &&
				!String.Equals(echo, "exit", StringComparison.InvariantCultureIgnoreCase)) {
				Console.Out.WriteLine("echo: {0}", echo);
			}

			Console.Out.WriteLine("Bye-Bye!");
		}
	}
}
