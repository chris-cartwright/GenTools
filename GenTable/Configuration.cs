using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
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

	public class ConfigurationSection : Common.ConfigurationSectionBase<Configuration>
	{
	}
}
