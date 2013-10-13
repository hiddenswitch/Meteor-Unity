using System;
using System.Collections;
using UnityEngine;
using Net.DDP.Client;
using Schema;
using Extensions;

public static class Accounts {
	public static Collection<Meteor.MongoDocument> Users {
		get;
		private set;
	}

	static Accounts() {
//		FacebookManager.sessionOpenedEvent += HandleFacebookSessionOpened;
//
//		FacebookBinding.init ();
//		FacebookBinding.setSessionLoginBehavior (FacebookSessionLoginBehavior.UseSystemAccountIfPresent);
	}

	#region Passwords
	const string CreateUserMethodName = "createUser";
	const string LoginUserMethodName = "login";
	#endregion

	#region SRP
	const string BeginPasswordExchangeMethodName = "beginPasswordExchange";
	#endregion

	#region Facebook

	#endregion

	#region IAccountManager implementation

	public static Coroutine LoginWithFacebook ()
	{
		throw new NotImplementedException ();
	}

	public static Coroutine LoginWithGoogle ()
	{
		throw new NotImplementedException ();
	}

	public static Method<LoginUserResult> LoginWith(string username, string password)
	{
		var loginMethod = LiveData.Instance.Call<LoginUserResult> (LoginUserMethodName, new InsecureLoginUserOptions () {
			password = password,
			user = new LoginUserUser()
			{
				username = username
			}
		});

		loginMethod.OnResponse += HandleOnLogin;

		return loginMethod;
	}

	static void HandleOnLogin (Meteor.Error error, LoginUserResult response)
	{
		if (error == null) {
			SubscribeToUsers ();
		}
	}

	public static Coroutine CreateAndLoginWith (string email, string username, string password)
	{
		var createUserAndLoginMethod = LiveData.Instance.Call<LoginUserResult>(CreateUserMethodName, new CreateUserOptions () {
			profile = new Profile()
			{
				name = username
			},
			email = email,
			srp = srp4net.Helpers.Crypto.SRP.GenerateVerifier(password),
			username = username
		});

		createUserAndLoginMethod.OnResponse += HandleOnLogin;

		return createUserAndLoginMethod;
	}

	static void SubscribeToUsers ()
	{
		if (Users == null) {
			Users = LiveData.Instance.Subscribe<Meteor.MongoDocument> ("users", "userData");
		}
	}

	#endregion
}

