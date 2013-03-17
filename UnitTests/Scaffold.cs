using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NUnit.Framework;

namespace UnitTests
{
	[SetUpFixture]
	public class Scaffold
	{
		public static readonly string ConnectionString = @"Data Source=.\SQLEXPRESS;Integrated Security=True;Database=KnownState";

		private static readonly Regex splitter = new Regex("^GO", RegexOptions.IgnoreCase | RegexOptions.Multiline);
		private enum SqlResource { KnownState, Cleanup, DatabaseSetup }

		private static void RunSql(SqlResource res, ref SqlConnection conn)
		{
			string sql;
			string name = String.Format("UnitTests.SQL.{0}.sql", res);
			using (Stream resource = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
			{
				StreamReader reader = new StreamReader(resource);
				sql = reader.ReadToEnd();
			}

			SqlTransaction trans = conn.BeginTransaction();
			SqlCommand cmd = new SqlCommand();
			foreach (string stmt in splitter.Split(sql))
			{
				cmd = new SqlCommand(stmt, conn, trans);
				if(cmd.ExecuteNonQuery() == 0)
				{
					trans.Rollback();
					throw new Exception("Could not run SQL: " + res.ToString());
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
