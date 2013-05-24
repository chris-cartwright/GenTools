using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using GenTable;
using NUnit.Framework;

namespace UnitTests
{
	[TestFixture]
	public class GenTableTests
	{
		private readonly Dictionary<string, string> _files = new Dictionary<string, string>()
		{
			{ "GenTable", Path.Combine(Environment.CurrentDirectory, "GenTable.cs") }
		};

		private class Columns
		{
			public PropertyInfo Id;
			public PropertyInfo Value;
		}

		[TestFixture]
		public class TableTests
		{
			private Table _table;

			[SetUp]
			public void SetUp()
			{
				_table = new Table("void");
			}

			[Test]
			public void Constructor()
			{
				Assert.AreEqual("void", _table.Name);
				Assert.AreEqual("@void", _table.NameClean);
				Assert.NotNull(_table.Columns);
				Assert.AreEqual(0, _table.Columns.Count);
			}

			[Test]
			public void Identity()
			{
				_table.Columns.Add(new Column("One", typeof(int)));
				_table.Columns.Add(new Column("Two", "tinyint", false, true));
				Assert.AreEqual("Two", _table.Identity.Name);

				SetUp();
				_table.Columns.Add(new Column("First", "nvarchar", true, true));
				Assert.AreEqual("First", _table.Identity.Name);

				SetUp();
				_table.Columns.Add(new Column("First", "byte", true, true));
				_table.Columns.Add(new Column("Second", "datetime", true, true));
				Assert.AreEqual("First", _table.Identity.Name);
			}
		}

		[TestFixture]
		public class ColumnTests
		{
			private Column _column;

			[SetUp]
			public void SetUp()
			{
				_column = new Column("First", typeof(int));
			}

			[Test]
			public void Constructor()
			{
				Assert.AreEqual("First", _column.Name);
				Assert.AreEqual("First", _column.NameClean);
				Assert.IsFalse(_column.IsNull);
				Assert.IsFalse(_column.IsIdentity);

				_column = new Column("void", "nvarchar", true, true);
				Assert.AreEqual("void", _column.Name);
				Assert.AreEqual("@void", _column.NameClean);
				Assert.IsTrue(_column.IsNull);
				Assert.IsTrue(_column.IsIdentity);
			}
		}

		private Program _genTable;
		private Assembly _assembly;

		private Type _type;
		private Columns _columns;

		[SetUp]
		public void SetUp()
		{
			SqlConnection conn = new SqlConnection(Scaffold.ConnectionString);
			_genTable = new Program(new Configuration() { OutputFile = _files["GenTable"], MasterNamespace = "Tables" });

			conn.Open();
			_genTable.LoadTables(conn);
			conn.Close();

			foreach (string file in _files.Values.Where(File.Exists))
			{
				File.SetAttributes(file, FileAttributes.Normal);
				File.Delete(file);
			}
		}

		[Test]
		public void CompileExecute()
		{
			LoadTables();
			WriteOutput();
			_assembly = Utilities.Compile(_files["GenTable"], Utilities.Include.GenTable);
			Execute();
			LoadFull();
			Populate();
			SaveFull();
		}

