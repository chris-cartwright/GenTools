using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Mono.Options;

namespace Common
{
	public static class Helpers
	{
		private class Settings
		{
			public ushort LoggingLevel { get; set; }
			public string MasterNamespace { get; set; }
			public string ConnectionString { get; set; }

			public void CopyTo<T>(ref T obj)
			{
				Type type = typeof(T);
				foreach (PropertyInfo pi in typeof(Settings).GetProperties())
					type.GetProperty(pi.Name).SetValue(obj, pi.GetValue(this, null), null);
			}

			public void CopyFrom<T>(ref T obj)
			{
				Type type = typeof(T);
				foreach (PropertyInfo pi in typeof(Settings).GetProperties())
					pi.SetValue(this, type.GetProperty(pi.Name).GetValue(obj, null), null);
			}
		}

		public static readonly string[] Keywords = new string[]
		{
			 "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const", "continue", "decimal", "default",
			 "delegate", "do", "double", "else", "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach", "goto",
			 "if", "implicit", "in", "int", "interface", "internal", "is", "lock", "long", "namespace", "new", "null", "object", "operator", "out",
			 "override", "params", "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc",
			 "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using",
			 "virtual", "void", "volatile", "while"
		};

		// http://msdn.microsoft.com/en-us/library/cc716729.aspx
		public static readonly Dictionary<string, Type> TypeMap = new Dictionary<string, Type>()
		{
			{ "nvarchar",			typeof(string) },
			{ "varchar",			typeof(string) },
			{ "int",				typeof(int) },
			{ "bigint",				typeof(long) },
			{ "smallint",			typeof(short) },
			{ "bit",				typeof(bool) },
			{ "datetime",			typeof(DateTime) },
			{ "money",				typeof(decimal) },
			{ "Flag",				typeof(bool) },
			{ "hierarchyid",		typeof(string) },
			{ "tinyint",			typeof(byte) },
			{ "nchar",				typeof(char) },
			{ "char",				typeof(char) },
			{ "image",				typeof(byte[]) },
			{ "uniqueidentifier",	typeof(Guid) },
			{ "text",				typeof(string) },
			{ "decimal",			typeof(decimal) },
			{ "float",				typeof(float) },
			{ "varbinary",			typeof(byte[]) },
			{ "date",				typeof(DateTime) },
			{ "sysname",			typeof(string) },
            { "timestamp",          typeof(byte[]) }
		};

		public static readonly Version Version;
		public static readonly GitRevisionAttribute Revision;
		public static readonly string VersionString;
		public static readonly string AssemblyName;
		public static readonly string Description;

		static Helpers()
		{
			Assembly assembly = Assembly.GetEntryAssembly();
			AssemblyName assemblyName = assembly.GetName();
			object[] attributes = assembly.GetCustomAttributes(false);

			Version = assemblyName.Version;
			Revision = attributes.OfType<GitRevisionAttribute>().FirstOrDefault() ?? new GitRevisionAttribute("nogit", true);
			VersionString = String.Format("{0}-{1}-{2}", Version, Revision.Revision, Revision.Dirty ? "dirty" : "clean");

			AssemblyName = assemblyName.Name;
			Description = attributes.OfType<AssemblyDescriptionAttribute>().First().Description;
		}

		public static string CleanKeyword(this string str)
		{
			if (Keywords.Contains(str))
				return "@" + str;
			else
				return str;
		}

		public static string CleanName(this string str)
		{
			string ret = str.Trim();

			if (Char.IsDigit(ret[0]))
				ret = "_" + ret.Substring(1);

			ret = new Regex(@"[^\w0-9]").Replace(ret, "_");
			ret = new Regex("_+").Replace(ret, "_");

			return ret.TrimStart('@').CleanKeyword();
		}

		public static int Setup<T>(string[] args, ref T appSettings, out string[] extra, out SqlConnection conn, OptionSet opts = null)
		{
			Console.WriteLine("{0} version {1}", AssemblyName, VersionString);

			extra = new string[0];
			conn = null;

			ushort verbosity = 0;
			Settings settings = new Settings();
			settings.CopyFrom(ref appSettings);

			bool help = false;
			OptionSet options = new OptionSet()
			{
				{ "v", "Increase verbosity level.", v => verbosity++ },
				{ "verbosity:", "Verbosity level. 0-4 are supported.", (ushort v) => settings.LoggingLevel = v },
				{ "n|namespace:", "Namespace generated code should exist in.", (string v) => settings.MasterNamespace = v },
				{ "c|connection:", "Name of connection string to use.", (string v) => settings.ConnectionString = v },
				{ "h|help", "Show this message.", v => help = v != null }
			};

			if (opts != null)
			{
				foreach (Option opt in opts)
					options.Add(opt);
			}

			try
			{
				extra = options.Parse(args).ToArray();
			}
			catch (OptionException ex)
			{
				Console.WriteLine("{0}: {1}", AssemblyName, ex.Message);
				Console.WriteLine("Try `{0} --help` for more information.", AssemblyName);
				return ReturnValue.InvalidOptions;
			}

			if (help)
			{
				PrintHelp(opts);
				return ReturnValue.Success;
			}

			if (verbosity != 0)
				settings.LoggingLevel = verbosity;

			Logger.Current = (Logger.Level)settings.LoggingLevel;
			Logger.Debug("Logging level: {0}", Logger.Current);

			string connectionName = AssemblyName + ".Properties.Settings." + settings.ConnectionString;
			if (ConfigurationManager.ConnectionStrings[connectionName] == null)
			{
				connectionName = settings.ConnectionString;
				if (ConfigurationManager.ConnectionStrings[connectionName] == null)
				{
					Logger.Error("Unknown connection: {0}", connectionName);
					return ReturnValue.UnknownConnection;
				}
			}

			Logger.Info("Using connection: {0}", ConfigurationManager.ConnectionStrings[connectionName]);
			conn = new SqlConnection(ConfigurationManager.ConnectionStrings[connectionName].ToString());

			try
			{
				conn.Open();
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine("Could not connect: {0}", ex.Message);
				return ReturnValue.ConnectFailed;
			}

			settings.CopyTo(ref appSettings);
			return ReturnValue.Success;
		}

		public static void PrintHelp(OptionSet opts)
		{
			Console.WriteLine("Usage: {0} [options] [output file/folder]", AssemblyName);
			Console.WriteLine(Description);
			Console.WriteLine("If no output file/folder is specified, the respective values from app.config are used.");
			Console.WriteLine();
			Console.WriteLine("Options:");
			opts.WriteOptionDescriptions(Console.Out);
		}

		public static int OpenWriter(string fileName, out StreamWriter file)
		{
			file = null;

			try
			{
				if (File.Exists(fileName))
				{
					FileAttributes attr = File.GetAttributes(fileName);
					if ((attr & FileAttributes.ReadOnly) > 0)
						File.SetAttributes(fileName, attr ^ FileAttributes.ReadOnly);
				}

				file = new StreamWriter(File.Open(fileName, FileMode.Create));
			}
			catch (UnauthorizedAccessException ex)
			{
				Logger.Error("Could not access output file: {0}", ex.Message);
				return ReturnValue.FileAccess;
			}
			catch (Exception ex)
			{
				Logger.Error("Unknown error: ({0}) {1}", ex.GetType().Name, ex.Message);
				return ReturnValue.FileAccess;
			}

			return ReturnValue.Success;
		}
	}
}
