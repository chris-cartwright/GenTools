
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

		public static bool Save(Unique column)
		{
			return SaveFull(column);
		}
	}

	public class Nullable : NullableTypes<Nullable>
	{
		public static Nullable Load(short id)
		{
			return LoadFull(id);
		}

		public static object Create(Nullable column)
		{
			return CreateFull(column);
		}

		public static bool Save(Nullable column)
		{
			return SaveFull(column);
		}
	}
}
