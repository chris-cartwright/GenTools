using System.ComponentModel;

namespace Tables
{
	public abstract class WrappedTable : INotifyPropertyChanged
	{
		public static readonly string ConnectionString = null;

		public event PropertyChangedEventHandler PropertyChanged;

		private bool _trackChanges;

		public bool IsDirty { get; protected set; }
		public bool TrackChanges
		{
			get { return _trackChanges; }
			set
			{
				if (value == _trackChanges)
					return;

				_trackChanges = value;
				if (_trackChanges)
					PropertyChanged += OnPropertyChanged;
				else
					PropertyChanged -= OnPropertyChanged;
			}
		}

		private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			IsDirty = true;
		}

		protected void Changed(string property)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(property));
		}
	}
}
