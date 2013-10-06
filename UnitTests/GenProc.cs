using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using GenProc;
using NUnit.Framework;

namespace UnitTests
{
	[TestFixture]
	public class GenProc
	{
		private readonly Dictionary<string, string> _files = new Dictionary<string, string>()
		{
			{ "GenProc", Path.Combine(Environment.CurrentDirectory, "GenProc.cs") }
		};

		[TestFixture]
		public class Branch
		{
			private Branch<int> _tree;

			[SetUp]
			public void SetUp()
			{
				_tree = new Branch<int>("Trunk");
			}

			[Test]
			public void Constructor()
			{
				Assert.AreEqual(_tree.Name, "Trunk");
				Assert.IsNotNull(_tree.Branches);
				Assert.IsNotNull(_tree.Leaves);
				Assert.IsEmpty(_tree.Branches);
				Assert.IsEmpty(_tree.Leaves);
			}

			[Test]
			public void Insert()
			{
				int i = 0;

				_tree.Insert(new string[] { }, i);
				Assert.IsEmpty(_tree.Branches);
				Assert.AreEqual(1, _tree.Leaves.Count);
				Assert.AreEqual(i, _tree.Leaves[0]);

				i++;

				_tree.Insert(new[] { "Branch 1" }, i);
				Assert.AreEqual(1, _tree.Branches.Count);
				Assert.IsEmpty(_tree.Branches[0].Branches);
				Assert.AreEqual("Branch 1", _tree.Branches[0].Name);
				Assert.AreEqual(1, _tree.Branches[0].Leaves.Count);
				Assert.AreEqual(i, _tree.Branches[0].Leaves[0]);

				i++;

				_tree.Insert(new[] { "Branch 1", "Sub 1" }, i);
				Assert.AreEqual(1, _tree.Branches.Count);
				Assert.AreEqual(1, _tree.Branches[0].Branches.Count);
				Assert.AreEqual("Branch 1", _tree.Branches[0].Name);
				Assert.AreEqual("Sub 1", _tree.Branches[0].Branches[0].Name);
				Assert.AreEqual(1, _tree.Branches[0].Branches[0].Leaves.Count);
				Assert.AreEqual(i, _tree.Branches[0].Branches[0].Leaves[0]);

				i++;

				_tree.Insert(new[] { "Branch 2", "Sub 1" }, i);
				Assert.AreEqual(2, _tree.Branches.Count);
				Assert.AreEqual(1, _tree.Branches[1].Branches.Count);
				Assert.AreEqual("Sub 1", _tree.Branches[1].Branches[0].Name);
				Assert.AreEqual("Branch 2", _tree.Branches[1].Name);
				Assert.AreEqual(0, _tree.Branches[1].Leaves.Count);
				Assert.AreEqual(1, _tree.Branches[1].Branches[0].Leaves.Count);
				Assert.AreEqual(i, _tree.Branches[1].Branches[0].Leaves[0]);

				i++;

				_tree.Insert(new[] { "Branch 2", "Sub 2" }, i);
				Assert.AreEqual(2, _tree.Branches.Count);
				Assert.AreEqual(2, _tree.Branches[1].Branches.Count);
				Assert.AreEqual("Sub 2", _tree.Branches[1].Branches[1].Name);
				Assert.AreEqual("Branch 2", _tree.Branches[1].Name);
				Assert.AreEqual(1, _tree.Branches[1].Branches[1].Leaves.Count);
				Assert.AreEqual(i, _tree.Branches[1].Branches[1].Leaves[0]);

				i++;

				_tree.Insert(new[] { "Branch 3", "Sub 1", "Sub sub 1" }, i);
				Assert.AreEqual(3, _tree.Branches.Count);
				Assert.AreEqual(1, _tree.Branches[2].Branches.Count);
				Assert.AreEqual(1, _tree.Branches[2].Branches[0].Branches.Count);
				Assert.AreEqual("Branch 3", _tree.Branches[2].Name);
				Assert.AreEqual("Sub 1", _tree.Branches[2].Branches[0].Name);
				Assert.AreEqual("Sub sub 1", _tree.Branches[2].Branches[0].Branches[0].Name);
				Assert.IsEmpty(_tree.Branches[2].Leaves);
				Assert.IsEmpty(_tree.Branches[2].Branches[0].Leaves);
				Assert.AreEqual(1, _tree.Branches[2].Branches[0].Branches[0].Leaves.Count);
				Assert.AreEqual(i, _tree.Branches[2].Branches[0].Branches[0].Leaves[0]);

				i++;

				_tree.Insert(new[] { "Branch 3", "Sub 1", "Sub sub 1" }, i);
				Assert.AreEqual(3, _tree.Branches.Count);
				Assert.AreEqual(1, _tree.Branches[2].Branches.Count);
				Assert.AreEqual(1, _tree.Branches[2].Branches[0].Branches.Count);
				Assert.AreEqual("Branch 3", _tree.Branches[2].Name);
				Assert.AreEqual("Sub 1", _tree.Branches[2].Branches[0].Name);
				Assert.AreEqual("Sub sub 1", _tree.Branches[2].Branches[0].Branches[0].Name);
				Assert.IsEmpty(_tree.Branches[2].Leaves);
				Assert.IsEmpty(_tree.Branches[2].Branches[0].Leaves);
				Assert.AreEqual(2, _tree.Branches[2].Branches[0].Branches[0].Leaves.Count);
				Assert.AreEqual(i, _tree.Branches[2].Branches[0].Branches[0].Leaves[1]);
			}

