﻿using System;
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
				Logger.Warn("Procedure missing namespaces: {0}", full);
				Name = full;
				Path = new string[] { Properties.Settings.Default.MiscClass };
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
			Settings = Properties.Settings.Default;

			Console.WriteLine("GenProc version {0}", Helpers.VersionString);

			bool help = false;
			OptionSet opts = new OptionSet()
			{
				{ "v", "Increase verbosity level.", v => Settings.LoggingLevel++ },
				{ "verbosity:", "Verbosity level. 0-4 are supported.", (ushort v) => Settings.LoggingLevel = v },
				{ "p|prefix:", "Prefix to use on naming collisions.", (string v) => Settings.CollisionPrefix = v },
				{ "m|monolithic", "Enable monolithic mode.", v => Settings.Monolithic = v != null },
				{ "n|namespace:", "Namespace generated code should exist in.", (string v) => Settings.MasterNamespace = v },
				{ "o|misc:", "Name of the class to use for procedures lacking underscores.", (string v) => Settings.MiscClass = v },
				{ "c|connection:", "Name of connection string to use.", (string v) => Settings.ConnectionString = v },
				{ "h|help", "Show this message.", v => help = v != null }
			};

			List<string> extra = null;
			try
			{
				extra = opts.Parse(args);
			}
			catch(OptionException ex)
			{
				Console.WriteLine("GenProc: {0}", ex.Message);
				Console.WriteLine("Try `GenProc --help` for more information.");
				return Return.InvalidOptions;
			}

			if(help)
			{
				PrintHelp(opts);
				return Return.Success;
			}

			Logger.Current = (Logger.Level)Settings.LoggingLevel;
			Logger.Debug("Logging level: {0}", Logger.Current);

			if (extra.Count > 0)
			{
				Settings.OutputDirectory = extra.First();
				Settings.MonolithicOutput = extra.First();
			}

			Settings.ConnectionString = "GenProc.Properties.Settings." + Settings.ConnectionString;
			if (ConfigurationManager.ConnectionStrings[Settings.ConnectionString] == null)
			{
				Logger.Error("Unknown connection: {0}", Settings.ConnectionString);
				return Return.UnknownConnection;
			}

			Stopwatch sw = new Stopwatch();
			sw.Start();

			int ret = (new Program()).Run(sw);

			sw.Stop();
			Logger.Info("Total time: {0}", sw.Elapsed);

			return ret;
		}

		private static void PrintHelp(OptionSet opts)
		{
			Console.WriteLine("Usage: GenProc [options] [output file/folder]");
			Console.WriteLine("Generate C# code for stored procedures in a SQL Server database.");
			Console.WriteLine("If no output file/folder is specified, the respective values from app.config are used.");
			Console.WriteLine();
			Console.WriteLine("Options:");
			opts.WriteOptionDescriptions(Console.Out);
		}

		private static class Return
		{
			public const int Success = 0;
			public const int ConnectFailed = 1;
			public const int ParseError = 2;
			public const int FileAccess = 3;
			public const int InvalidOptions = 4;
			public const int UnknownConnection = 5;
			public const int Unknown = 255;
		}

		public int Run(Stopwatch sw)
		{
			Settings = Properties.Settings.Default;

			Logger.Info("Using connection: {0}", ConfigurationManager.ConnectionStrings[Settings.ConnectionString]);
			SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings[Settings.ConnectionString].ToString());

			try
			{
				conn.Open();
			}
			catch (SqlException ex)
			{
				Logger.Error("Could not connect: {0}", ex.Message);
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
				while (reader.Read())
				{
					string procName = reader["procedure"].ToString();
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

					if (reader["parameter"] == DBNull.Value)
						continue;

					Parameter p = new Parameter(
						reader["parameter"].ToString(),
						reader["type"].ToString(),
						Convert.ToBoolean(reader["output"]),
						reader["value"].ToString().Trim()
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

				// Add last procedure
				if (proc != Procedure.None)
					trunk.Insert(proc.Path, proc);
			}
			catch (Exception ex)
			{
				Logger.Error("Error parsing data: {0}", ex.Message);
				return Return.ParseError;
			}
			finally
			{
				if (conn.State == ConnectionState.Open)
					conn.Close();
			}

			Logger.Info("Done database stuff: {0}", sw.Elapsed);
			Logger.Debug("Time spent inserting: {0}", inserts);

			string path = Settings.OutputDirectory;

			// Clean out old code
			if (Directory.Exists(path))
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
					if (File.Exists(Settings.MonolithicOutput))
					{
						FileAttributes attr = File.GetAttributes(Settings.MonolithicOutput);
						if ((attr & FileAttributes.ReadOnly) > 0)
							File.SetAttributes(Settings.MonolithicOutput, attr ^ FileAttributes.ReadOnly);
					}

					StreamWriter writer = new StreamWriter(File.Open(Settings.MonolithicOutput, FileMode.Create));
					writer.Write(f.TransformText());
					writer.Close();
				}
				catch (UnauthorizedAccessException ex)
				{
					Logger.Error("Could not access output file: {0}", ex.Message);
					return Return.FileAccess;
				}
				catch (Exception ex)
				{
					Logger.Error("Unknown error: ({0}) {1}", ex.GetType().Name, ex.Message);
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
