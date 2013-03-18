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
			ConfigurationElementBase copyFrom = new ConfigurationElementBase()
			{
				LoggingLevel = Logger.Level.Error,
				MasterNamespace = "Test",
				ConnectionString = "Connect"
			};
			ConfigurationElementBase test = new ConfigurationElementBase();

			test.CopyFrom(ref copyFrom);
			Assert.AreEqual(Logger.Level.Error, test.LoggingLevel);
			Assert.AreEqual("Test", test.MasterNamespace);
			Assert.AreEqual("Connect", test.ConnectionString);

			ConfigurationElementBase copyTo = new ConfigurationElementBase();
			test.CopyTo(ref copyTo);
			Assert.AreEqual(Logger.Level.Error, copyTo.LoggingLevel);
			Assert.AreEqual("Test", copyTo.MasterNamespace);
			Assert.AreEqual("Connect", copyTo.ConnectionString);
		}
	}
}
