namespace Schema
{
	public class Profile
	{
		public string name;
		public float[] location;
	}

	public class CreateUserOptions
	{
		public Profile profile;
		public string username;
		public string email;
		public Verifier srp;
	}

	public class BeginPasswordExchangeOptions
	{
		public LoginUserUser user;
		public string A;
	}

	public class LoginUserUser
	{
		public string username;
	}

	public class LoginUserResult
	{
		public string token;
		public string id;
	}

	public class ChallengeResponseOptions
	{
		public ChallengeResponse srp;
	}

	public class InsecureLoginUserOptions
	{
		public LoginUserUser user;
		public string password;
	}

	public class Verifier
	{
		public string identity;
		public string salt;
		public string verifier;
	}

	public class PasswordChallenge
	{
		public string B;
		public string identity;
		public string salt;
	}

	public class ChallengeResponse
	{
		public string M;
	}

	public class StartExchange
	{
		public string A;
	}

	public class PasswordExchangeVerifier
	{
		public string A;
		public PasswordExchangeVerifierUser user;
	}

	public class PasswordExchangeVerifierUser
	{
		public string username;
	}
}

