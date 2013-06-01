using System.Configuration;
using Common;
using JetBrains.Annotations;

namespace GenTypes
{
	public class Configuration : ConfigurationElementBase
	{
		[ConfigurationProperty("Language", DefaultValue = "_")]
		public string Language
		{
			get { return (string)this["Language"]; }
			[UsedImplicitly]
			set { this["Language"] = value; }
		}
	}

	public class ConfigurationSection : ConfigurationSectionBase<Configuration>
	{
	}
}
