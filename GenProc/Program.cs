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
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;

namespace GenProc
{
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
		public readonly Dictionary<string, Type> ParameterTypeMap = new Dictionary<string, Type>()
		{
			{ "nvarchar",		typeof(string) },
			{ "varchar",		typeof(string) },
			{ "int",			typeof(int) },
			{ "bigint",			typeof(long) },
			{ "smallint",		typeof(short) },
			{ "bit",			typeof(bool) },
			{ "datetime",		typeof(DateTime) },
			{ "money",			typeof(decimal) },
			{ "Flag",			typeof(bool) },
			{ "hierarchyid",	typeof(string) },
			{ "tinyint",		typeof(byte) },
			{ "nchar",			typeof(string) }
		};

		public string Name;
		public Type Type;
		public bool Output;
		public string Default;

		public Parameter(string name, Type type, bool output, string def)
		{
			Name = name;
			Type = type;
			Output = output;
			Default = def;
		}

		public Parameter(string name, string sqlType, bool output, string def)
		{
			Name = name;
			Output = output;
			Default = def;

			if (ParameterTypeMap.ContainsKey(sqlType))
				Type = ParameterTypeMap[sqlType];
			else
				Console.Error.WriteLine("Could not find type: {0}", sqlType);
		}
	}

	public class Procedure
	{
		public string[] Path;
		public string Name;
		public List<Parameter> Parameters;

		private Procedure()
		{
			Path = new string[0];
			Parameters = new List<Parameter>();
		}

		public Procedure(string full)
			: this()
		{
			if (full.Substring(0, 2) == "p_")
				full = full.Substring(2);

			string[] parts = full.Split('_');
			if (parts.Length == 1)
			{
				Console.Error.WriteLine("Procedure missing namespaces: {0}", full);
				Name = full;
				Path = new string[] { Properties.Settings.Default.MasterClass };
				return;
			}

			Path = parts.Take(parts.Length - 1).ToArray();
			Name = parts.Last();
		}

		public Procedure(string[] path, string name)
			: this()
		{
			Path = path;
			Name = name;
		}
	}

	public class Program
	{
		static void Main(string[] args)
		{
			(new Program()).Run();
		}

		public void Run()
		{
			Console.WriteLine("GenProc version {0}\n", Assembly.GetExecutingAssembly().GetName().Version);
			Console.WriteLine("Using connection: {0}", Properties.Settings.Default.DatabaseConnection);
			Console.WriteLine("Database: {0}", Properties.Settings.Default.DatabaseName);

			SqlConnection conn = new SqlConnection(Properties.Settings.Default.DatabaseConnection);
			conn.Open();

			Branch<Procedure> trunk = new Branch<Procedure>(Properties.Settings.Default.MasterNamespace);

			Server server = new Server(new ServerConnection(conn));
			Database db = server.Databases[Properties.Settings.Default.DatabaseName];
			foreach (DataRow row in db.EnumObjects(DatabaseObjectTypes.StoredProcedure).Rows)
			{
				string schema = row["Schema"].ToString();
				if (schema == "sys" || schema == "INFORMATION_SCHEMA")
					continue;

				StoredProcedure sp = (StoredProcedure)server.GetSmoObject(new Urn(row["Urn"].ToString()));
				if (sp.IsSystemObject)
					continue;

				Procedure proc = new Procedure(sp.Name);
				foreach (StoredProcedureParameter param in sp.Parameters)
				{
					Parameter p = new Parameter(param.Name, param.DataType.Name, param.IsOutputParameter, param.DefaultValue);
					if (p.Type == typeof(string))
						p.Default = '"' + p.Default.Trim('\'') + '"';

					proc.Parameters.Add(p);
				}

				trunk.Insert(proc.Path, proc);
			}

			conn.Close();

			//string path = Path.GetFullPath(Properties.Settings.Default.OutputDirectory);
			string path = Properties.Settings.Default.OutputDirectory;

			// Clean out old code
			if(Directory.Exists(path))
				Directory.Delete(path, true);

			WriteCode(trunk, path, "");
		}

		private void WriteCode(Branch<Procedure> branch, string path, string node, int depth = 0)
		{
			node = node.TrimStart('.');

			Console.WriteLine("{0}Path: {1}", new String('\t', depth), path);
			Directory.CreateDirectory(path);

			foreach (Branch<Procedure> brch in branch.Branches)
				WriteCode(brch, Path.Combine(path, branch.Name), node + "." + branch.Name, depth + 1);

			if (branch.Leaves.Count == 0)
				return;

			string file = Path.Combine(path, branch.Name) + ".cs";
			Console.WriteLine("{0}File: {1}", new String('\t', depth), file);
			StreamWriter tw = new StreamWriter(File.Create(file));

			StringBuilder funcs = new StringBuilder();
			foreach (Procedure proc in branch.Leaves.OrderBy(p => p.Name))
			{
				Console.WriteLine("{0}Procedure: {1}", new String('\t', depth), proc.Name);

				Templates.Function func = new Templates.Function();
				func.Session = new Dictionary<string, object>();
				func.Session["name"] = proc.Name;
				func.Session["parameters"] = proc.Parameters.ToArray();
				func.Initialize();
				funcs.Append(func.TransformText());
			}

			Templates.File f = new Templates.File();
			f.Session = new Dictionary<string, object>();
			f.Session["namespace"] = node;
			f.Session["className"] = branch.Name;
			f.Session["functions"] = funcs.ToString();
			f.Initialize();
			tw.Write(f.TransformText());

			tw.Close();
		}
	}
}
