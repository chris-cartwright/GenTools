
/*
 * This file would actually be included in the project using the generated classes.
 * It's just here because it goes with the templates.
 */

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace Procedures
{
	public abstract class WrappedProcedure
	{
		public static readonly string ConnectionString = null;

		public class NoResultException : Exception { }

		protected SqlCommand _cmd;

		protected abstract void AssignInputs();
		protected abstract void AssignOutputs();

		/// <summary>
		/// Copies output values to the class' fields.
		/// </summary>
		/// <remarks>
		/// This will close the database connection.
		/// Generally, this should not be called directly. Only do so when required.
		/// </remarks>
		public void Bind()
		{
			if (_cmd.Connection.State == ConnectionState.Open)
				_cmd.Connection.Close();

			AssignOutputs();
		}

		/// <summary>
		/// Calls SqlCommand.ExecuteReader and returns the result as an in-memory object.
		/// </summary>
		/// <returns>Table data returned from the stored procedure</returns>
		public Dictionary<string, object>[] Memory()
		{
			try
			{
				AssignInputs();
				_cmd.Connection.Open();
				List<Dictionary<string, object>> ret = new List<Dictionary<string, object>>();
				SqlDataReader reader = _cmd.ExecuteReader();

				while (reader.Read())
				{
					Dictionary<string, object> row = new Dictionary<string, object>();
					for (int i = 0; i < reader.FieldCount; i++)
						row[reader.GetName(i)] = reader[i];

					ret.Add(row);
				}

				_cmd.Connection.Close();
				AssignOutputs();

				return ret.ToArray();
			}
			finally
			{
				if (_cmd.Connection.State == ConnectionState.Open)
					_cmd.Connection.Close();
			}
		}

		/// <summary>
		/// Used for lazy evaluation. Useful when only some rows are needed out of a result set.
		/// </summary>
		/// <remarks>
		/// The connection will be kept open until the enumerable is evaluated!
		/// 
		/// Output parameters do not get populated until the connection is closed or the
		/// data has been iterated over.
		/// </remarks>
		/// <seealso cref="http://support.microsoft.com/kb/308621" />
		/// <returns>An enumerable dictionary collection</returns>
		public IEnumerable<Dictionary<string, object>> Enumerable()
		{
			SqlDataReader reader;

			try
			{
				AssignInputs();
				_cmd.Connection.Open();
				reader = _cmd.ExecuteReader();
			}
			catch (Exception)
			{
				if (_cmd.Connection.State == ConnectionState.Open)
					_cmd.Connection.Close();

				throw;
			}

			while (reader.Read())
			{
				Dictionary<string, object> row = new Dictionary<string, object>();
				for (int i = 0; i < reader.FieldCount; i++)
					row[reader.GetName(i)] = reader[i];

				yield return row;
			}
		}

		/// <summary>
		/// Returns the first row of a result as an in-memory object.
		/// </summary>
		/// <returns>First row of data</returns>
		public Dictionary<string, object> FirstRow()
		{
			try
			{
				AssignInputs();
				_cmd.Connection.Open();
				SqlDataReader reader = _cmd.ExecuteReader();

				if (!reader.Read())
					throw new NoResultException();

				Dictionary<string, object> row = new Dictionary<string, object>();
				for (int i = 0; i < reader.FieldCount; i++)
					row[reader.GetName(i)] = reader[i];

				_cmd.Connection.Close();
				AssignOutputs();

				return row;
			}
			finally
			{
				if (_cmd.Connection.State == ConnectionState.Open)
					_cmd.Connection.Close();
			}
		}

		/// <summary>
		/// Calls SqlCommand.ExecuteReader and returns the SqlDataReader object.
		/// Preference should be given to <see cref="Memory"/>
		/// </summary>
		/// <remarks>
		/// This does not handle the connection. The user must close it.
		/// 
		/// Output parameters do not get populated until the connection is closed or the
		/// data has been iterated over. <see cref="AssignOutputs"/> must be called manually.
		/// </remarks>
		/// <seealso cref="http://support.microsoft.com/kb/308621" />
		/// <returns>SqlDataReader returned from ExecuteReader</returns>
		public SqlDataReader Reader()
		{
			try
			{
				AssignInputs();
				_cmd.Connection.Open();
				SqlDataReader reader = _cmd.ExecuteReader();
				return reader;
			}
			catch (Exception)
			{
				if (_cmd.Connection.State == ConnectionState.Open)
					_cmd.Connection.Close();

				throw;
			}
		}

		/// <summary>
		/// Calls SqlCommand.ExecuteNonQuery and returns the result.
		/// </summary>
		/// <returns>Result from ExecuteNonQuery</returns>
		public int NonQuery()
		{
			try
			{
				AssignInputs();
				_cmd.Connection.Open();
				int ret = _cmd.ExecuteNonQuery();

				_cmd.Connection.Close();
				AssignOutputs();

				return ret;
			}
			finally
			{
				if (_cmd.Connection.State == ConnectionState.Open)
					_cmd.Connection.Close();
			}
		}

		/// <summary>
		/// Calls SqlCommand.ExecuteScalar and converts the returned object to the requested type.
		/// </summary>
		/// <typeparam name="T">Type to convert scalar to</typeparam>
		/// <returns>Converted type from ExecuteScalar</returns>
		public T Scalar<T>() where T : IConvertible
		{
			try
			{
				AssignInputs();
				_cmd.Connection.Open();
				object ret = _cmd.ExecuteScalar();

				if (ret == null || ret == DBNull.Value)
					throw new NoResultException();

				_cmd.Connection.Close();
				AssignOutputs();

				return (T)Convert.ChangeType(ret, typeof(T));
			}
			finally
			{
				if (_cmd.Connection.State == ConnectionState.Open)
					_cmd.Connection.Close();
			}
		}
	}
}
