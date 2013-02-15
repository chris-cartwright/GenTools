using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GenProc
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
			{ "sysname",			typeof(string) }
		};

		public static readonly Version Version;
		public static readonly string Revision;

		static Helpers()
		{
			Version = Assembly.GetExecutingAssembly().GetName().Version;
			Revision = ((GitRevisionAttribute)Assembly.GetExecutingAssembly().GetCustomAttribute(typeof(GitRevisionAttribute))).Revision;
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

	public class Branch<T>
	{
		private static Branch<T> Resolve(Branch<T> branch, string[] parts)
		{
			Branch<T> brch = branch.Branches.Where(b => b.Name == parts[0]).FirstOrDefault();
			if (brch == null)
			{
				brch = new Branch<T>(parts[0]);
				branch.Branches.Add(brch);
			}

			if (parts.Length == 1)
				return brch;

			return Resolve(brch, parts.Skip(1).ToArray());
		}

		public string Name;
		public List<Branch<T>> Branches;
		public List<T> Leaves;

		public Branch(string name)
		{
			Name = name;
			Branches = new List<Branch<T>>();
			Leaves = new List<T>();
		}

		public Branch<T> Insert(string[] parts, T leaf)
		{
			if (parts.Length == 0)
			{
				Leaves.Add(leaf);
				return this;
			}

			Branch<T> ret = Resolve(this, parts);
			ret.Leaves.Add(leaf);
			return ret;
		}
	}

	public class Parameter
	{
		private string _name;
		public string Name
		{
			get { return _name; }
			set { _name = value; NameClean = value.CleanName(); }
		}

		public string NameClean;
		public Type Type;
		public bool IsOutput;
		public string Default;
		public bool IsNull;
		
		public Parameter(string name, Type type, bool output, string def)
		{
			Name = name;
			Type = type;
			IsOutput = output;
			Default = def;
		}

		public Parameter(string name, string sqlType, bool output, string def)
		{
			Name = name;
			IsOutput = output;
			Default = def;

			if (Helpers.TypeMap.ContainsKey(sqlType))
				Type = Helpers.TypeMap[sqlType];
			else
			{
				Type = typeof(object);
				Console.Error.WriteLine("Could not find type: {0}", sqlType);
			}
		}
	}

	public class Procedure
	{
		public static readonly Procedure None;
		
		static Procedure()
		{
			// Name must be at least two characters
			None = new Procedure("THIS IS NOT A PROCEDURE");
		}

		private string _name;
		public string Name
		{
			get { return _name; }
			set { _name = value; NameClean = value.CleanName(); }
		}

		public string[] Path;
		public string NameClean;
		public string Original;
		public List<Parameter> Parameters;

		private Procedure()
		{
			Path = new string[0];
			Parameters = new List<Parameter>();
		}

		public Procedure(string full)
			: this()
		{
			Original = full;

			string prefix = full.Substring(0, 2);
			if (prefix == "p_" || prefix == "s_")
				full = full.Substring(2);

			IEnumerable<string> parts = full.Split('_').Where(p => p.Length > 0);
			if (parts.Count() == 1)
			{
				Console.Error.WriteLine("Procedure missing namespaces: {0}", full);
				Name = full;
				Path = new string[] { Properties.Settings.Default.MasterClass };
				return;
			}

			Path = parts.Take(parts.Count() - 1).ToArray();
			Name = parts.Last();
		}
	}

	public class Program
	{
		private static Properties.Settings Settings;

		static int Main(string[] args)
		{
			if (args.Length == 1)
			{
				Properties.Settings.Default.MonolithicOutput = args[0];
			}

			Stopwatch sw = new Stopwatch();
			sw.Start();

			int ret = (new Program()).Run(sw);

			sw.Stop();
			Console.WriteLine("Total time: {0}", sw.Elapsed);

			return ret;
		}

		private static class Return
		{
			public const int Success = 0;
			public const int ConnectFailed = 1;
			public const int ParseError = 2;
			public const int FileAccess = 3;
		}

		public int Run(Stopwatch sw)
		{
			Settings = Properties.Settings.Default;

			Console.WriteLine("\nGenProc version {0}-{1}\n", Helpers.Version, Helpers.Revision);
			Console.WriteLine("Using connection: {0}", Settings.DatabaseConnection);

			SqlConnection conn = new SqlConnection(Settings.DatabaseConnection);

			try
			{
				conn.Open();
			}
			catch (SqlException ex)
			{
				Console.Error.WriteLine("Could not connect: {0}", ex.Message);
				return Return.ConnectFailed;
			}

			Branch<Procedure> trunk = new Branch<Procedure>(Settings.MasterNamespace);
			TimeSpan inserts = new TimeSpan();

			SqlCommand cmd = new SqlCommand("p_ListProcedures", conn);
			cmd.CommandType = CommandType.StoredProcedure;

			try
			{
				SqlDataReader reader = cmd.ExecuteReader();
				Procedure proc = Procedure.None;
				while(reader.Read())
				{
					string procName = reader["Procedure"].ToString();
					if (procName != proc.Original)
					{
						if (proc != Procedure.None)
						{
							TimeSpan ts = sw.Elapsed;
							trunk.Insert(proc.Path, proc);
							inserts += sw.Elapsed - ts;
						}

						proc = new Procedure(procName);
					}

					Parameter p = new Parameter(
						reader["Parameter"].ToString(),
						reader["Type"].ToString(),
						Convert.ToBoolean(reader["Output"]),
						reader["Value"].ToString().Trim()
					);
					if (!p.IsOutput)
					{
						if (p.Default.ToLower() == "null")
							p.IsNull = true;
						else if (p.Type == typeof(string))
							p.Default = '"' + p.Default.Trim('\'') + '"';
						else if (p.Type == typeof(bool))
							p.Default = p.Default == "0" ? "false" : "true";
					}
					else
						p.Default = null;

					if (p.NameClean == proc.NameClean)
					{
						p.NameClean = Char.ToLower(p.NameClean[0]) + p.NameClean.Substring(1);
						if (p.NameClean == proc.NameClean)
							p.NameClean = Char.ToUpper(p.NameClean[0]) + p.NameClean.Substring(1);
					}

					proc.Parameters.Add(p);
				}
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine("Error parsing data: {0}", ex.Message);
				return Return.ParseError;
			}
			finally
			{
				if(conn.State == ConnectionState.Open)
					conn.Close();
			}

			Console.WriteLine("Done database stuff: {0}", sw.Elapsed);
			Console.WriteLine("Time spent inserting: {0}", inserts);

			string path = Settings.OutputDirectory;

			// Clean out old code
			if(Directory.Exists(path))
				Directory.Delete(path, true);

			if (Settings.Monolithic)
			{
				StringWriter str = new StringWriter();
				WriteCodeMonolithic(str, trunk, true);

				Templates.File f = new Templates.File();
				f.Session = new Dictionary<string, object>();
				f.Session["namespace"] = Settings.MasterNamespace;
				f.Session["class"] = str.ToString();
				f.Initialize();

				try
				{
					FileAttributes attr = File.GetAttributes(Settings.MonolithicOutput);
					if ((attr & FileAttributes.ReadOnly) > 0)
						File.SetAttributes(Settings.MonolithicOutput, attr ^ FileAttributes.ReadOnly);

					StreamWriter writer = new StreamWriter(File.Open(Settings.MonolithicOutput, FileMode.Create));
					writer.Write(f.TransformText());
					writer.Close();
				}
				catch (UnauthorizedAccessException ex)
				{
					Console.Error.WriteLine("Could not access output file: ({0}) {1}", ex.GetType().Name, ex.Message);
					return Return.FileAccess;
				}
			}
			else
				WriteCodeMulti(trunk, path, "");

			return Return.Success;
		}

		private void WriteCodeMulti(Branch<Procedure> branch, string path, string node, int depth = 0)
		{
			node = node.TrimStart('.');
			Directory.CreateDirectory(path);

			foreach (Branch<Procedure> brch in branch.Branches)
				WriteCodeMulti(brch, Path.Combine(path, branch.Name), node + "." + branch.Name, depth + 1);

			if (branch.Leaves.Count == 0)
				return;

			string className = branch.Name;
			if (branch.Branches.Count > 0)
				className = Properties.Settings.Default.CollisionPrefix + className;

			string file = Path.Combine(path, className) + ".cs";
			StreamWriter tw = new StreamWriter(File.Create(file));

			StringBuilder funcs = new StringBuilder();
			foreach (Procedure proc in branch.Leaves.OrderBy(p => p.Name))
			{
				Templates.Function func = new Templates.Function();
				func.Session = new Dictionary<string, object>();
				func.Session["name"] = proc.Name;
				func.Session["parameters"] = proc.Parameters.ToArray();
				func.Session["procedure"] = proc.Original;
				func.Initialize();
				funcs.Append(func.TransformText());
			}

			Templates.Class c = new Templates.Class();
			c.Session = new Dictionary<string, object>();
			c.Session["functions"] = funcs.ToString();
			c.Session["className"] = className;
			c.Initialize();
			
			Templates.File f = new Templates.File();
			f.Session = new Dictionary<string, object>();
			f.Session["namespace"] = node;
			f.Session["class"] = c.TransformText();
			f.Initialize();
			tw.Write(f.TransformText());

			tw.Close();
		}

		private void WriteCodeMonolithic(TextWriter tw, Branch<Procedure> branch, bool first = false)
		{
			StringWriter classes = new StringWriter();
			foreach (Branch<Procedure> brch in branch.Branches)
			{
				if (branch.Leaves.Count(p => p.Name == brch.Name) > 0)
					brch.Name = Properties.Settings.Default.CollisionPrefix + brch.Name;

				WriteCodeMonolithic(classes, brch);
			}

			if (first)
			{
				tw.Write(classes.ToString());
				return;
			}

			StringBuilder funcs = new StringBuilder();
			if (branch.Leaves.Count > 0)
			{
				foreach (Procedure proc in branch.Leaves.OrderBy(p => p.Name))
				{
					Templates.Function func = new Templates.Function();
					func.Session = new Dictionary<string, object>();
					func.Session["procedure"] = proc;
					func.Initialize();
					funcs.Append(func.TransformText());
				}
			}

			Templates.Class c = new Templates.Class();
			c.Session = new Dictionary<string, object>();
			c.Session["functions"] = funcs.ToString();
			c.Session["className"] = branch.Name;
			c.Session["classes"] = classes.ToString();
			c.Initialize();
			tw.Write(c.TransformText());
		}
	}
}
