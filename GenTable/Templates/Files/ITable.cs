using System.ComponentModel;

namespace Tables
{
	public interface ITable
	{
		event PropertyChangedEventHandler PropertyChanged;

		bool IsDirty { get; }
		bool TrackChanges { get; set; }
	}
}
