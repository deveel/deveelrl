using System;

namespace Deveel {
	public enum State {
		None = 0x00000,
		MoreInput = 0x00040,
		Overwrite = 0x02000,
		Completing = 0x04000,
		Done = 0x80000
	}
}