﻿using System;

namespace Common
{
	[AttributeUsage(AttributeTargets.Assembly)]
	public class GitRevisionAttribute : Attribute
	{
		public string Revision { get; private set; }
		public bool Dirty { get; private set; }

		public GitRevisionAttribute(string rev, bool dirty)
		{
			Revision = rev;
			Dirty = dirty;
		}
	}
}