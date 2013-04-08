using System;
using System.Reflection;
using NUnit.Core;
using NUnit.Core.Extensibility;

namespace GenToolsAddin
{
	[NUnitAddin]
	public class RequireDecoratorAddin : Attribute, IAddin, ITestDecorator
	{
		public bool Install(IExtensionHost host)
		{
			IExtensionPoint decorators = host.GetExtensionPoint("TestDecorators");
			if (decorators == null)
				return false;

			decorators.Install(this);
			return true;
		}

		public Test Decorate(Test test, MemberInfo member)
		{
			throw new NotImplementedException();
		}
	}
}
