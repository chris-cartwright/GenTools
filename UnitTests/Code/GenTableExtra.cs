
namespace Tables.Extra
{
	public class BadColumn : BadColumnType<BadColumn>
	{
		public static BadColumn Load(int id)
		{
			return LoadFull(id);
		}

		public static bool Save(BadColumn column)
		{
			return SaveFull(column);
		}
	}

	public class Unique : UniqueValues<Unique>
	{
		public static Unique Load(byte id)
		{
			return LoadFull(id);
		}

		public static object Create(Unique column)
		{
			return CreateFull(column);
		}
	}
}
