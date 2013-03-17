using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace GenProc
{
	public class Configuration : Common.ConfigurationElementBase
	{
		[ConfigurationProperty("CollisionPrefix", DefaultValue = "C")]
		public string CollisionPrefix
		{
			get { return (string)this["CollisionPrefix"]; }
			set { this["CollisionPrefix"] = value; }
		}

		[ConfigurationProperty("Monolithic", DefaultValue = false)]
		public bool Monolithic
		{
			get { return (bool)this["Monolithic"]; }
			set { this["Monolithic"] = value; }
		}

		[ConfigurationProperty("MiscClass", IsRequired = true, DefaultValue = "Misc")]
		[RegexStringValidator(@"^[\w_][\w\d\._]+$")]
		public string MiscClass
		{
			get { return (string)this["MiscClass"]; }
			set { this["MiscClass"] = value; }
		}

		[ConfigurationProperty("OutputDirectory")]
		public string OutputDirectory
		{
			get { return (string)this["OutputDirectory"]; }
			set { this["OutputDirectory"] = value; }
		}
	}

	public class ConfigurationSection : Common.ConfigurationSectionBase<Configuration>
	{
	}
}
