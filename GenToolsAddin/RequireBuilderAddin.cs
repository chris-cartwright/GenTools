using System;
using System.Linq;
using System.Reflection;
using NUnit.Core;
using NUnit.Core.Extensibility;

namespace GenToolsAddin
{
	[NUnitAddin]
	public class RequireBuilderAddin : IAddin, ISuiteBuilder
	{
		public bool Install(IExtensionHost host)
		{
			IExtensionPoint builders = host.GetExtensionPoint("SuiteBuilders");
			if (builders == null)
				return false;

			builders.Install(this);
			return true;
		}

		public bool CanBuildFrom(Type type)
		{
			return Reflect.HasMethodWithAttribute(type, "RequireDecoratorAttribute", false);
		}

		public Test BuildFrom(Type type)
		{
			MethodInfo[] methods = Reflect.GetMethodsWithAttribute(type, "RequireDecoratorAttribute", false);
			TestSuite suite = new TestSuite(type);
			foreach (MethodInfo method in methods.Where(m => m.GetCustomAttribute<RequireDecoratorAddin>() == null))
			{
				suite.Add(new NUnitTestMethod(method));
			}

			return suite;
		}
	}
}
