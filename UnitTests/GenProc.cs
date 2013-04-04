using System;
using System.Data.SqlClient;
using System.IO;
using GenProc;
using NUnit.Framework;

namespace UnitTests
{
	[TestFixture]
	public class GenProc
	{
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
				int i = 0;

				_tree.Insert(new[] { "Branch 1", "Sub 1" }, i);
				Assert.AreEqual(1, _tree.Branches.Count);
				Assert.AreEqual(1, _tree.Branches[0].Branches.Count);
				Assert.AreEqual("Branch 1", _tree.Branches[0].Name);
				Assert.AreEqual("Sub 1", _tree.Branches[0].Branches[0].Name);
				Assert.AreEqual(1, _tree.Branches[0].Branches[0].Leaves.Count);
				Assert.AreEqual(i, _tree.Branches[0].Branches[0].Leaves[0]);

				_tree.Insert(new[] { "Branch 1", "Sub 1" }, i);
				Assert.AreEqual(1, _tree.Branches.Count);
				Assert.AreEqual(1, _tree.Branches[0].Branches.Count);
				Assert.AreEqual("Branch 1", _tree.Branches[0].Name);
				Assert.AreEqual("Sub 1", _tree.Branches[0].Branches[0].Name);
				Assert.AreEqual(1, _tree.Branches[0].Branches[0].Leaves.Count);
				Assert.AreEqual(i, _tree.Branches[0].Branches[0].Leaves[0]);
			}
		}

		[TestFixture]
		public class Parameter
		{
			private global::GenProc.Parameter _p;

			[Test]
			public void NotFound()
			{
				_p = new global::GenProc.Parameter("Test", "doesn't exist", true, null);
				Assert.AreEqual(typeof(object), _p.Type);

				_p = new global::GenProc.Parameter("Test 2", "nvarchar(40)", true, null);
				Assert.AreEqual(typeof(object), _p.Type);
			}

			[Test]
			public void Mapping()
			{
				_p = new global::GenProc.Parameter("Test 1", "nvarchar", true, null);
				Assert.AreEqual(typeof(string), _p.Type);

				_p = new global::GenProc.Parameter("Test 2", "int", true, null);
				Assert.AreEqual(typeof(int), _p.Type);

				_p = new global::GenProc.Parameter("Test 3", "flag", true, null);
				Assert.AreEqual(typeof(bool), _p.Type);

				_p = new global::GenProc.Parameter("Test 4", "date", true, null);
				Assert.AreEqual(typeof(DateTime), _p.Type);
			}

			[Test]
			public void Case()
			{
				_p = new global::GenProc.Parameter("Test 1", "date", true, null);
				Assert.AreEqual(typeof(DateTime), _p.Type);

				_p = new global::GenProc.Parameter("Test 2", "DAte", true, null);
				Assert.AreEqual(typeof(DateTime), _p.Type);

				_p = new global::GenProc.Parameter("Test 3", "Date", true, null);
				Assert.AreEqual(typeof(DateTime), _p.Type);

				_p = new global::GenProc.Parameter("Test 4", "DatE", true, null);
				Assert.AreEqual(typeof(DateTime), _p.Type);
			}
		}

		private Program _genProc;

		[SetUp]
		public void SetUp()
		{
			SqlConnection conn = new SqlConnection(Scaffold.ConnectionString);
			_genProc = new Program(new Configuration() { OutputFile = "GenProc.cs" });

			conn.Open();
			_genProc.LoadProcedures(conn);
			conn.Close();
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
		}

		[Test]
		public void LoadProcedures()
		{
			/* Check loaded tree */
			Assert.AreEqual(0, _genProc.Procedures.Leaves.Count);
			Assert.AreEqual(3, _genProc.Procedures.Branches.Count);

			Branch<Procedure> brch = _genProc.Procedures.Branches[0];
			Assert.AreEqual("Completely", brch.Name);
			Assert.AreEqual(1, brch.Leaves.Count);
			Assert.AreEqual("Valid", brch.Leaves[0].Name);

			brch = _genProc.Procedures.Branches[1];
			Assert.AreEqual("Misc", brch.Name);
			Assert.AreEqual(4, brch.Leaves.Count);
			Assert.AreEqual("ListProcedures", brch.Leaves[0].Name);
			Assert.AreEqual("ListTables", brch.Leaves[1].Name);
			Assert.AreEqual("ListTypeTables", brch.Leaves[2].Name);
			Assert.AreEqual("MissingUnderscore", brch.Leaves[3].Name);

			brch = _genProc.Procedures.Branches[2];
			Assert.AreEqual("No", brch.Name);
			Assert.AreEqual(1, brch.Leaves.Count);
			Assert.AreEqual("Params", brch.Leaves[0].Name);

			/* Check procedure definitions */
			Action<global::GenProc.Parameter, string, Type, string, bool, bool> verifyDefault = delegate(global::GenProc.Parameter parameter, string name, Type type, string def, bool isNull, bool isOutput)
			{
				Assert.AreEqual(name, parameter.Name);
				Assert.AreEqual(type, parameter.Type);
				Assert.AreEqual(def, parameter.Default);
				Assert.AreEqual(isNull, parameter.IsNull);
				Assert.AreEqual(isOutput, parameter.IsOutput);
			};

			Action<global::GenProc.Parameter, string, Type, bool, bool> verify = delegate(global::GenProc.Parameter parameter, string name, Type type, bool isNull, bool isOutput)
			{
				Assert.AreEqual(name, parameter.Name);
				Assert.AreEqual(type, parameter.Type);
				// Only IsNull - an empty string is legitimate
				Assert.IsNull(parameter.Default);
				Assert.AreEqual(isNull, parameter.IsNull);
				Assert.AreEqual(isOutput, parameter.IsOutput);
			};

			Procedure proc = _genProc.Procedures.Branches[0].Leaves[0];
			Assert.AreEqual(7, proc.Parameters.Count);
			verify(proc.Parameters[0], "@Column", typeof(int), false, false);
			verifyDefault(proc.Parameters[1], "@Second", typeof(byte), "0", false, false);
			verify(proc.Parameters[2], "@Third", typeof(string), false, false);
			verify(proc.Parameters[3], "@Nullable", typeof(int), true, false);
			verifyDefault(proc.Parameters[4], "@Default", typeof(string), "test default", false, false);
			verify(proc.Parameters[5], "@Output", typeof (int), true, true);
			verifyDefault(proc.Parameters[6], "@DefString", typeof(string), "", false, false);

			proc = _genProc.Procedures.Branches[1].Leaves[3];
			Assert.AreEqual(1, proc.Parameters.Count);
			verify(proc.Parameters[0], "@Column", typeof (int), false, false);

			proc = _genProc.Procedures.Branches[2].Leaves[0];
			Assert.AreEqual(0, proc.Parameters.Count);
		}

		[Test]
		public void WriteOutput()
		{
			string file = Path.Combine(Environment.CurrentDirectory, "GenProc.cs");
			if (File.Exists(file))
			{
				File.Delete(file);
			}

			_genProc.Write();
			Assert.IsTrue(File.Exists(file));
		}
	}
}