			[Test]
			[ExpectedException(typeof(ArgumentException), ExpectedMessage = "Cannot have duplicate leaves.")]
			public void InsertDuplicate()
			{
				_tree.Insert(new[] { "Branch 1", "Sub 1" }, 0);
				Assert.AreEqual(1, _tree.Branches.Count);
				Assert.AreEqual(1, _tree.Branches[0].Branches.Count);
				Assert.AreEqual("Branch 1", _tree.Branches[0].Name);
				Assert.AreEqual("Sub 1", _tree.Branches[0].Branches[0].Name);
				Assert.AreEqual(1, _tree.Branches[0].Branches[0].Leaves.Count);
				Assert.AreEqual(0, _tree.Branches[0].Branches[0].Leaves[0]);

				_tree.Insert(new[] { "Branch 1", "Sub 1" }, 0);
				Assert.AreEqual(1, _tree.Branches.Count);
				Assert.AreEqual(1, _tree.Branches[0].Branches.Count);
				Assert.AreEqual("Branch 1", _tree.Branches[0].Name);
				Assert.AreEqual("Sub 1", _tree.Branches[0].Branches[0].Name);
				Assert.AreEqual(1, _tree.Branches[0].Branches[0].Leaves.Count);
				Assert.AreEqual(0, _tree.Branches[0].Branches[0].Leaves[0]);
			}
		}

		[TestFixture]
		public class CustomTypeTests
		{
			[Test]
			public void Constructor()
			{
				// ReSharper disable once JoinDeclarationAndInitializer
				CustomType type;

				type = new CustomType("TableType");
				Assert.AreEqual("TableType", type.Name);
				Assert.IsFalse(type.IsValueType);
				Assert.IsTrue(type.IsArray);
				Assert.IsTrue(type.IsTable);

				type = new CustomType(typeof(string));
				Assert.AreEqual("String", type.Name);
				Assert.IsFalse(type.IsValueType);
				Assert.IsFalse(type.IsArray);
				Assert.IsFalse(type.IsTable);
			}

			public static IEnumerable<TestCaseData> EqualSource()
			{
				yield return new TestCaseData(typeof(string));
				yield return new TestCaseData(typeof(int));
				yield return new TestCaseData(typeof(DateTime));
				yield return new TestCaseData(typeof(long?));
			}

