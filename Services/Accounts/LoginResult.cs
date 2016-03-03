namespace Meteor
{
	/// <summary>
	/// The results of a login call.
	/// </summary>
	public class LoginResult
	{
		/// <summary>
		/// The token to log back in again.
		/// </summary>
		public string token;
		/// <summary>
		/// The user ID.
		/// </summary>
		public string id;
		/// <summary>
		/// A time, in ticks, when this login token was issued. Used to verify expiration.
		/// </summary>
		public long when;
		/// <summary>
		/// Was the user already logged in previously with this token?
		/// </summary>
		public bool alreadyLoggedIn;
	}	
}
