using System;
using System.Configuration;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;

namespace Common
{
	public class ConfigurationElementBase : ConfigurationElement
	{
		private const BindingFlags Flags = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance;

		[ConfigurationProperty("LoggingLevel", DefaultValue = Logger.Level.Error)]
		public Logger.Level LoggingLevel
		{
			get { return (Logger.Level)this["LoggingLevel"]; }
			set { this["LoggingLevel"] = value; }
		}

		[ConfigurationProperty("MasterNamespace", IsRequired = true, DefaultValue = "Procedures")]
		[RegexStringValidator(@"^[\w_][\w\d\._]+$")]
		public string MasterNamespace
		{
			get { return (string)this["MasterNamespace"]; }
			set { this["MasterNamespace"] = value; }
		}

		[ConfigurationProperty("Name", IsRequired = true, IsKey = true)]
		public string Name
		{
			get { return (string)this["Name"]; }
			set { this["Name"] = value; }
		}

		[ConfigurationProperty("ConnectionString")]
		public string ConnectionString
		{
			get { return (string)this["ConnectionString"]; }
			set { this["ConnectionString"] = value; }
		}

		[ConfigurationProperty("OutputFile", IsRequired = true)]
		public string OutputFile
		{
			get { return (string)this["OutputFile"]; }
			set { this["OutputFile"] = value; }
		}

		public void CopyTo<T>(ref T obj)
		{
			Type type = typeof(T);
			foreach (PropertyInfo pi in typeof(ConfigurationElementBase).GetProperties(Flags))
				type.GetProperty(pi.Name).SetValue(obj, pi.GetValue(this, null), null);
		}

		public void CopyFrom<T>(ref T obj)
		{
			Type type = typeof(T);
			foreach (PropertyInfo pi in typeof(ConfigurationElementBase).GetProperties(Flags))
				pi.SetValue(this, type.GetProperty(pi.Name).GetValue(obj, null), null);
		}

		public override bool IsReadOnly()
		{
			return false;
		}
	}

	public abstract class ConfigurationElementCollectionBase<T> : ConfigurationElementCollection
		where T : ConfigurationElementBase, new()
	{
		protected override ConfigurationElement CreateNewElement()
		{
			return new T();
		}

		protected override object GetElementKey(ConfigurationElement element)
		{
			return ((T)element).Name;
		}

		public string[] Keys
		{
			get { return BaseGetAllKeys().Select(p => (string)p).ToArray(); }
		}

		public T Get(string key)
		{
			return BaseGet(key) as T;
		}
	}

	public class ConfigurationSectionBase<T> : ConfigurationSection
		where T : ConfigurationElementBase, new()
	{
		[ConfigurationProperty("", IsRequired = true, IsDefaultCollection = true)]
		public ConfigurationElementCollectionBase<T> Instances
		{
			get { return (ConfigurationElementCollectionBase<T>)this[""]; }
			[UsedImplicitly]
			set { this[""] = value; }
		}

		[UsedImplicitly]
		public string[] Keys
		{
			get { return Instances.Keys; }
		}

		public T Get(string key)
		{
			return Instances.Get(key);
		}
	}
}
