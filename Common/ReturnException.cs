using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Common
{
	public enum ReturnCode
	{
		Success,
		ConnectFailed,
		ParseError,
		FileAccess,
		InvalidOptions,
		UnknownConnection,
		Unknown = 255
	}

	public class ReturnException : Exception
	{
		public ReturnCode Code;

		public ReturnException(ReturnCode code)
			: base()
		{
			Code = code;
		}
	}
}
