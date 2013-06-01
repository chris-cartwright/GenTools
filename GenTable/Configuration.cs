using System.Configuration;
using Common;
using JetBrains.Annotations;

namespace GenTable
{
	public class Configuration : ConfigurationElementBase
	{
		[ConfigurationProperty("CollisionPostfix", DefaultValue = "_")]
		public string CollisionPostfix
		{
			get { return (string)this["CollisionPostfix"]; }
			[UsedImplicitly]
			set { this["CollisionPostfix"] = value; }
		}
	}

	public class ConfigurationSection : ConfigurationSectionBase<Configuration>
	{
	}
}
