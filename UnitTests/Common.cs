using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using global::Common;
using System.Dynamic;

namespace UnitTests
{
	public class Common
	{
		private class Settings
		{
			public ushort LoggingLevel { get; set; }
			public string MasterNamespace { get; set; }
			public string ConnectionString { get; set; }
		}

		[Test]
		public void CleanName()
		{
			Assert.AreEqual("@new", "new".CleanName());
			Assert.AreEqual("NoKeyword", "NoKeyword".CleanName());
			Assert.AreEqual("With_Space", "With Space".CleanName());
			Assert.AreEqual("_", @"<>?:""{}|~!@#$%^&*()_+,./;'[]\`-=".CleanName());
			Assert.AreEqual("_Digit", "9Digit".CleanName());
			// Pad first character because of the above test
			Assert.AreEqual("D1234567890", "D1234567890".CleanName());
			Assert.AreEqual("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ", "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ".CleanName());
		}

		[Test]
		public void SettingsTest()
		{
			Settings copyFrom = new Settings() { LoggingLevel = 10, MasterNamespace = "Test", ConnectionString = "Connect" };
			Helpers.Settings test = new Helpers.Settings();

			test.CopyFrom(ref copyFrom);
			Assert.AreEqual(10, test.LoggingLevel);
			Assert.AreEqual("Test", test.MasterNamespace);
			Assert.AreEqual("Connect", test.ConnectionString);

			Settings copyTo = new Settings();
			test.CopyTo(ref copyTo);
			Assert.AreEqual(10, copyTo.LoggingLevel);
			Assert.AreEqual("Test", copyTo.MasterNamespace);
			Assert.AreEqual("Connect", copyTo.ConnectionString);
		}
	}
}
