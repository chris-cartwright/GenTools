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
			{ "flag",				typeof(bool) },
			{ "hierarchyid",		typeof(string) },
			{ "tinyint",			typeof(byte) },
			{ "nchar",				typeof(string) },
			{ "char",				typeof(string) },
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
			if (assembly == null)
				assembly = Assembly.GetExecutingAssembly();

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
			if (str.Length == 0)
				return String.Empty;

			string ret = str.Trim().TrimStart('@');

			if (Char.IsDigit(ret[0]))
				ret = "_" + ret.Substring(1);

			ret = new Regex(@"[^\w0-9]").Replace(ret, "_");
			ret = new Regex("_+").Replace(ret, "_");

			return ret.CleanKeyword();
		}

		public static string GetConfig(string[] args)
		{
			bool help = false;
			string connection = null;
			OptionSet options = new OptionSet()
			{
				{ "k|name:", "Name of connection string to use.", (string v) => connection = v },
				{ "h|help", "Show this message.", v => help = v != null }
			};

			try
			{
				options.Parse(args);
			}
			catch (OptionException ex)
			{
				Console.WriteLine("{0}: {1}", AssemblyName, ex.Message);
				Console.WriteLine("Try `{0} -n help --help` for more information.", AssemblyName);
				throw new ReturnException(ReturnCode.InvalidOptions);
			}

			if (help)
				PrintShortHelp();

			return connection;
		}

		public static string[] Setup<T>(string[] args, ref T appConfig, out SqlConnection conn, OptionSet opts = null)
		{
			Console.WriteLine("{0} version {1}", AssemblyName, VersionString);
			
			conn = null;

			// Convert appConfig to internal class because out/ref parameters cannot be used in lambdas
			ConfigurationElementBase config = new ConfigurationElementBase();
			config.CopyFrom(ref appConfig);

			short verbosity = -1;
			bool help = false;
			OptionSet options = new OptionSet()
			{
				{ "v", "Increase verbosity level.", v => verbosity++ },
				{ "verbosity:", "Verbosity level. 0-4 are supported.", (ushort v) => config.LoggingLevel = (Logger.Level)v },
				{ "n|namespace:", "Namespace generated code should exist in.", (string v) => config.MasterNamespace = v },
				{ "c|connection:", "Connection string for database.", (string v) => config.ConnectionString = v },
				// Swallow name so it doesn't come back as an extra
				{ "k|name:", "Name of connection string to use.", (string v) => config.Name = v },
				{ "h|help", "Show this message.", v => help = v != null }
			};

			if (opts != null)
			{
				foreach (Option opt in opts)
					options.Add(opt);
			}

			if (help)
			{
				PrintHelp(options);
				return null;
			}

			string[] extra;
			try
			{
				extra = options.Parse(args).ToArray();
			}
			catch (OptionException ex)
			{
				Console.WriteLine("{0}: {1}", AssemblyName, ex.Message);
				Console.WriteLine("Try `{0} -n help --help` for more information.", AssemblyName);
				throw new ReturnException(ReturnCode.InvalidOptions);
			}

			if (help)
				PrintHelp(options);

			if (verbosity != -1)
				config.LoggingLevel = (Logger.Level)(++verbosity);

			Logger.Current = (Logger.Level)config.LoggingLevel;
			Logger.Debug("Logging level: {0}", Logger.Current);

			string connection = null;
			if (config.Name != null)
			{
				if (ConfigurationManager.ConnectionStrings[config.Name] == null)
				{
					Logger.Error("Unknown connection: {0}", config.Name);
					throw new ReturnException(ReturnCode.UnknownConnection);
				}

				connection = ConfigurationManager.ConnectionStrings[config.Name].ToString();
			}
			else
				connection = config.ConnectionString;

			Logger.Info("Using connection: {0}", connection);
			conn = new SqlConnection(connection);

			try
			{
				conn.Open();
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine("Could not connect: {0}", ex.Message);
				throw new ReturnException(ReturnCode.ConnectFailed);
			}

			config.CopyTo(ref appConfig);

			return extra;
		}

		public static void PrintHelp(OptionSet opts)
		{
			Console.WriteLine("Usage: {0} [options] [output file/folder]", AssemblyName);
			Console.WriteLine(Description);
			Console.WriteLine("If no output file/folder is specified, the respective values from app.config are used.");
			Console.WriteLine();
			Console.WriteLine("Options:");
			opts.WriteOptionDescriptions(Console.Out);
			Environment.Exit((int)ReturnCode.Success);
		}

		public static void PrintShortHelp()
		{
			Console.WriteLine("Usage: {0} [options] [output file/folder]", AssemblyName);
			Console.WriteLine(Description);
			Console.WriteLine("If no output file/folder is specified, the respective values from app.config are used.");
			Console.WriteLine("For full help listing, provide any value for --name");
			Environment.Exit((int)ReturnCode.Success);
		}

		public static StreamWriter OpenWriter(string fileName)
		{
			StreamWriter writer;

			try
			{
				if (File.Exists(fileName))
				{
					FileAttributes attr = File.GetAttributes(fileName);
					if ((attr & FileAttributes.ReadOnly) > 0)
						File.SetAttributes(fileName, attr ^ FileAttributes.ReadOnly);
				}

				writer = new StreamWriter(File.Open(fileName, FileMode.Create));
			}
			catch (UnauthorizedAccessException ex)
			{
				Logger.Error("Could not access output file: {0}", ex.Message);
				throw new ReturnException(ReturnCode.FileAccess);
			}
			catch (Exception ex)
			{
				Logger.Error("Unknown error: ({0}) {1}", ex.GetType().Name, ex.Message);
				throw new ReturnException(ReturnCode.FileAccess);
			}

			return writer;
		}
	}
}
