using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Common
{
	public static class ReturnValue
	{
		public const int Success = 0;
		public const int ConnectFailed = 1;
		public const int ParseError = 2;
		public const int FileAccess = 3;
		public const int InvalidOptions = 4;
		public const int UnknownConnection = 5;
		public const int Unknown = 255;
	}
}
