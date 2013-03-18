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

namespace GenTypes
{
	public class Column
	{
		public string Name;
		public Type Type;
		public bool IsNull;
		public bool IsIdentity;

		public Column(string name, Type type)
		{
			Name = name;
			Type = type;
		}

		public Column(string name, string sqlType, bool isNull, bool isIdent)
		{
			Name = name;
			IsNull = isNull;
			IsIdentity = isIdent;

			if (Helpers.TypeMap.ContainsKey(sqlType))
				Type = Helpers.TypeMap[sqlType];
			else
			{
				Type = typeof(object);
				Logger.Warn("Could not find type: {0}", sqlType);
			}
		}
	}

	public class Table
	{
		public static readonly Table None = new Table("");

		public string Name;
		public Dictionary<string, object> Data;
		public List<Column> Columns;

		private Column _identity;
		public Column Identity
		{
			get
			{
				if (_identity == null)
					_identity = Columns.Where(c => c.IsIdentity).First();

				return _identity;
			}
		}

		public Table(string name)
		{
			Name = name;
			Data = new Dictionary<string, object>();
			Columns = new List<Column>();
		}
	}

	public class Data
	{
		public class Comparer : IEqualityComparer<Data>
		{
			public bool Equals(Data x, Data y)
			{
				return x.Name == y.Name;
			}

			public int GetHashCode(Data obj)
			{
				return obj.Name.GetHashCode();
			}
		}

		private string _name;
		public string Name
		{
			get { return _name; }
			set { _name = value; NameClean = value.CleanName(); }
		}

		public string NameClean;
		public object ID;
	}

	public class Mapping : HashSet<Data>
	{
		private string _name;
		public string Name
		{
			get { return _name; }
			set { _name = value; NameClean = value.CleanName(); }
		}

		public string NameClean;
		public Type IdType;

		public Mapping()
			: base(new Data.Comparer())
		{
		}
	}

	public class Program
	{
		private static int Main(string[] args)
		{
			SqlConnection conn = null;

			try
			{
				ConfigurationSection section = (ConfigurationSection)ConfigurationManager.GetSection("outputs");
				Configuration config;
				string name = Helpers.GetConfig(args);
				if (String.IsNullOrWhiteSpace(name))
					config = new Configuration();
				else
					config = section.Get(name);

				string[] extra = Helpers.Setup(args, ref config, out conn);

				if (extra.Length > 0)
					config.OutputFile = extra.First();

				Program prog = new Program(config);

				Stopwatch sw = new Stopwatch();
				sw.Start();

				prog.LoadTables(conn);
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

		public List<Mapping> Mappings;

		public Program(Configuration settings)
		{
			_settings = settings;
			Mappings = new List<Mapping>();
		}

		public void LoadTables(SqlConnection conn)
		{
			SqlCommand cmd = new SqlCommand("p_ListTypeTables", conn) { CommandType = CommandType.StoredProcedure };

			try
			{
				Mappings = new List<Mapping>();
				SqlDataReader reader = cmd.ExecuteReader();
				Table table = Table.None;
				List<Table> tables = new List<Table>();
				while (reader.Read())
				{
					string tableName = reader["table"].ToString();
					if (tableName != table.Name)
					{
						if (table != Table.None)
						{
							Logger.Debug("Found table: {0}", table.Name);
							tables.Add(table);
						}

						table = new Table(tableName);
					}

					Column col = new Column(reader["column"].ToString(), reader["type"].ToString(), (bool)reader["nullable"], (bool)reader["identity"]);

					table.Columns.Add(col);
				}

				reader.Close();

				Logger.Debug("Found table: {0}", table.Name);
				tables.Add(table);

				foreach (Table t in tables)
				{
					SqlCommand fetcher = new SqlCommand("select * from [" + t.Name + "]", conn);
					reader = fetcher.ExecuteReader();

					string dataColumn = t.Identity.Name.Substring(0, t.Identity.Name.Length - 2);
					Column col = t.Columns.Where(p => p.Name == dataColumn && p.Type == typeof(string) && !p.IsNull).FirstOrDefault();
					if (col == null)
					{
						Logger.Info("Could not find value column: {0}", t.Name);
						reader.Close();
						continue;
					}

					Mapping map = new Mapping()
					{
						Name = t.Name,
						IdType = t.Identity.Type
					};

					bool duplicate = false;
					while (reader.Read())
					{
						if (!map.Add(new Data() { ID = reader[t.Identity.Name], Name = reader[col.Name].ToString() }))
						{
							Logger.Warn("Table contains duplicate values: {0}", t.Name);
							duplicate = true;
							break;
						}
					}

					if (map.Count == 0)
						Logger.Info("Table was empty: {0}", t.Name);

					if (!duplicate && map.Count > 0)
						Mappings.Add(map);

					reader.Close();
				}
			}
			catch (Exception ex)
			{
				Logger.Error("Parse error: ({0}) {1}", ex.GetType().Name, ex.Message);
				throw new ReturnException(ReturnCode.ParseError);
			}
		}

		public void Write()
		{
			StringBuilder classes = new StringBuilder();
			foreach (Mapping map in Mappings)
			{
				Templates.ClassBase cls = null;
				Type type = Type.GetType("GenTypes.Templates." + _settings.Language + ".Class");
				cls = (Templates.ClassBase)Activator.CreateInstance(type);
				cls.Assign(map);
				classes.Append(cls.TransformText());
			}

			Templates.FileBase file = null;
			Type fileType = Type.GetType("GenTypes.Templates." + _settings.Language + ".File");
			file = (Templates.FileBase)Activator.CreateInstance(fileType);
			file.Assign(classes.ToString(), _settings.MasterNamespace);

			StreamWriter writer = Helpers.OpenWriter(_settings.OutputFile);

			writer.Write(file.TransformText());
			writer.Close();
		}
	}
}
