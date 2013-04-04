using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Common;
using Mono.Options;

namespace GenProc
{
	public class Branch<T>
	{
		private static Branch<T> Resolve(Branch<T> branch, string[] parts)
		{
			Branch<T> brch = branch.Branches.FirstOrDefault(b => b.Name == parts[0]);
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
			if (ret.Leaves.Contains(leaf))
				throw new ArgumentException("Cannot have duplicate leaves.");

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

			sqlType = sqlType.ToLower();
			if (Helpers.TypeMap.ContainsKey(sqlType))
				Type = Helpers.TypeMap[sqlType];
			else
			{
				Type = typeof(object);
				Logger.Warn("Could not find type: {0}", sqlType);
			}
		}
	}

	public class Procedure
	{
		public static readonly Procedure None;

		static Procedure()
		{
			// Name must be at least two characters
			None = new Procedure();
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

		private Procedure() { }

		public Procedure(string full, string name, string[] path)
		{
			Parameters = new List<Parameter>();

			Original = full;
			Name = name;
			Path = path;
		}
	}

	public class Program
	{
		public static int Main(string[] args)
		{
			SqlConnection conn = null;

			try
			{
				ConfigurationSection section = (ConfigurationSection)ConfigurationManager.GetSection("outputs");
				string name = Helpers.GetConfig(args);
				Configuration config = String.IsNullOrWhiteSpace(name) ? new Configuration() : section.Get(name);

				OptionSet opts = new OptionSet()
				{
					{ "p|prefix:", "Prefix to use on naming collisions.", v => config.CollisionPrefix = v },
					{ "m|monolithic", "Enable monolithic mode.", v => config.Monolithic = v != null },
					{ "o|misc:", "Name of the class to use for procedures lacking underscores.", v => config.MiscClass = v }
				};

				string[] extra = Helpers.Setup(args, ref config, out conn, opts);

				if (extra.Length > 0)
				{
					config.OutputDirectory = extra.First();
					config.OutputFile = extra.First();
				}

				Program prog = new Program(config);

				Stopwatch sw = new Stopwatch();
				sw.Start();

				prog.LoadProcedures(conn);
				prog.Write();

				sw.Stop();
				Logger.Info("Total time: {0}", sw.Elapsed);
			}
			catch (ReturnException ex)
			{
				return (int)ex.Code;
			}
			finally
			{
				if (conn != null && conn.State == ConnectionState.Open)
					conn.Close();
			}

			return (int)ReturnCode.Success;
		}

		private Configuration _settings;

		public Branch<Procedure> Procedures;

		public Program(Configuration settings)
		{
			_settings = settings;
		}

		public Procedure Parse(string full)
		{
			string name = full;
			string prefix = full.Substring(0, 2);
			if (prefix == "p_" || prefix == "s_")
				name = full.Substring(2);

			string[] parts = name.Split('_').Where(p => p.Length > 0).ToArray();
			if (parts.Length == 1)
			{
				Logger.Warn("Procedure missing namespaces: {0}", full);
				return new Procedure(full, name, new[] { _settings.MiscClass });
			}

			return new Procedure(full, parts.Last(), parts.Take(parts.Count() - 1).ToArray());
		}

		public void Write()
		{
			string path = _settings.OutputDirectory;

			// Clean out old code
			if (Directory.Exists(path))
				Directory.Delete(path, true);

			if (_settings.Monolithic)
			{
				StringWriter str = new StringWriter();
				WriteCodeMonolithic(str, Procedures, true);

				Templates.File f = new Templates.File() { Session = new Dictionary<string, object>() };
				f.Session["namespace"] = _settings.MasterNamespace;
				f.Session["class"] = str.ToString();
				f.Initialize();

				StreamWriter writer = Helpers.OpenWriter(_settings.OutputFile);

				writer.Write(f.TransformText());
				writer.Close();
			}
			else
				WriteCodeMulti(Procedures, path, "");
		}

