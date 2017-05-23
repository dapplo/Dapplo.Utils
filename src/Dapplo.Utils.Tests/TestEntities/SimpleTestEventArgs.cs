using System;

namespace Dapplo.Utils.Tests.TestEntities
{
	/// <summary>
	/// Arguments for the test event
	/// </summary>
	public class SimpleTestEventArgs : EventArgs
	{
		public string TestValue { get; set; }
	}
}
