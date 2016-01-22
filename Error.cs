namespace Meteor
{
	[System.Serializable]
	public class Error
	{
		public string reason;
		public int error;
		public string details;

		public Error ()
		{
		}

		bool IsNull ()
		{
			return error == 0
			&& string.IsNullOrEmpty (reason)
			&& string.IsNullOrEmpty (details);
		}

		public override int GetHashCode ()
		{
			return base.GetHashCode ();
		}

		public override bool Equals (object obj)
		{
			if (obj == null) {
				return IsNull ();
			}

			var other = (Error)obj;

			return reason == other.reason
			&& error == other.error;
		}

		public static bool operator == (Error a, Error b)
		{
			var isNullA = (object)a == null || a.IsNull ();
			var isNullB = (object)b == null || b.IsNull ();

			var nullComparison = isNullA && isNullB;

			return nullComparison || (isNullA ? b.Equals (a) : a.Equals (b));
		}

		public static bool operator != (Error a, Error b)
		{
			return !(a == b);
		}
	}
}

