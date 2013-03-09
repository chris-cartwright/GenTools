using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using Common;
using Mono.Options;

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
				return Columns.Where(c => c.IsIdentity).FirstOrDefault();
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
		private static Properties.Settings Settings;

		private static int Main(string[] args)
		{
			Settings = Properties.Settings.Default;

			string[] extra;
			SqlConnection conn;
			int ret = Helpers.Setup(args, ref Settings, out extra, out conn);

			if (ret != ReturnValue.Success)
				return ret;

			if (extra.Length > 0)
				Settings.OutputFile = extra.First();

			SqlCommand cmd = new SqlCommand("p_ListTables", conn);
			cmd.CommandType = CommandType.StoredProcedure;

			List<Table> tables = new List<Table>();
			try
			{
				SqlDataReader reader = cmd.ExecuteReader();
				Table table = Table.None;
				while (reader.Read())
				{
					string tableName = reader["table"].ToString();
					if (tableName != table.Name && tableName + Settings.CollisionPostfix != table.Name)
					{
						if (table != Table.None)
							tables.Add(table);

						table = new Table(tableName);
					}

					Column col = new Column(reader["column"].ToString(), reader["type"].ToString(), (bool)reader["nullable"], (bool)reader["identity"]);

					if (col.Name == table.Name)
						table.Name = table.Name + Settings.CollisionPostfix;

					table.Columns.Add(col);
				}

				tables.Add(table); // add the last table from the result set.
			}
			catch (Exception ex)
			{
				Logger.Error("Parse error: ({0}) {1}", ex.GetType().Name, ex.Message);
				return ReturnValue.Unknown;
			}

			Logger.Info("Done database stuff");

			StringBuilder tbls = new StringBuilder();
			foreach (Table t in tables)
			{
				Templates.Class c = new Templates.Class();
				c.Session = new Dictionary<string, object>();
				c.Session["table"] = t;
				c.Initialize();
				tbls.Append(c.TransformText());
			}

			Templates.File f = new Templates.File();
			f.Session = new Dictionary<string, object>();
			f.Session["classes"] = tbls.ToString();
			f.Session["namespace"] = Settings.MasterNamespace;
			f.Initialize();

			try
			{
				if (File.Exists(Settings.OutputFile))
				{
					FileAttributes attr = File.GetAttributes(Settings.OutputFile);
					if ((attr & FileAttributes.ReadOnly) > 0)
						File.SetAttributes(Settings.OutputFile, attr ^ FileAttributes.ReadOnly);
				}

				StreamWriter writer = new StreamWriter(File.Open(Settings.OutputFile, FileMode.Create));
				writer.Write(f.TransformText());
				writer.Close();
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