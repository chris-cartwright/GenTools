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

			Console.WriteLine("GenTable version {0}", Helpers.Version);

			bool help = false;
			OptionSet opts = new OptionSet()
			{
				{ "v", "Increase verbosity level.", v => Settings.LoggingLevel++ },
				{ "verbosity:", "Verbosity level. 0-4 are supported.", (ushort v) => Settings.LoggingLevel = v },
				{ "n|namespace:", "Namespace generated code should exist in.", (string v) => Settings.MasterNamespace = v },
				{ "c|connection:", "Name of connection string to use.", (string v) => Settings.ConnectionString = v },
				{ "h|help", "Show this message.", v => help = v != null }
			};

			List<string> extra = null;
			try
			{
				extra = opts.Parse(args);
			}
			catch (OptionException ex)
			{
				Console.WriteLine("GenTable: {0}", ex.Message);
				Console.WriteLine("Try `GenTable --help` for more information.");
				return Return.InvalidOptions;
			}

			if (help)
			{
				PrintHelp(opts);
				return Return.Success;
			}

			Logger.Current = (Logger.Level)Settings.LoggingLevel;
			Logger.Debug("Logging level: {0}", Logger.Current);

			if (extra.Count > 0)
				Settings.OutputFile = extra.First();

			Settings.ConnectionString = "GenTable.Properties.Settings." + Settings.ConnectionString;
			if (ConfigurationManager.ConnectionStrings[Settings.ConnectionString] == null)
			{
				Logger.Error("Unknown connection: {0}", Settings.ConnectionString);
				return Return.UnknownConnection;
			}

			Logger.Info("Using connection: {0}", ConfigurationManager.ConnectionStrings[Settings.ConnectionString]);
			SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings[Settings.ConnectionString].ToString());

			try
			{
				conn.Open();
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine("Could not connect: {0}", ex.Message);
				return Return.ConnectFailed;
			}

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
				return Return.Unknown;
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
				return Return.FileAccess;
			}
			catch (Exception ex)
			{
				Logger.Error("Unknown error: ({0}) {1}", ex.GetType().Name, ex.Message);
				return Return.FileAccess;
			}

			return Return.Success;
		}

		private static void PrintHelp(OptionSet opts)
		{
			Console.WriteLine("Usage: GenTable [options] [output file/folder]");
			Console.WriteLine("Generate C# code for tables in a SQL Server database.");
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
	}
}