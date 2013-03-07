using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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

		static Helpers()
		{
			Assembly assembly = Assembly.GetEntryAssembly();
			Version = assembly.GetName().Version;

			object[] rev = assembly.GetCustomAttributes(typeof(GitRevisionAttribute), true);
			if (rev.Length > 0)
				Revision = (GitRevisionAttribute)rev[0];
			else
				Revision = new GitRevisionAttribute("unknown-e", true);

			VersionString = String.Format("{0}-{1}-{2}", Version, Revision.Revision, Revision.Dirty ? "dirty" : "clean");
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
			return str.TrimStart('@').CleanKeyword();
		}
	}
}
