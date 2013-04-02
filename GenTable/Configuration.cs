using System.Configuration;
using Common;

namespace GenTable
{
	public class Configuration : ConfigurationElementBase
	{
		[ConfigurationProperty("CollisionPostfix", DefaultValue = "_")]
		public string CollisionPostfix
		{
			get { return (string)this["CollisionPostfix"]; }
			set { this["CollisionPostfix"] = value; }
		}
	}

	public class ConfigurationSection : ConfigurationSectionBase<Configuration>
	{
	}
}