		private void LoadTables()
		{
			/* Check loaded tables */
			Assert.AreEqual(11, _genTable.Tables.Count);

			Table table = null;
			int i = 0;

			Action<Column, Column> areEqual = delegate(Column lhs, Column rhs)
			{
				Assert.AreEqual(lhs.Name, rhs.Name, lhs.Name);
				Assert.AreEqual(lhs.Type, rhs.Type, lhs.Name);
				Assert.AreEqual(lhs.IsNull, rhs.IsNull, lhs.Name);
				Assert.AreEqual(lhs.IsIdentity, rhs.IsIdentity, lhs.Name);
			};

			// ReSharper disable ImplicitlyCapturedClosure
			Action<string, Column[]> checkTable = delegate(string tableName, Column[] columns)
			{
				table = _genTable.Tables[i];

				Assert.AreEqual(tableName, table.Name);
				Assert.AreEqual(columns.Length, table.Columns.Count);
				for (int j = 0; j < columns.Length; j++)
					areEqual(columns[j], table.Columns[j]);

				i++;
			};

			Action<string, Column[], string> checkTableIdent = delegate(string tableName, Column[] columns, string identityName)
			{
				checkTable(tableName, columns);
				Assert.AreEqual(identityName, table.Identity.Name);
			};
			// ReSharper restore ImplicitlyCapturedClosure

			checkTableIdent(
				"BadColumnType",
				new[] {
					new Column("BadColumnID", typeof(int)) { IsIdentity = true },
					new Column("DoesntExist", typeof(string))
				},
				"BadColumnID"
			);

			checkTableIdent(
				"DuplicateType",
				new[] {
					new Column("DuplicateID", typeof(byte)) { IsIdentity = true },
					new Column("Duplicate", typeof(string))
				},
				"DuplicateID"
			);

			checkTableIdent(
				"EmptyType",
				new[] {
					new Column("EmptyID", typeof(int)) { IsIdentity = true },
					new Column("Empty", typeof(string))
				},
				"EmptyID"
			);

			checkTableIdent(
				"IllegalValueType",
				new[] {
					new Column("IllegalID", typeof(byte)) { IsIdentity = true },
					new Column("Illegal", typeof(string))
				},
				"IllegalID"
			);

			checkTable(
				"NoIdentityType",
				new[] {
					new Column("NoIdentityID", typeof(int)),
					new Column("NoIdentity", typeof(string))
				}
			);
			Assert.IsNull(table.Identity);

			checkTableIdent(
				"NullableTypes",
				new[] {
					new Column("NullableID", typeof(short)) { IsIdentity = true },
					new Column("Nullable", typeof(string)) { IsNull = true }
				},
				"NullableID"
			);

			checkTableIdent(
				// Members cannot have the same name as their enclosing type
				"ShouldntSee_",
				new[] {
					new Column("ShouldntSeeID", typeof(long)) { IsIdentity = true },
					new Column("ShouldntSee", typeof(string))
				},
				"ShouldntSeeID"
			);

			checkTableIdent(
				"SingleColumnType",
				new[] {
					new Column("SingleColumnID", typeof(byte)) { IsIdentity = true }
				},
				"SingleColumnID"
			);

			checkTableIdent(
				"StandardTypes",
				new[] {
					new Column("StandardID", typeof(short)) { IsIdentity = true },
					new Column("Standard", typeof(string))
				},
				"StandardID"
			);

			checkTableIdent(
				"TinyType",
				new[] {
					new Column("TinyID", typeof(byte)) { IsIdentity = true },
					new Column("Tiny", typeof(string))
				},
				"TinyID"
			);

			checkTableIdent(
				"WrongType_",
				new[] {
					new Column("WrongTypeID", typeof(short)) { IsIdentity = true },
					new Column("WrongType", typeof(int))
				},
				"WrongTypeID"
			);
		}

		private void WriteOutput()
		{
			if (File.Exists(_files["GenTable"]))
			{
				File.Delete(_files["GenTable"]);
			}

			_genTable.Write();
			Assert.IsTrue(File.Exists(_files["GenTable"]));
		}

		private void Execute()
		{
			Type[] types = _assembly.GetExportedTypes();
			Assert.AreEqual(12, types.Count(t => t.Namespace == "Tables"));

			// ReSharper disable JoinDeclarationAndInitializer
			Type type;
			PropInfo[] expected;
			// ReSharper restore JoinDeclarationAndInitializer

			Action<PropInfo, PropertyInfo> validate = delegate(PropInfo lhs, PropertyInfo rhs)
			{
				Assert.IsTrue(rhs.CanRead);
				Assert.IsTrue(rhs.CanWrite);
				PropInfo.AreEqual(lhs, rhs);
			};

			// First table was generated
			#region BadColumnType
			type = types.FirstOrDefault(t => t.FullName == "Tables.BadColumnType`1");
			Assert.IsNotNull(type);

			expected = new[]
			{
				new PropInfo("BadColumnID", typeof(int)),
				new PropInfo("DoesntExist", typeof(string))
			};
			expected.Apply(type.GetProperties(), validate);
			#endregion

			// Random table
			#region DuplicateType
			type = types.FirstOrDefault(t => t.FullName == "Tables.DuplicateType`1");
			Assert.IsNotNull(type);

			expected = new[]
			{
				new PropInfo("DuplicateID", typeof(byte)),
				new PropInfo("Duplicate", typeof(string))
			};
			expected.Apply(type.GetProperties(), validate);
			#endregion

			// Prevents collision
			#region ShoudlntSee
			type = types.FirstOrDefault(t => t.FullName == "Tables.ShouldntSee_`1");
			Assert.IsNotNull(type);

			expected = new[]
			{
				new PropInfo("ShouldntSeeID", typeof(long)),
				new PropInfo("ShouldntSee", typeof(string))
			};
			expected.Apply(type.GetProperties(), validate);
			#endregion

			// Grabs tables without identity columns
			#region NoIdentityType
			type = types.FirstOrDefault(t => t.FullName == "Tables.NoIdentityType`1");
			Assert.IsNotNull(type);

			expected = new[]
			{
				new PropInfo("NoIdentityID", typeof(int)),
				new PropInfo("NoIdentity", typeof(string))
			};
			expected.Apply(type.GetProperties(), validate);
			#endregion

			// Generates tables with nullable columns
			#region NullableTypes
			type = types.FirstOrDefault(t => t.FullName == "Tables.NullableTypes`1");
			Assert.IsNotNull(type);

			expected = new[]
			{
				new PropInfo("NullableID", typeof(short)),
				new PropInfo("Nullable", typeof(string))
			};
			expected.Apply(type.GetProperties(), validate);
			#endregion

			// Last table was generated
			#region WrongType
			// Test to make sure last table was generated
			type = types.FirstOrDefault(t => t.FullName == "Tables.WrongType_`1");
			Assert.IsNotNull(type);

			expected = new[]
			{
				new PropInfo("WrongTypeID", typeof(short)),
				new PropInfo("WrongType", typeof(int))
			};
			expected.Apply(type.GetProperties(), validate);
			#endregion
		}

