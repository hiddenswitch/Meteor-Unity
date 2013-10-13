using System;
using System.Collections;
using UnityEngine;
using Net.DDP.Client;
using Schema;
using Extensions;

public class Accounts : MonoBehaviour, IAccountManager {
	public event Action<bool> OnLogin;
	public bool canLoginFacebook;

	public Collection<object> Users {
		get;
		private set;
	}

	// Use this for initialization
	void Start () {
		// Init Facebook
//		FacebookManager.sessionOpenedEvent += HandleFacebookSessionOpened;
//
//		FacebookBinding.init ();
//		FacebookBinding.setSessionLoginBehavior (FacebookSessionLoginBehavior.UseSystemAccountIfPresent);
		canLoginFacebook = true;
	}

	// Update is called once per frame
	void Update () {

	}

	#region Passwords
	public const string CreateUserMethodName = "createUser";
	public const string LoginUserMethodName = "login";
	Method<LoginUserResult> loginMethod;

	void HandleOnLoginUserResponse (Meteor.Error error, LoginUserResult response)
	{
		Debug.LogWarning (string.Format ("Accounts.HandleOnLoginUserResponse: {0}",response.Serialize()));
		Debug.LogError (string.Format ("Accounts.HandleOnLoginUserResponse: {0}", error.Serialize ()));
		if (OnLogin != null) {
			OnLogin (true);
		}
		loginMethod.OnResponse -= HandleOnLoginUserResponse;
		loginMethod = null;
	}

	#endregion

	#region SRP
	public const string BeginPasswordExchangeMethodName = "beginPasswordExchange";
	Method<PasswordChallenge> beginPasswordExchangeMethod;

	void BeginPasswordExchange(string verifier, string username)
	{

	}
	#endregion

	#region Facebook
	
	void HandleFacebookSessionOpened ()
	{

	}

	#endregion

	#region IAccountManager implementation

	public void LoginWithFacebook ()
	{
		if (canLoginFacebook)
		{
//			FacebookBinding.login ();
		}

		if (OnLogin != null)
		{
			OnLogin (false);
		}
	}

	public void LoginWithGoogle ()
	{
		throw new NotImplementedException ();
	}

//	[Obsolete("Does not generate correct passwords.")]
//	public void LoginWith (string username, string password)
//	{
//		SubscribeToUsers ();
//
//		if (loginMethod != null)
//		{
//			Debug.LogWarning ("Accounts.LoginWith: A user login is already in progress.");
//		}
//
//		BeginPasswordExchangeOptions beginPasswordExchangeOptions = new BeginPasswordExchangeOptions () {
//			A = srp4net.Helpers.Crypto.SRP.StartExchange().A,
//			user = new LoginUserUser()
//			{
//				username = username
//			}
//		};
//
//		beginPasswordExchangeMethod = Client.Call<PasswordChallenge>(BeginPasswordExchangeMethodName, (Meteor.Error error, PasswordChallenge response) => {
//			ChallengeResponse challengeResponse = srp4net.Helpers.Crypto.SRP.RespondToChallenge(password,response.identity,response.salt,response.B);
//			loginMethod = Client.Call<LoginUserResult>(LoginUserMethodName, HandleOnLoginUserResponse, new ChallengeResponseOptions() {
//				srp = challengeResponse
//			});
//		},beginPasswordExchangeOptions);
//	}

	public void LoginWith(string username, string password)
	{
		SubscribeToUsers ();

		if (loginMethod != null)
		{
			Debug.LogWarning ("Accounts.LoginWith: A user login is already in progress.");
		}

		loginMethod = Client.Call<LoginUserResult> (LoginUserMethodName, HandleOnLoginUserResponse, new InsecureLoginUserOptions () {
			password = password,
			user = new LoginUserUser()
			{
				username = username
			}
		});
	}

	public void CreateAndLoginWith (string email, string username, string password)
	{
		SubscribeToUsers();

		if (loginMethod != null)
		{
			Debug.LogWarning ("Accounts.CreateAndLoginWith: A user login is already in progress.");
		}

		Client.Call<object>(CreateUserMethodName,
		                    delegate(Meteor.Error error, object response) {
			Debug.LogWarning (string.Format ("Accounts.HandleOnCreateUserResponse: {0}",response.Serialize()));
			Debug.LogError (string.Format ("Accounts.HandleOnCreateUserResponse: {0}", error.Serialize ()));

		}, new CreateUserOptions () {
			profile = new Profile()
			{
				name = username
			},
			email = email,
			srp = srp4net.Helpers.Crypto.SRP.GenerateVerifier(password),
			username = username
		});
	}

	void SubscribeToUsers ()
	{
		if (Users == null) {
			Users = Client.Subscribe<object> ("users", "userData");
		}
	}

	public LiveData Client {
		get;
		set;
	}

	#endregion
}

