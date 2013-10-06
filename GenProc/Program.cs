using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Common;
using Mono.Options;

namespace GenProc
{
	public class CustomType
	{
		private readonly Type _type;

		public string Name { get; private set; }
		public bool IsValueType { get; private set; }
		public bool IsArray { get; private set; }
		public bool IsTable { get; private set; }

		//public static implicit operator CustomType(Type type)
		//{
		//	return new CustomType(type);
		//}

		public static bool operator ==(CustomType lhs, Type rhs)
		{
			return lhs._type == rhs;
		}

		public static bool operator !=(CustomType lhs, Type rhs)
		{
			return !(lhs == rhs);
		}

		protected bool Equals(CustomType other)
		{
			return _type == other._type && string.Equals(Name, other.Name) && IsValueType.Equals(other.IsValueType) && IsArray.Equals(other.IsArray);
		}

		/// <summary>
		/// Constructs a table type
		/// </summary>
		/// <param name="name">Name of user table type</param>
		public CustomType(string name)
		{
			if (name == null)
				throw new ArgumentNullException("name");

			Name = name;
			IsValueType = false;
			IsArray = true;
			IsTable = true;
		}

		public CustomType(Type type)
		{
			_type = type;
			Name = type.Name;
			IsValueType = type.IsValueType;
			IsArray = type.IsArray;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
				return false;

			if (ReferenceEquals(this, obj))
				return true;

			if (obj.GetType() != GetType())
				return false;

			return Equals((CustomType)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hashCode = (_type != null ? _type.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ Name.GetHashCode();
				hashCode = (hashCode * 397) ^ IsValueType.GetHashCode();
				hashCode = (hashCode * 397) ^ IsArray.GetHashCode();
				return hashCode;
			}
		}
	}

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

	public abstract class Entity
	{
		private string _name;

		public string Name
		{
			get { return _name; }
			set
			{
				_name = value;
				NameClean = value.CleanName();
			}
		}

		public string NameClean;
	}

	public class Column : Entity
	{
		public CustomType Type;
		public SqlDbType SqlType;
		public bool IsNull;
		public int Size;

		public Column(string name, CustomType type, SqlDbType sqlType, bool isNull, int size)
		{
			IsNull = isNull;
			Size = size;
			Name = name;
			Type = type;
			SqlType = sqlType;
		}
	}

	public class Parameter : Column
	{
		public bool IsOutput;
		public string Default;

		public Parameter(string name, CustomType type, SqlDbType sqlType, int size, bool output, string def)
			: base(name, type, sqlType, false, size)
		{
			Size = size;
			IsOutput = output;
			Default = def;
		}
	}

	public class Procedure : Entity
	{
		public static readonly Procedure None;

		static Procedure()
		{
			// Name must be at least two characters
			None = new Procedure();
		}

		public string[] Path;
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

	public class TableType : Entity
	{
		public static readonly TableType None;

		static TableType()
		{
			None = new TableType();
		}

		public List<Column> Columns;

		private TableType() { }

		public TableType(string name)
		{
			Name = name;
			Columns = new List<Column>();
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

				prog.LoadTableTypes(conn);
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

		private readonly Configuration _settings;

		public Branch<Procedure> Procedures;
		public List<TableType> TableTypes;

		public Program(Configuration settings)
		{
			_settings = settings;
		}

		public CustomType ResolveType(string sqlType)
		{
			sqlType = sqlType.ToLower();
			TableType tableType = TableTypes.FirstOrDefault(tt => tt.Name.ToLower() == sqlType);

			if (Helpers.TypeMap.ContainsKey(sqlType))
				return new CustomType(Helpers.TypeMap[sqlType]);

			if (tableType != null)
				return new CustomType(tableType.Name);

			Logger.Warn("Could not find type: {0}", sqlType);
			return new CustomType(typeof(object));
		}

		public SqlDbType ResolveSqlType(string sqlType)
		{
			sqlType = sqlType.ToLower();
			TableType tableType = TableTypes.FirstOrDefault(tt => tt.Name.ToLower() == sqlType);

			if (Helpers.SqlTypeMap.ContainsKey(sqlType))
				return Helpers.SqlTypeMap[sqlType];

			if (tableType != null)
				return SqlDbType.Structured;

			Logger.Warn("Could not find SQL type: {0}", sqlType);
			return SqlDbType.VarBinary;
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

				StringBuilder tableTypes = new StringBuilder();
				foreach (TableType tableType in TableTypes)
				{
					Templates.TableType tt = new Templates.TableType() { Session = new Dictionary<string, object>() };
					tt.Session["tableType"] = tableType;
					tt.Initialize();
					tableTypes.Append(tt.TransformText());
				}

				Templates.File f = new Templates.File() { Session = new Dictionary<string, object>() };
				f.Session["namespace"] = _settings.MasterNamespace;
				f.Session["class"] = str.ToString();
				f.Session["tableTypes"] = tableTypes.ToString();
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
						ResolveType(reader["type"].ToString()),
						ResolveSqlType(reader["type"].ToString()),
						Convert.ToInt32(reader["size"]),
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
						else if (p.Type == typeof(string))
							p.Default = p.Default.Trim('\'');
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

				reader.Close();

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

		public void LoadTableTypes(SqlConnection conn)
		{
			TableTypes = new List<TableType>();

			SqlCommand cmd = new SqlCommand("p_ListUserTableTypes", conn) { CommandType = CommandType.StoredProcedure };

			Stopwatch sw = new Stopwatch();
			sw.Start();

			try
			{
				SqlDataReader reader = cmd.ExecuteReader();
				TableType tableType = TableType.None;
				while (reader.Read())
				{
					string typeName = reader["table"].ToString();
					if (typeName != tableType.Name)
					{
						if (tableType != TableType.None)
							TableTypes.Add(tableType);

						tableType = new TableType(typeName);
					}

					tableType.Columns.Add(new Column(
						reader["column"].ToString(),
						ResolveType(reader["type"].ToString()),
						ResolveSqlType(reader["type"].ToString()),
						Convert.ToBoolean(reader["nullable"]),
						Convert.ToInt32(reader["size"])
					));
				}

				reader.Close();

				// Add last table type
				if (tableType != TableType.None)
					TableTypes.Add(tableType);
			}
			catch (Exception ex)
			{
				Logger.Error("Error parsing data: {0}", ex.Message);
				throw new ReturnException(ReturnCode.ParseError);
			}

			sw.Stop();
			Logger.Info("Done database stuff: {0}", sw.Elapsed);
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
