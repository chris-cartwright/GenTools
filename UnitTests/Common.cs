using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Common;
using JetBrains.Annotations;
using NUnit.Framework;

namespace UnitTests
{
	public class Common
	{
		private readonly Dictionary<string, string> _files = new Dictionary<string, string>()
		{
			{ "OpenWriter", Path.Combine(Environment.CurrentDirectory, "Test.readonly") }
		};

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

		[UsedImplicitly]
		public static IEnumerable<TestCaseData> SetupSource()
		{
			yield return new TestCaseData(new[] { "--name=Test" }, new ConfigurationElementBase() { Name = "Test", LoggingLevel = Logger.Level.Error }).Returns(new string[] { });
			yield return new TestCaseData(new[] { "-v" }, new ConfigurationElementBase() { LoggingLevel = Logger.Level.Error }).Returns(new string[] { });
			yield return new TestCaseData(new[] { "-vv" }, new ConfigurationElementBase() { LoggingLevel = Logger.Level.Warn }).Returns(new string[] { });
			yield return new TestCaseData(new[] { "-vvv" }, new ConfigurationElementBase() { LoggingLevel = Logger.Level.Info }).Returns(new string[] { });
			yield return new TestCaseData(new[] { "-vvvv" }, new ConfigurationElementBase() { LoggingLevel = Logger.Level.Debug }).Returns(new string[] { });
			yield return new TestCaseData(new[] { "test", "one", "two", "three" }, new ConfigurationElementBase()).Returns(new[] { "test", "one", "two", "three" });
		}

		[Test]
		[TestCaseSource("SetupSource")]
		public string[] Setup(string[] args, ConfigurationElementBase expected)
		{
			ConfigurationElementBase test = new ConfigurationElementBase();
			string[] extra = Helpers.Setup(args, ref test);
			expected.CompareTo(test, BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
			return extra;
		}

		[Test]
		public void OpenWriter()
		{
			if (File.Exists(_files["OpenWriter"]))
			{
				try
				{
					File.Delete(_files["OpenWriter"]);
				}
				catch (Exception)
				{
					Console.Error.WriteLine("Previous test failed to delete test file.");
					File.SetAttributes(_files["OpenWriter"], FileAttributes.Normal);
					File.Delete(_files["OpenWriter"]);
				}
			}

			File.Create(_files["OpenWriter"]).Close();
			File.SetAttributes(_files["OpenWriter"], FileAttributes.ReadOnly);

			Assert.AreEqual(FileAttributes.ReadOnly, File.GetAttributes(_files["OpenWriter"]));
			Assert.Throws<UnauthorizedAccessException>(() => File.WriteAllText(_files["OpenWriter"], "test"));

			Assert.DoesNotThrow(delegate
			{
				using (StreamWriter sw = Helpers.OpenWriter(_files["OpenWriter"]))
				{
					sw.Write("test 2");
				}
			});

			Assert.AreEqual("test 2", File.ReadAllText(_files["OpenWriter"]));
			File.Delete(_files["OpenWriter"]);
			Assert.IsFalse(File.Exists(_files["OpenWriter"]));
		}

		[TestFixtureTearDown]
		public void TearDown()
		{
			foreach (string file in _files.Values.Where(File.Exists))
			{
				File.SetAttributes(file, FileAttributes.Normal);
				File.Delete(file);
			}
		}
	}
}
