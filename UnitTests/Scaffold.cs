using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace UnitTests
{
	[SetUpFixture]
	public class Scaffold
	{
		public static string ConnectionString {
			get { return ConfigurationManager.ConnectionStrings["KnownState"].ConnectionString; }
		}

		private static readonly Regex Splitter = new Regex("^go", RegexOptions.IgnoreCase | RegexOptions.Multiline);
		private enum SqlResource { KnownState, Cleanup, DatabaseSetup }

		private static void RunSql(SqlResource res, ref SqlConnection conn)
		{
			string sql;
			string name = String.Format("UnitTests.SQL.{0}.sql", res);
			using (Stream resource = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
			{
				Debug.Assert(resource != null, "Missing resource: " + name);
				StreamReader reader = new StreamReader(resource);
				sql = reader.ReadToEnd();
			}

			SqlTransaction trans = conn.BeginTransaction();
			foreach (string stmt in Splitter.Split(sql).Where(p => !String.IsNullOrWhiteSpace(p)))
			{
				SqlCommand cmd = new SqlCommand(stmt, conn, trans);
				if(cmd.ExecuteNonQuery() == 0)
				{
					trans.Rollback();
					throw new Exception("Could not run SQL: " + res);
				}
			}

			trans.Commit();
		}

		[SetUp]
		public void SetUp()
		{
			SqlConnection conn = new SqlConnection(ConnectionString);

			conn.Open();
			RunSql(SqlResource.Cleanup, ref conn);
			RunSql(SqlResource.KnownState, ref conn);
			RunSql(SqlResource.DatabaseSetup, ref conn);
			conn.Close();
		}
	}
}
