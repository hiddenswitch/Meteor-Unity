namespace Meteor.Internal
{

	public class PasswordDigest
	{
		public string digest;
		public string algorithm = "sha-256";

		public PasswordDigest (string password)
		{
			var sha256 = new System.Security.Cryptography.SHA256Managed ();
			var hash = sha256.ComputeHash (System.Text.Encoding.UTF8.GetBytes (password));
			digest = System.BitConverter.ToString (hash).Replace ("-", "").ToLower ();
		}
	}
}
