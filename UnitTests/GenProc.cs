using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace UnitTests
{
	[TestFixture]
    public class GenProc
    {
		[TestFixture]
		public class Branch
		{
			private global::GenProc.Branch<int> tree;

			[SetUp]
			public void SetUp()
			{
				tree = new global::GenProc.Branch<int>("Trunk");
			}

			[Test]
			public void Constructor()
			{
				Assert.AreEqual(tree.Name, "Trunk");
				Assert.IsNotNull(tree.Branches);
				Assert.IsNotNull(tree.Leaves);
				Assert.IsEmpty(tree.Branches);
				Assert.IsEmpty(tree.Leaves);
			}

			[Test]
			public void Insert()
			{
				int i = 0;

				tree.Insert(new string[] { }, i);
				Assert.IsEmpty(tree.Branches);
				Assert.AreEqual(1, tree.Leaves.Count);
				Assert.AreEqual(i, tree.Leaves[0]);

				i++;

				tree.Insert(new string[] { "Branch 1" }, i);
				Assert.AreEqual(1, tree.Branches.Count);
				Assert.IsEmpty(tree.Branches[0].Branches);
				Assert.AreEqual("Branch 1", tree.Branches[0].Name);
				Assert.AreEqual(1, tree.Branches[0].Leaves.Count);
				Assert.AreEqual(i, tree.Branches[0].Leaves[0]);

				i++;

				tree.Insert(new string[] { "Branch 1", "Sub 1" }, i);
				Assert.AreEqual(1, tree.Branches.Count);
				Assert.AreEqual(1, tree.Branches[0].Branches.Count);
				Assert.AreEqual("Branch 1", tree.Branches[0].Name);
				Assert.AreEqual("Sub 1", tree.Branches[0].Branches[0].Name);
				Assert.AreEqual(1, tree.Branches[0].Branches[0].Leaves.Count);
				Assert.AreEqual(i, tree.Branches[0].Branches[0].Leaves[0]);

				i++;

				tree.Insert(new string[] { "Branch 2", "Sub 1" }, i);
				Assert.AreEqual(2, tree.Branches.Count);
				Assert.AreEqual(1, tree.Branches[1].Branches.Count);
				Assert.AreEqual("Sub 1", tree.Branches[1].Branches[0].Name);
				Assert.AreEqual("Branch 2", tree.Branches[1].Name);
				Assert.AreEqual(0, tree.Branches[1].Leaves.Count);
				Assert.AreEqual(1, tree.Branches[1].Branches[0].Leaves.Count);
				Assert.AreEqual(i, tree.Branches[1].Branches[0].Leaves[0]);

				i++;

				tree.Insert(new string[] { "Branch 2", "Sub 2" }, i);
				Assert.AreEqual(2, tree.Branches.Count);
				Assert.AreEqual(2, tree.Branches[1].Branches.Count);
				Assert.AreEqual("Sub 2", tree.Branches[1].Branches[1].Name);
				Assert.AreEqual("Branch 2", tree.Branches[1].Name);
				Assert.AreEqual(1, tree.Branches[1].Branches[1].Leaves.Count);
				Assert.AreEqual(i, tree.Branches[1].Branches[1].Leaves[0]);

				i++;

				tree.Insert(new string[] { "Branch 3", "Sub 1", "Sub sub 1" }, i);
				Assert.AreEqual(3, tree.Branches.Count);
				Assert.AreEqual(1, tree.Branches[2].Branches.Count);
				Assert.AreEqual(1, tree.Branches[2].Branches[0].Branches.Count);
				Assert.AreEqual("Branch 3", tree.Branches[2].Name);
				Assert.AreEqual("Sub 1", tree.Branches[2].Branches[0].Name);
				Assert.AreEqual("Sub sub 1", tree.Branches[2].Branches[0].Branches[0].Name);
				Assert.IsEmpty(tree.Branches[2].Leaves);
				Assert.IsEmpty(tree.Branches[2].Branches[0].Leaves);
				Assert.AreEqual(1, tree.Branches[2].Branches[0].Branches[0].Leaves.Count);
				Assert.AreEqual(i, tree.Branches[2].Branches[0].Branches[0].Leaves[0]);

				i++;

				tree.Insert(new string[] { "Branch 3", "Sub 1", "Sub sub 1" }, i);
				Assert.AreEqual(3, tree.Branches.Count);
				Assert.AreEqual(1, tree.Branches[2].Branches.Count);
				Assert.AreEqual(1, tree.Branches[2].Branches[0].Branches.Count);
				Assert.AreEqual("Branch 3", tree.Branches[2].Name);
				Assert.AreEqual("Sub 1", tree.Branches[2].Branches[0].Name);
				Assert.AreEqual("Sub sub 1", tree.Branches[2].Branches[0].Branches[0].Name);
				Assert.IsEmpty(tree.Branches[2].Leaves);
				Assert.IsEmpty(tree.Branches[2].Branches[0].Leaves);
				Assert.AreEqual(2, tree.Branches[2].Branches[0].Branches[0].Leaves.Count);
				Assert.AreEqual(i, tree.Branches[2].Branches[0].Branches[0].Leaves[1]);
			}

			[Test]
			[ExpectedException(typeof(ArgumentException), ExpectedMessage="Cannot have duplicate leaves.")]
			public void InsertDuplicate()
			{
				int i = 0;

				tree.Insert(new string[] { "Branch 1", "Sub 1" }, i);
				Assert.AreEqual(1, tree.Branches.Count);
				Assert.AreEqual(1, tree.Branches[0].Branches.Count);
				Assert.AreEqual("Branch 1", tree.Branches[0].Name);
				Assert.AreEqual("Sub 1", tree.Branches[0].Branches[0].Name);
				Assert.AreEqual(1, tree.Branches[0].Branches[0].Leaves.Count);
				Assert.AreEqual(i, tree.Branches[0].Branches[0].Leaves[0]);

				tree.Insert(new string[] { "Branch 1", "Sub 1" }, i);
				Assert.AreEqual(1, tree.Branches.Count);
				Assert.AreEqual(1, tree.Branches[0].Branches.Count);
				Assert.AreEqual("Branch 1", tree.Branches[0].Name);
				Assert.AreEqual("Sub 1", tree.Branches[0].Branches[0].Name);
				Assert.AreEqual(1, tree.Branches[0].Branches[0].Leaves.Count);
				Assert.AreEqual(i, tree.Branches[0].Branches[0].Leaves[0]);
			}
		}

		[TestFixture]
		public class Parameter
		{
			private global::GenProc.Parameter p;

			[Test]
			public void NotFound()
			{
				p = new global::GenProc.Parameter("Test", "doesn't exist", true, null);
				Assert.AreEqual(typeof(object), p.Type);

				p = new global::GenProc.Parameter("Test 2", "nvarchar(40)", true, null);
				Assert.AreEqual(typeof(object), p.Type);
			}

			[Test]
			public void Mapping()
			{
				p = new global::GenProc.Parameter("Test 1", "nvarchar", true, null);
				Assert.AreEqual(typeof(string), p.Type);

				p = new global::GenProc.Parameter("Test 2", "int", true, null);
				Assert.AreEqual(typeof(int), p.Type);

				p = new global::GenProc.Parameter("Test 3", "flag", true, null);
				Assert.AreEqual(typeof(bool), p.Type);

				p = new global::GenProc.Parameter("Test 4", "date", true, null);
				Assert.AreEqual(typeof(DateTime), p.Type);
			}

			[Test]
			public void Case()
			{
				p = new global::GenProc.Parameter("Test 1", "date", true, null);
				Assert.AreEqual(typeof(DateTime), p.Type);

				p = new global::GenProc.Parameter("Test 2", "DAte", true, null);
				Assert.AreEqual(typeof(DateTime), p.Type);

				p = new global::GenProc.Parameter("Test 3", "Date", true, null);
				Assert.AreEqual(typeof(DateTime), p.Type);

				p = new global::GenProc.Parameter("Test 4", "DatE", true, null);
				Assert.AreEqual(typeof(DateTime), p.Type);
			}
		}

		global::GenProc.Program genProc;

		[Test]
		public void Parse()
		{
			genProc = new global::GenProc.Program(new global::GenProc.Configuration());
			global::GenProc.Procedure p;

			p = genProc.Parse("p_Test_Name");
			Assert.IsEmpty(p.Parameters);
			Assert.AreEqual("p_Test_Name", p.Original);
			Assert.AreEqual("Name", p.Name);
			Assert.AreEqual(new string[] { "Test" }, p.Path);

			p = genProc.Parse("Test_Name");
			Assert.IsEmpty(p.Parameters);
			Assert.AreEqual("Test_Name", p.Original);
			Assert.AreEqual("Name", p.Name);
			Assert.AreEqual(new string[] { "Test" }, p.Path);

			p = genProc.Parse("TestName");
			Assert.IsEmpty(p.Parameters);
			Assert.AreEqual("TestName", p.Original);
			Assert.AreEqual("TestName", p.Name);
			Assert.AreEqual(new string[] { "Misc" }, p.Path);

			p = genProc.Parse("Test_Second");
			Assert.IsEmpty(p.Parameters);
			Assert.AreEqual("Test_Second", p.Original);
			Assert.AreEqual("Second", p.Name);
			Assert.AreEqual(new string[] { "Test" }, p.Path);

			p = genProc.Parse("Second_new");
			Assert.IsEmpty(p.Parameters);
			Assert.AreEqual("Second_new", p.Original);
			Assert.AreEqual("new", p.Name);
			Assert.AreEqual(new string[] { "Second" }, p.Path);

			p = genProc.Parse("Third_while");
			Assert.IsEmpty(p.Parameters);
			Assert.AreEqual("Third_while", p.Original);
			Assert.AreEqual("while", p.Name);
			Assert.AreEqual(new string[] { "Third" }, p.Path);

			p = genProc.Parse("p_Three_Deep_Procedure");
			Assert.IsEmpty(p.Parameters);
			Assert.AreEqual("p_Three_Deep_Procedure", p.Original);
			Assert.AreEqual("Procedure", p.Name);
			Assert.AreEqual(new string[] { "Three", "Deep" }, p.Path);

			p = genProc.Parse("s_Three");
			Assert.IsEmpty(p.Parameters);
			Assert.AreEqual("s_Three", p.Original);
			Assert.AreEqual("Three", p.Name);
			Assert.AreEqual(new string[] { "Misc" }, p.Path);
		}
    }
}
