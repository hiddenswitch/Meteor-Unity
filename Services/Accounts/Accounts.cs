using System;
using System.Collections;
using UnityEngine;
using Meteor;
using Extensions;

namespace Meteor {
	public static class Accounts {
		const string TokenKey = "Meteor.Accounts.Token";
		const string GuestUsernameKey = "Meteor.Accounts.GuestUsername";
		const string GuestEmailKey = "Meteor.Accounts.GuestEmail";
		const string GuestPasswordKey = "Meteor.Accounts.GuestPassword";

		public static Error Error {get; private set;}
		public static LoginUserResult Response {get; private set;}

		public static Collection<MongoDocument> Users {
			get;
			private set;
		}

		public static string UserId {
			get {
				return Response.id;
			}
		}

		static Accounts() {
			Error = new Error () {
				error = 500,
				reason = "You have not attempted to login yet!"
			};
			// Check that we're connected to the server. If we're not, print an error.
			if (!LiveData.Instance.Connected) {
				Debug.LogError ("Meteor.Accounts: You are not connected to a server. Make sure to call LiveData.Instance.Connect().");
			}
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
			Error = error;
			Response = response;

			if (error == null) {
				PlayerPrefs.SetString (TokenKey, response.token);
			} else {
				PlayerPrefs.SetString (TokenKey, null);
				Debug.LogWarning (error.reason);
			}
		}

		public static Coroutine LoginAsGuest() {
			return CoroutineHost.Instance.StartCoroutine (LoginAsGuestCoroutine ());
		}

		static IEnumerator LoginAsGuestCoroutine ()
		{
			var tokenLogin = LoginWithToken ();
//			// If we can login with token, go for it.
			yield return (Coroutine)tokenLogin;
			if (tokenLogin.Error == null) {
				yield break;
			}
			// Failed to login with token

			// Create a guest account.
			var guestUsername = PlayerPrefs.GetString (GuestUsernameKey, null);
			var guestEmail = PlayerPrefs.GetString (GuestEmailKey, null);
			var guestPassword = PlayerPrefs.GetString (GuestPasswordKey, null);
			Debug.Log (guestUsername);
			if (!string.IsNullOrEmpty (guestUsername)) {
				yield return (Coroutine)Accounts.LoginWith (guestUsername, guestPassword);
				if (Error == null) {
					yield break;
				}
			}




			var padding = UnityEngine.Random.Range (0, Int32.MaxValue);
			guestUsername = string.Format ("anonymous{0}@partyga.me", padding);
			guestEmail = string.Format ("player{0}", padding);
			guestPassword = UnityEngine.Random.Range (0, Int32.MaxValue).ToString ();
			PlayerPrefs.SetString (GuestUsernameKey, guestUsername);
			PlayerPrefs.SetString (GuestEmailKey, guestEmail);
			PlayerPrefs.SetString (guestPassword, guestPassword);
			yield return Accounts.CreateAndLoginWith (guestUsername, guestEmail, guestPassword);
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

		#endregion
	}
}