			[Test]
			[TestCaseSource("EqualSource")]
			public void Equal(Type type)
			{
				Assert.IsTrue(new CustomType(type) == type);
				Assert.IsFalse(new CustomType(type) != type);
			}

			public static IEnumerable<TestCaseData> NotEqualSource()
			{
				yield return new TestCaseData(typeof(int), typeof(string));
				yield return new TestCaseData(typeof(string), typeof(int));
				yield return new TestCaseData(typeof(int), typeof(DateTime));
				yield return new TestCaseData(typeof(long?), typeof(int?));
			}

			[Test]
			[TestCaseSource("NotEqualSource")]
			public void NotEqual(Type lhs, Type rhs)
			{
				Assert.IsFalse(new CustomType(lhs) == rhs);
				Assert.IsTrue(new CustomType(lhs) != rhs);
			}
		}

		private Program _genProc;
		private Assembly _assembly;

		[SetUp]
		public void SetUp()
		{
			SqlConnection conn = new SqlConnection(Scaffold.ConnectionString);
			_genProc = new Program(new Configuration() { OutputFile = _files["GenProc"], Monolithic = true });

			conn.Open();
			_genProc.LoadTableTypes(conn);
			_genProc.LoadProcedures(conn);
			conn.Close();

			foreach (string file in _files.Values.Where(File.Exists))
			{
				File.SetAttributes(file, FileAttributes.Normal);
				File.Delete(file);
			}
		}

		[Test]
		public void Parse()
		{
			_genProc = new Program(new Configuration());

			Procedure p = _genProc.Parse("p_Test_Name");
			Assert.IsEmpty(p.Parameters);
			Assert.AreEqual("p_Test_Name", p.Original);
			Assert.AreEqual("Name", p.Name);
			Assert.AreEqual(new[] { "Test" }, p.Path);

			p = _genProc.Parse("Test_Name");
			Assert.IsEmpty(p.Parameters);
			Assert.AreEqual("Test_Name", p.Original);
			Assert.AreEqual("Name", p.Name);
			Assert.AreEqual(new[] { "Test" }, p.Path);

			p = _genProc.Parse("TestName");
			Assert.IsEmpty(p.Parameters);
			Assert.AreEqual("TestName", p.Original);
			Assert.AreEqual("TestName", p.Name);
			Assert.AreEqual(new[] { "Misc" }, p.Path);

			p = _genProc.Parse("Test_Second");
			Assert.IsEmpty(p.Parameters);
			Assert.AreEqual("Test_Second", p.Original);
			Assert.AreEqual("Second", p.Name);
			Assert.AreEqual(new[] { "Test" }, p.Path);

			p = _genProc.Parse("Second_new");
			Assert.IsEmpty(p.Parameters);
			Assert.AreEqual("Second_new", p.Original);
			Assert.AreEqual("new", p.Name);
			Assert.AreEqual(new[] { "Second" }, p.Path);

			p = _genProc.Parse("Third_while");
			Assert.IsEmpty(p.Parameters);
			Assert.AreEqual("Third_while", p.Original);
			Assert.AreEqual("while", p.Name);
			Assert.AreEqual(new[] { "Third" }, p.Path);

			p = _genProc.Parse("p_Three_Deep_Procedure");
			Assert.IsEmpty(p.Parameters);
			Assert.AreEqual("p_Three_Deep_Procedure", p.Original);
			Assert.AreEqual("Procedure", p.Name);
			Assert.AreEqual(new[] { "Three", "Deep" }, p.Path);

			p = _genProc.Parse("s_Three");
			Assert.IsEmpty(p.Parameters);
			Assert.AreEqual("s_Three", p.Original);
			Assert.AreEqual("Three", p.Name);
			Assert.AreEqual(new[] { "Misc" }, p.Path);
			Console.Write(DateTime.Now.Millisecond);
		}

