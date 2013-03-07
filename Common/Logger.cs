using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Common
{
	public static class Logger
	{
		public enum Level { Error, Warn, Info, Debug };

		public static Level Current = Level.Error;

		private static void Log(Level level, string message, params object[] repl)
		{
			if (Current < level)
				return;

			if (level == Level.Error)
			{
				Console.Error.WriteLine(message, repl);
				return;
			}

			Console.WriteLine(message, repl);
		}

		public static void Error(Exception ex)
		{
			Error("Error: {0}", ex.Message);
		}

		public static void Error(string message, params object[] repl)
		{
			Log(Level.Error, message, repl);
		}

		public static void Warn(Exception ex)
		{
			Error("Warn: {0}", ex.Message);
		}

		public static void Warn(string message, params object[] repl)
		{
			Log(Level.Warn, message, repl);
		}

		public static void Info(Exception ex)
		{
			Error("Info: {0}", ex.Message);
		}

		public static void Info(string message, params object[] repl)
		{
			Log(Level.Info, message, repl);
		}

		[ConditionalAttribute("DEBUG")]
		public static void Debug(Exception ex)
		{
			Error("Debug: {0}", ex.Message);
		}

		[ConditionalAttribute("DEBUG")]
		public static void Debug(string message, params object[] repl)
		{
			Log(Level.Debug, message, repl);
		}
	}
}
