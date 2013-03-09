using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using Mono.Options;

namespace GenTypes
{
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

			SqlCommand cmd = new SqlCommand("p_ListTypeTables", conn) { CommandType = CommandType.StoredProcedure };

			return ReturnValue.Success;
		}
	}
}
