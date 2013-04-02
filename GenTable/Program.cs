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

namespace GenTable
{
	public class Table
	{
		public static readonly Table None;

		static Table()
		{
			None = new Table("");
		}

		private string _name;
		public string Name
		{
			get { return _name; }
			set { _name = value; NameClean = value.CleanName(); }
		}

		public string NameClean;
		public List<Column> Columns;
		public Column Identity
		{
			get
			{
				return Columns.FirstOrDefault(c => c.IsIdentity);
			}
		}

		public Table(string name)
		{
			Name = name;
			Columns = new List<Column>();
		}
	}

	public class Column
	{
		private string _name;
		public string Name
		{
			get { return _name; }
			set { _name = value; NameClean = value.CleanName(); }
		}

		public string NameClean;
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

	public class Program
	{
		private static int Main(string[] args)
		{
			SqlConnection conn = null;

			try
			{
				ConfigurationSection section = (ConfigurationSection)ConfigurationManager.GetSection("outputs");
				string name = Helpers.GetConfig(args);
				Configuration config = String.IsNullOrWhiteSpace(name) ? new Configuration() : section.Get(name);

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

		public List<Table> Tables;

		public Program(Configuration settings)
		{
			_settings = settings;
			Tables = new List<Table>();
		}

		public void LoadTables(SqlConnection conn)
		{
			SqlCommand cmd = new SqlCommand("p_ListTables", conn) { CommandType = CommandType.StoredProcedure };

			Tables = new List<Table>();
			try
			{
				SqlDataReader reader = cmd.ExecuteReader();
				Table table = Table.None;
				while (reader.Read())
				{
					string tableName = reader["table"].ToString();
					if (tableName != table.Name && tableName + _settings.CollisionPostfix != table.Name)
					{
						if (table != Table.None)
							Tables.Add(table);

						table = new Table(tableName);
					}

					Column col = new Column(reader["column"].ToString(), reader["type"].ToString(), (bool)reader["nullable"], (bool)reader["identity"]);

					if (col.Name == table.Name)
						table.Name = table.Name + _settings.CollisionPostfix;

					table.Columns.Add(col);
				}

				Tables.Add(table); // add the last table from the result set.
			}
			catch (Exception ex)
			{
				Logger.Error("Parse error: ({0}) {1}", ex.GetType().Name, ex.Message);
				throw new ReturnException(ReturnCode.Unknown);
			}

			Logger.Info("Done database stuff");
		}

		public void Write()
		{
			StringBuilder tbls = new StringBuilder();
			foreach (Table t in Tables)
			{
				Templates.Class c = new Templates.Class() { Session = new Dictionary<string, object>() };
				c.Session["table"] = t;
				c.Initialize();
				tbls.Append(c.TransformText());
			}

			Templates.File f = new Templates.File() { Session = new Dictionary<string, object>() };
			f.Session["classes"] = tbls.ToString();
			f.Session["namespace"] = _settings.MasterNamespace;
			f.Initialize();

			StreamWriter writer = Helpers.OpenWriter(_settings.OutputFile);
			writer.Write(f.TransformText());
			writer.Close();
		}
	}
}