		public void LoadProcedures(SqlConnection conn)
		{
			Procedures = new Branch<Procedure>(_settings.MasterNamespace);
			TimeSpan inserts = new TimeSpan();

			SqlCommand cmd = new SqlCommand("p_ListProcedures", conn) { CommandType = CommandType.StoredProcedure };

			Stopwatch sw = new Stopwatch();
			sw.Start();

			try
			{
				SqlDataReader reader = cmd.ExecuteReader();
				Procedure proc = Procedure.None;
				while (reader.Read())
				{
					string procName = reader["procedure"].ToString();
					if (procName != proc.Original)
					{
						if (proc != Procedure.None)
						{
							TimeSpan ts = sw.Elapsed;
							Procedures.Insert(proc.Path, proc);
							inserts += sw.Elapsed - ts;
						}

						proc = Parse(procName);
					}

					if (reader["parameter"] == DBNull.Value)
						continue;

					Parameter p = new Parameter(
						reader["parameter"].ToString(),
						reader["type"].ToString(),
						Convert.ToBoolean(reader["output"]),
						reader["value"].ToString().Trim()
					);
					if (!String.IsNullOrWhiteSpace(p.Default))
					{
						if (p.Default.ToLower() == "null")
						{
							p.IsNull = true;
							p.Default = null;
						}
						else if (p.Type == typeof (string))
							p.Default = p.Default.Trim('\'');
						else if (p.Type == typeof (bool))
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

				// Add last procedure
				if (proc != Procedure.None)
					Procedures.Insert(proc.Path, proc);
			}
			catch (Exception ex)
			{
				Logger.Error("Error parsing data: {0}", ex.Message);
				throw new ReturnException(ReturnCode.ParseError);
			}

			sw.Stop();
			Logger.Info("Done database stuff: {0}", sw.Elapsed);
			Logger.Debug("Time spent inserting: {0}", inserts);
		}

		private void WriteCodeMulti(Branch<Procedure> branch, string path, string node)
		{
			node = node.TrimStart('.');
			Directory.CreateDirectory(path);

			foreach (Branch<Procedure> brch in branch.Branches)
				WriteCodeMulti(brch, Path.Combine(path, branch.Name), node + "." + branch.Name);

			if (branch.Leaves.Count == 0)
				return;

			string className = branch.Name;
			if (branch.Branches.Count > 0)
				className = _settings.CollisionPrefix + className;

			string file = Path.Combine(path, className) + ".cs";
			StreamWriter tw = new StreamWriter(File.Create(file));

			StringBuilder funcs = new StringBuilder();
			foreach (Procedure proc in branch.Leaves.OrderBy(p => p.Name))
			{
				Templates.Function func = new Templates.Function { Session = new Dictionary<string, object>() };
				func.Session["name"] = proc.Name;
				func.Session["parameters"] = proc.Parameters.ToArray();
				func.Session["procedure"] = proc.Original;
				func.Initialize();
				funcs.Append(func.TransformText());
			}

			Templates.Class c = new Templates.Class() { Session = new Dictionary<string, object>() };
			c.Session["functions"] = funcs.ToString();
			c.Session["className"] = className;
			c.Initialize();

			Templates.File f = new Templates.File() { Session = new Dictionary<string, object>() };
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
					brch.Name = _settings.CollisionPrefix + brch.Name;

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
					Templates.Function func = new Templates.Function() { Session = new Dictionary<string, object>() };
					func.Session["procedure"] = proc;
					func.Initialize();
					funcs.Append(func.TransformText());
				}
			}

			Templates.Class c = new Templates.Class() { Session = new Dictionary<string, object>() };
			c.Session["functions"] = funcs.ToString();
			c.Session["className"] = branch.Name;
			c.Session["classes"] = classes.ToString();
			c.Initialize();
			tw.Write(c.TransformText());
		}
	}
}
