using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
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

	public class ConfigurationSection : Common.ConfigurationSectionBase<Configuration>
	{
	}
}
