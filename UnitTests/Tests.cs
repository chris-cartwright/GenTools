using System.Collections.Generic;
using NUnit.Framework;

namespace UnitTests
{
	[TestFixture]
	public class OrderedDictionary
	{
		[Test]
		public void Order()
		{
			int[] keys = new[] { 8, 4, 3, 7 };

			OrderedDictionary<int, int> dict = new OrderedDictionary<int, int>();
			dict[8] = 4;
			dict[4] = 6;
			dict[3] = 5;
			dict[7] = 2;

			int counter = 0;
			foreach (KeyValuePair<int, int> pair in dict)
			{
				switch (counter)
				{
				case 0:
					Assert.AreEqual(8, pair.Key);
					Assert.AreEqual(4, pair.Value);
					break;

				case 1:
					Assert.AreEqual(4, pair.Key);
					Assert.AreEqual(6, pair.Value);
					break;

				case 2:
					Assert.AreEqual(3, pair.Key);
					Assert.AreEqual(5, pair.Value);
					break;

				case 3:
					Assert.AreEqual(7, pair.Key);
					Assert.AreEqual(2, pair.Value);
					break;

				default:
					Assert.Fail("More keys than expected.");
					break;
				}

				counter++;
			}

			for (int i = 0; i < keys.Length; i++)
				Assert.AreEqual(keys[i], dict.Keys[i]);
		}
	}
}
