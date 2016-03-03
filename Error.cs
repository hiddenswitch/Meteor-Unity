namespace Meteor
{
	/// <summary>
	/// A Meteor error.
	/// </summary>
	[System.Serializable]
	public class Error
	{
		/// <summary>
		/// The reason for the error. Corresponds to the second argument of your <code>new Meteor.Error</code> call in your Meteor code.
		/// </summary>
		public string reason;
		/// <summary>
		/// The error code. Corresponds to the first argument of your <code>new Meteor.Error</code> call in your Meteor code.
		/// </summary>
		public int error;
		/// <summary>
		/// The error details. Corresponds to the third and last argument of your <code>new Meteor.Error</code> call in your Meteor code.
		/// </summary>
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

		/// <summary>
		/// Serves as a hash function for a <see cref="Meteor.Error"/> object. This is defined as the error code in order to facilitate better comparisons.
		/// </summary>
		/// <returns>A hash code for this instance that is suitable for use in hashing algorithms and data structures such as a hash table.</returns>
		public override int GetHashCode ()
		{
			return error;
		}

		/// <summary>
		/// Determines whether the specified <see cref="System.Object"/> is equal to the current <see cref="Meteor.Error"/>. If both objects are errors,
		/// their reason and error codes will be compared instead of the instances.
		/// </summary>
		/// <param name="obj">The <see cref="System.Object"/> to compare with the current <see cref="Meteor.Error"/>.</param>
		/// <returns><c>true</c> if the specified <see cref="System.Object"/> is equal to the current <see cref="Meteor.Error"/>;
		/// otherwise, <c>false</c>.</returns>
		public override bool Equals (object obj)
		{
			if (obj == null) {
				return IsNull ();
			}

			var other = (Error)obj;

			return reason == other.reason
			&& error == other.error;
		}

		/// <summary>
		/// Compares two error objects to see if their reasons and error codes are equal.
		/// </summary>
		public static bool operator == (Error a, Error b)
		{
			var isNullA = (object)a == null || a.IsNull ();
			var isNullB = (object)b == null || b.IsNull ();

			var nullComparison = isNullA && isNullB;

			return nullComparison || (isNullA ? b.Equals (a) : a.Equals (b));
		}

		/// <summary>
		/// Compares two error objects to see if their reasons and error codes are not equal.
		/// </summary>
		public static bool operator != (Error a, Error b)
		{
			return !(a == b);
		}
	}
}

