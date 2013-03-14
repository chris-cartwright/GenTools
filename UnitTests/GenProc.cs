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

		[TestFixture]
		public class Procedure
		{
			global::GenProc.Procedure p;

			[Test]
			public void Name()
			{
				p = new global::GenProc.Procedure("p_Test_Name");
				Assert.IsEmpty(p.Parameters);
				Assert.AreEqual("p_Test_Name", p.Original);
				Assert.AreEqual("Name", p.Name);
				Assert.AreEqual("Name", p.NameClean);
				Assert.AreEqual(new string[] { "Test" }, p.Path);

				p = new global::GenProc.Procedure("Test_Name");
				Assert.IsEmpty(p.Parameters);
				Assert.AreEqual("Test_Name", p.Original);
				Assert.AreEqual("Name", p.Name);
				Assert.AreEqual("Name", p.NameClean);
				Assert.AreEqual(new string[] { "Test" }, p.Path);

				p = new global::GenProc.Procedure("TestName");

				p = new global::GenProc.Procedure("Test_Second");

				p = new global::GenProc.Procedure("Test_Third");

				p = new global::GenProc.Procedure("Second_new");

				p = new global::GenProc.Procedure("Third_while");

				p = new global::GenProc.Procedure("p_Three_Deep_Procedure");
			}
		}
    }
}
