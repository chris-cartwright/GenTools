
/*
 * This file would actually be included in the project using the generated classes.
 * It's just here because it goes with the templates.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Procedures
{
	public static class WrappedProcedure
	{
		private SqlCommand _cmd;

		internal WrappedProcedure(SqlCommand cmd)
		{
			_cmd = cmd;
		}

		public Dictionary<string, object>[] Memory()
		{
			try
			{
				_cmd.Connection.Open();
				List<Dictionary<string, object>> ret = new List<Dictionary<string, object>>();
				SqlDataReader reader = _cmd.ExecuteReader();
				while (reader.Next())
				{
					Dictionary<string, object> row = new Dictionary<string, object>();
					for (int i = 0; i < reader.FieldCount(); i++)
						row[reader.GetName(i)] = reader[i];

					ret.Add(row);
				}

				return ret.ToArray();
			}
			finally
			{
				if (_cmd.Connection.State == ConnectionState.Open)
					_cmd.Close();
			}
		}

		public SqlDataReader Reader()
		{
			try
			{
				_cmd.Connection.Open();
				return _cmd.ExecuteReader();
			}
			catch (Exception ex)
			{
				if (_cmd.Connection.State == ConnectionState.Open)
					_cmd.Close();

				throw ex;
			}

			return null;
		}

		public int NonQuery()
		{
			try
			{
				_cmd.Connection.Open();
				return _cmd.ExecuteNonQuery();
			}
			finally
			{
				if (_cmd.Connection.State == ConnectionState.Open)
					_cmd.Close();
			}

			return -1;
		}

		public T Scalar<T>() where T : IConvertible
		{
			try
			{
				_cmd.Connection.Open();
				return Convert.ChangeType(_cmd.ExecuteScalar(), typeof(T));
			}
			finally
			{
				if (_cmd.Connection.State == ConnectionState.Open)
					_cmd.Close();
			}
		}
	}
}
