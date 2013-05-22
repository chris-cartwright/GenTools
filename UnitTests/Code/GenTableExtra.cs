
namespace Tables.Extra
{
	public class BadColumn : BadColumnType<BadColumn>
	{
		public static BadColumn Load(int id)
		{
			return LoadFull(id);
		}
	}
}
