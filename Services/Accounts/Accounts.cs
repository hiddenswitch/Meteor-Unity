using System;
using System.Collections;
using UnityEngine;
using Meteor;
using Extensions;

namespace Meteor {
	public static class Accounts {
		private const string TokenKey = "tokenKey";
		public static Collection<MongoDocument> Users {
			get;
			private set;
		}

		public static string UserId {
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

		public static Method<LoginUserResult> LoginWithToken() {
			var token = PlayerPrefs.GetString (TokenKey, null);

			var loginMethod = LiveData.Instance.Call<LoginUserResult> (LoginUserMethodName, new Meteor.LoginWithTokenOptions() {
				resume = token
			});

			loginMethod.OnResponse += HandleOnLogin;

			return loginMethod;
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

		static void HandleOnLogin (Error error, LoginUserResult response)
		{
			if (error == null) {
				PlayerPrefs.SetString (TokenKey, response.token);
				UserId = response.id;
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
				Users = LiveData.Instance.Subscribe<Meteor.MongoDocument> ("users", "users");
			}
		}

		#endregion
	}
}
