using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[AttributeUsage(AttributeTargets.Assembly)]
internal class GitRevisionAttribute : Attribute
{
	public string Revision { get; private set; }

	public GitRevisionAttribute(string rev)
	{
		Revision = rev;
	}
}
