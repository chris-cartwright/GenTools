
using System;
using System.Collections.Generic;

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

	public class Shouldnt : ShouldntSee_<Shouldnt>
	{
		public static Shouldnt Load(long id)
		{
			return LoadFull(id);
		}

		public static object Create(Shouldnt column)
		{
			return CreateFull(column);
		}

		public static bool Save(Shouldnt column)
		{
			return SaveFull(column);
		}

		public string ProjectString { get; set; }
		public string CopyFromString { get; set; }

		public Shouldnt()
		{
			ProjectString = String.Empty;
			CopyFromString = String.Empty;
		}

		public override void CopyFrom<TOther>(TOther other)
		{
			base.CopyFrom(other);
			CopyFromString += "Shouldnt";
		}

		public override void Project(Dictionary<string, object> data)
		{
			base.Project(data);
			ProjectString += "Shouldnt";
		}
	}

	public class ShouldntChild : Shouldnt
	{
		public new string ShouldntSee
		{
			get { return base.ShouldntSee; }
			private set { base.ShouldntSee = value; }
		}

		public override void Project(Dictionary<string, object> data)
		{
			base.Project(data);
			ProjectString += "ShouldntChild";
		}

		public override void CopyFrom<TOther>(TOther other)
		{
			base.CopyFrom(other);
			CopyFromString += "ShouldntChild<T>";
		}

		public void CopyFrom(ShouldntChild other)
		{
			CopyFrom((Shouldnt)other);
			CopyFromString += "ShouldntChild";
		}
	}
}
