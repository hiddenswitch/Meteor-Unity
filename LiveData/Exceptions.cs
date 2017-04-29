using System;
using System.Runtime.Serialization;

namespace Meteor {
	public class MeteorException : System.Exception {
		private int _code;

		int Code {
			get {
				return _code;
			}
		}

		public MeteorException() : base()
		{
		}

		public MeteorException(int code, String message) : base(message)
		{
			this._code = code;
		}

		public MeteorException(int code, String message, Exception innerException) : base(message, innerException)
		{
			this._code = code;
		}

		protected MeteorException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}
	}
}