		private void LoadFull()
		{
			LoadGeneric("BadColumn", "BadColumnID", "DoesntExist");
			MethodInfo method = LoadMethod("Load");

			Action<int, string> load = delegate(int lId, string eValue)
			{
				object row = method.InvokeStatic(lId);
				Assert.AreEqual(lId, _columns.Id.GetValue(row, null));
				Assert.AreEqual(eValue, _columns.Value.GetValue(row, null));
			};

			load(1, "Test");
			load(2, "Test 2");
			load(3, "Test 3");

			Assert.IsNull(method.InvokeStatic(4));
		}

		private void Populate()
		{
			LoadGeneric("BadColumn", "BadColumnID", "DoesntExist");
			MethodInfo method = LoadMethod("Populate");

			Action<Dictionary<string, object>, int, string> populate = delegate(Dictionary<string, object> data, int eId, string eValue)
			{
				object row = method.InvokeStatic(data);
				Assert.AreEqual(row.GetType(), _type);
				Assert.AreEqual(eId, _columns.Id.GetValue(row, null));
				Assert.AreEqual(eValue, _columns.Value.GetValue(row, null));
			};

			populate(new Dictionary<string, object>() {
				{ "BadColumnID", 4 },
				{ "DoesntExist", "Blarg" }
			}, 4, "Blarg");

			populate(new Dictionary<string, object>(), default(int), default(string));

			populate(new Dictionary<string, object>() {
				{ "BadColumnID", 4 }
			}, 4, default(string));

			// Can't use InvokeStatic. Passing a single null 'disables' the params behaviour
			Assert.Throws(Has.InnerException.TypeOf<ArgumentNullException>(), () => method.Invoke(null, new object[] { null }));
		}

		private void SaveFull()
		{
			LoadGeneric("BadColumn", "BadColumnID", "DoesntExist");
			MethodInfo save = LoadMethod("Save");
			MethodInfo load = LoadMethod("Load");

			Action<int, string> saveLoad = delegate(int id, string value)
			{
				object row = Activator.CreateInstance(_type);
				_columns.Id.SetValue(row, id, null);
				_columns.Value.SetValue(row, value, null);
				Assert.IsTrue((bool)save.InvokeStatic(row));

				row = load.InvokeStatic(id);
				Assert.IsNotNull(row);
				Assert.AreEqual(value, _columns.Value.GetValue(row, null));
			};

			saveLoad(1, "Blarg");
			saveLoad(2, "Blarg 2");
			saveLoad(3, "");

			object fail = Activator.CreateInstance(_type);
			_columns.Id.SetValue(fail, 4, null);
			_columns.Value.SetValue(fail, "This should fail", null);
			Assert.IsFalse((bool)save.InvokeStatic(fail));
		}

		private MethodInfo LoadMethod(string name)
		{
			MethodInfo method = _type.GetMethod(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy);
			Assert.IsNotNull(method);
			return method;
		}

		private void LoadGeneric(string name, string idName, string valueName)
		{
			_type = _assembly.GetExportedTypes().FirstOrDefault(t => t.Name == name);
			Assert.IsNotNull(_type);

			FieldInfo field = _type.GetField("ConnectionString", BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Static);
			Assert.IsNotNull(field);
			field.SetValue(null, Scaffold.ConnectionString);

			PropertyInfo id = _type.GetProperty(idName);
			Assert.IsNotNull(id);

			PropertyInfo value = _type.GetProperty(valueName);
			Assert.IsNotNull(value);

			_columns = new Columns() { Id = id, Value = value };
		}
	}
}