		[Test]
		public void NotFound()
		{
			Assert.AreEqual(new CustomType(typeof(object)), _genProc.ResolveType("doesn't exist"));
			Assert.AreEqual(new CustomType(typeof(object)), _genProc.ResolveType("nvarchar(40)"));
		}

		public static IEnumerable<TestCaseData> MappingSource()
		{
			yield return new TestCaseData("nvarchar", typeof(string), SqlDbType.NVarChar);
			yield return new TestCaseData("int", typeof(int), SqlDbType.Int);
			yield return new TestCaseData("flag", typeof(bool), SqlDbType.Bit);
			yield return new TestCaseData("date", typeof(DateTime), SqlDbType.Date);
		}

		[Test]
		[TestCaseSource("MappingSource")]
		public void Mapping(string stringType, Type type, SqlDbType sqlType)
		{
			Assert.AreEqual(new CustomType(type), _genProc.ResolveType(stringType));
			Assert.AreEqual(sqlType, _genProc.ResolveSqlType(stringType));
		}

		public static IEnumerable<TestCaseData> CaseSource()
		{
			yield return new TestCaseData("date", typeof(DateTime));
			yield return new TestCaseData("DAte", typeof(DateTime));
			yield return new TestCaseData("Date", typeof(DateTime));
			yield return new TestCaseData("DatE", typeof(DateTime));
			yield return new TestCaseData("DATE", typeof(DateTime));
		}

		[Test]
		[TestCaseSource("CaseSource")]
		public void Case(string stringType, Type type)
		{
			Assert.AreEqual(new CustomType(type), _genProc.ResolveType(stringType));
		}

		[Test]
		public void CompileExecute()
		{
			LoadProcedures();
			WriteOutput();
			_assembly = Utilities.Compile(_files["GenProc"], Utilities.Include.GenProc);
			Execute();
		}

		private void LoadProcedures()
		{
			/* Check loaded tree */
			Assert.AreEqual(0, _genProc.Procedures.Leaves.Count);
			Assert.AreEqual(4, _genProc.Procedures.Branches.Count);

			Branch<Procedure> brch = _genProc.Procedures.Branches[0];
			Assert.AreEqual("Completely", brch.Name);
			Assert.AreEqual(1, brch.Leaves.Count);
			Assert.AreEqual("Valid", brch.Leaves[0].Name);

			brch = _genProc.Procedures.Branches[1];
			Assert.AreEqual("Misc", brch.Name);
			Assert.AreEqual(6, brch.Leaves.Count);
			Assert.AreEqual("ListProcedures", brch.Leaves[0].Name);
			Assert.AreEqual("ListTables", brch.Leaves[1].Name);
			Assert.AreEqual("ListTypeTables", brch.Leaves[2].Name);
			Assert.AreEqual("ListUserTableTypes", brch.Leaves[3].Name);
			Assert.AreEqual("MissingUnderscore", brch.Leaves[4].Name);
			Assert.AreEqual("TableParameter", brch.Leaves[5].Name);

			brch = _genProc.Procedures.Branches[2];
			Assert.AreEqual("No", brch.Name);
			Assert.AreEqual(1, brch.Leaves.Count);
			Assert.AreEqual("Params", brch.Leaves[0].Name);

			brch = _genProc.Procedures.Branches[3];
			Assert.AreEqual("Output", brch.Name);
			Assert.AreEqual(2, brch.Leaves.Count);
			Assert.AreEqual("NonNull", brch.Leaves[0].Name);
			Assert.AreEqual("Test", brch.Leaves[1].Name);

			/* Check procedure definitions */
			Action<Parameter, string, Type, int, string, bool, bool> verifyDefault =
				delegate(Parameter parameter, string name, Type type, int size, string def, bool isNull, bool isOutput)
				{
					Assert.AreEqual(name, parameter.Name);
					Assert.AreEqual(new CustomType(type), parameter.Type);
					Assert.AreEqual(def, parameter.Default);
					Assert.AreEqual(isNull, parameter.IsNull);
					Assert.AreEqual(isOutput, parameter.IsOutput);
					Assert.AreEqual(size, parameter.Size);
				};

			Action<Parameter, string, Type, int, bool, bool> verify =
				delegate(Parameter parameter, string name, Type type, int size, bool isNull, bool isOutput)
				{
					Assert.AreEqual(name, parameter.Name);
					Assert.AreEqual(new CustomType(type), parameter.Type);
					// Only IsNull - an empty string is legitimate
					Assert.IsNull(parameter.Default);
					Assert.AreEqual(isNull, parameter.IsNull);
					Assert.AreEqual(isOutput, parameter.IsOutput);
					Assert.AreEqual(size, parameter.Size);
				};

			Procedure proc = _genProc.Procedures.Branches[0].Leaves[0];
			Assert.AreEqual(7, proc.Parameters.Count);
			verify(proc.Parameters[0], "@Column", typeof(int), 4, false, false);
			verifyDefault(proc.Parameters[1], "@Second", typeof(byte), 1, "0", false, false);
			// nvarchar are double-wide; 20 instead of 10
			verify(proc.Parameters[2], "@Third", typeof(string), 20, false, false);
			verify(proc.Parameters[3], "@Nullable", typeof(int), 4, true, false);
			verifyDefault(proc.Parameters[4], "@Default", typeof(string), 100, "test default", false, false);
			verify(proc.Parameters[5], "@Output", typeof(int), 4, true, true);
			verifyDefault(proc.Parameters[6], "@DefString", typeof(string), 10, "", false, false);

			proc = _genProc.Procedures.Branches[1].Leaves[4];
			Assert.AreEqual(1, proc.Parameters.Count);
			verify(proc.Parameters[0], "@Column", typeof(int), 4, false, false);

			proc = _genProc.Procedures.Branches[2].Leaves[0];
			Assert.AreEqual(0, proc.Parameters.Count);
		}

