using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.CSharp;
using NUnit.Framework;

namespace UnitTests
{
	internal static class Utilities
	{
		public enum Include { WrappedProcedure };

		private static readonly string[] Assemblies = new[]
		{
			"mscorlib.dll",
			"System.dll",
			"System.Data.dll"
		};

		private static string GetDll(string file)
		{
			Regex regex = new Regex(@".*(/|\\)[a-zA-Z]+\.cs$");
			Match match = regex.Match(file);
			Assert.IsTrue(match.Success);
			return match.Groups[0].Value;
		}

		private static string ExtractCode(string file)
		{
			string path = Path.Combine(Directory.GetCurrentDirectory(), file);

			string name = String.Format("UnitTests.Code.{0}", file);
			using (Stream resource = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
			{
				Debug.Assert(resource != null, "Missing resource: " + name);
				StreamReader reader = new StreamReader(resource);
				StreamWriter writer = new StreamWriter(path);

				string line;
				while ((line = reader.ReadLine()) != null)
					writer.WriteLine(line);

				reader.Close();
				writer.Close();
			}

			return path;
		}

		public static Assembly Compile(string file, Include inc)
		{
			string dll = GetDll(file);

			CSharpCodeProvider csc = new CSharpCodeProvider(new Dictionary<string, string>()
			{
				{ "CompilerVersion", "v4.0" }
			});
			CompilerParameters ps = new CompilerParameters(Assemblies, dll)
			{
				WarningLevel = 4,
				TreatWarningsAsErrors = true,
				OutputAssembly = file + ".dll"
			};

			List<string> files = new List<string>() { file };

			string path = null;
			if (inc == Include.WrappedProcedure)
				path = ExtractCode("WrappedProcedure.cs");

			if (path != null)
				files.Add(path);

			CompilerResults cr = csc.CompileAssemblyFromFile(ps, files.ToArray());
			if (cr.Errors.Count > 0)
			{
				foreach (CompilerError error in cr.Errors)
				{
					Console.Error.WriteLine(error);
				}

				Assert.Fail("Could not compile source file: " + file);
			}

			return cr.CompiledAssembly;
		}

		public static void Apply<TFirst, TSecond>(this IEnumerable<TFirst> first, IEnumerable<TSecond> second,
			Action<TFirst, TSecond> action)
		{
			IEnumerator<TSecond> enumer = second.GetEnumerator();
			foreach (TFirst f in first)
			{
				enumer.MoveNext();
				action(f, enumer.Current);
			}
		}
	}

	public class OrderedDictionary<TKey, TValue> : Dictionary<TKey, TValue>
	{
		private List<TKey> _keys;

		public new TKey[] Keys
		{
			get { return _keys.ToArray(); }
		}

		public OrderedDictionary()
		{
			_keys = new List<TKey>();
		}

		public new IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			return _keys.Select(key => new KeyValuePair<TKey, TValue>(key, base[key])).GetEnumerator();
		}

		public new void Add(TKey key, TValue value)
		{
			base[key] = value;
		}

		public new TValue this[TKey key]
		{
			get { return base[key]; }
			set
			{
				_keys.Add(key);
				base[key] = value;
			}
		}
	}

	internal class ParamInfo
	{
		public static void AreEqual(ParamInfo lhs, ParameterInfo rhs)
		{
			Assert.AreEqual(lhs.Name, rhs.Name, lhs.Name);
			Assert.AreEqual(lhs.ParameterType, rhs.ParameterType, lhs.Name);
			Assert.AreEqual(lhs.DefaultValue, rhs.DefaultValue, lhs.Name);
			Assert.AreEqual(lhs.IsOut, rhs.IsOut, lhs.Name);
		}

		public Object DefaultValue { get; set; }
		public bool IsOut { get; set; }
		public string Name { get; set; }
		public Type ParameterType { get; set; }

		public ParamInfo(string name, Type parameterType)
		{
			DefaultValue = DBNull.Value;
			Name = name;
			ParameterType = parameterType;
		}
	}
}
