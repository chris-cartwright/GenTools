using System.Configuration;
using Common;

namespace GenTypes
{
	public class Configuration : ConfigurationElementBase
	{
		[ConfigurationProperty("Language", DefaultValue = "_")]
		public string Language
		{
			get { return (string)this["Language"]; }
			set { this["Language"] = value; }
		}
	}

	public class ConfigurationSection : ConfigurationSectionBase<Configuration>
	{
	}
}