		private void WriteOutput()
		{
			if (File.Exists(_files["GenProc"]))
			{
				File.Delete(_files["GenProc"]);
			}

			_genProc.Write();
			Assert.IsTrue(File.Exists(_files["GenProc"]));
		}

		private void Execute()
		{
			Type[] types = _assembly.GetExportedTypes();
			Assert.AreEqual(17, types.Length);

			// ReSharper disable JoinDeclarationAndInitializer
			Type type;
			ParamInfo[] expected;
			object inst;
			// ReSharper restore JoinDeclarationAndInitializer

			#region p_Completely_Valid

			type = types.FirstOrDefault(t => t.FullName == "Procedures.Completely+Valid");
			Assert.IsNotNull(type);

			expected = new[]
			{
				new ParamInfo("Column", typeof(int)),
				new ParamInfo("Third", typeof(string)),
				new ParamInfo("Second", typeof(byte)) { DefaultValue = 0 },
				new ParamInfo("Nullable", typeof(int?)) { DefaultValue = null},
				new ParamInfo("Default", typeof(string)) { DefaultValue = "test default" },
				//new ParamInfo("Output", typeof(int?)) { IsOut = true },
				new ParamInfo("DefString", typeof(string)) { DefaultValue = "" }
			};
			expected.Apply(type.GetConstructors()[0].GetParameters().OrderBy(p => p.Position), ParamInfo.AreEqual);

			#region Expose System.InvalidOperationException : the Size property has an invalid size of 0.

			Utilities.SetConnectionString(ref type);
			inst = type.GetConstructors()[0].Invoke(new object[] { 1, "test", (byte)1, 1, "blarg", "value" });
			type.GetField("Second").SetValue(inst, (byte)2);
			type.GetField("Nullable").SetValue(inst, null);
			type.GetField("DefString").SetValue(inst, "blarg");
			type.GetMethod("NonQuery").Invoke(inst, new object[] { });

			#endregion

			#endregion

			#region p_MissingUnderscore

			type = types.FirstOrDefault(t => t.FullName == "Procedures.Misc+MissingUnderscore");
			Assert.IsNotNull(type);

			expected = new[]
			{
				new ParamInfo("Column", typeof(int))
			};
			expected.Apply(type.GetConstructors()[0].GetParameters().OrderBy(p => p.Position), ParamInfo.AreEqual);

			#endregion

			#region p_No_Params

			type = types.FirstOrDefault(t => t.FullName == "Procedures.No+Params");
			Assert.IsNotNull(type);
			Assert.IsEmpty(type.GetConstructors()[0].GetParameters());

			inst = type.GetConstructors()[0].Invoke(new object[] { });
			type.GetMethod("NonQuery").Invoke(inst, new object[] { });

			#endregion

			#region p_Output_NonNull

			type = types.FirstOrDefault(t => t.FullName == "Procedures.Output+NonNull");
			Assert.IsNotNull(type);
			Assert.IsEmpty(type.GetConstructors()[0].GetParameters());

			#region Expose InvalidCastException

			Utilities.SetConnectionString(ref type);
			inst = type.GetConstructors()[0].Invoke(new object[] { });
			type.GetMethod("NonQuery").Invoke(inst, new object[] { });
			Assert.AreEqual(5, type.GetField("Tester").GetValue(inst));
			Assert.AreEqual("Blarggy", type.GetField("String").GetValue(inst));

			#endregion

			#endregion

			#region p_Output_Test

			type = types.FirstOrDefault(t => t.FullName == "Procedures.Output+Test");
			Assert.IsNotNull(type);
			Assert.IsEmpty(type.GetConstructors()[0].GetParameters());

			#region Expose System.InvalidOperationException : the Size property has an invalid size of 0.

			Utilities.SetConnectionString(ref type);
			inst = type.GetConstructors()[0].Invoke(new object[] { });
			type.GetMethod("NonQuery").Invoke(inst, new object[] { });
			Assert.AreEqual(42, type.GetField("Output").GetValue(inst));
			Assert.AreEqual("Marvin", type.GetField("String").GetValue(inst));

			#endregion

			#endregion

			#region p_TableParameter

			type = types.FirstOrDefault(t => t.FullName == "Procedures.Misc+TableParameter");
			Assert.IsNotNull(type);

			Utilities.SetConnectionString(ref type);

			Type tt = types.FirstOrDefault(t => t.FullName == "Procedures.TableTypes.TableType");
			Assert.IsNotNull(tt);

			PropertyInfo column1 = tt.GetProperty("Column1");
			Assert.IsNotNull(column1);

			PropertyInfo column2 = tt.GetProperty("Column2");
			Assert.IsNotNull(column2);

			Array @params = Array.CreateInstance(tt, 2);
			object first = Activator.CreateInstance(tt);
			object second = Activator.CreateInstance(tt);

			column1.SetValue(first, 7, null);
			column2.SetValue(first, "Test", null);
			column1.SetValue(second, 8, null);
			column2.SetValue(second, "Test 2", null);

			@params.SetValue(first, 0);
			@params.SetValue(second, 1);

			object proc = Activator.CreateInstance(type, @params);
			type.GetMethod("NonQuery").Invoke(proc, new object[] { });

			SqlConnection conn = new SqlConnection(Scaffold.ConnectionString);
			SqlCommand cmd = new SqlCommand("select * from NoIdentityType where NoIdentityID in (7, 8)", conn);

			try
			{
				conn.Open();
				SqlDataReader reader = cmd.ExecuteReader();
				Assert.IsTrue(reader.HasRows);

				while (reader.Read())
				{
					switch ((int)reader["NoIdentityID"])
					{
					case 7:
						Assert.AreEqual("Test", reader["NoIdentity"]);
						break;

					case 8:
						Assert.AreEqual("Test 2", reader["NoIdentity"]);
						break;

					default:
						Assert.Fail("Unkown data received");
						break;
					}
				}
			}
			finally
			{
				conn.Close();
			}

			#endregion
		}
	}
}
