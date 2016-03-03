using System;
using System.Collections;
using UnityEngine;
using Meteor;
using Meteor.Extensions;
using Meteor.Internal;

namespace Meteor
{
	/// <summary>
	/// Contains all the methods used to create and manage user accounts.
	/// </summary>
	public static class Accounts
	{
		/// <summary>
		/// The email domain used for guest accounts.
		/// </summary>
		public static string GuestEmailDomain = "example.com";
		/// <summary>
		/// The permissions requested by a Facebook login.
		/// </summary>
		public static string FacebookScope = "email";
		const string TokenKey = "Meteor.Accounts.Token";
		const string IdKey = "Meteor.Accounts.Id";
		const string GuestUsernameKey = "Meteor.Accounts.GuestUsername";
		const string GuestEmailKey = "Meteor.Accounts.GuestEmail";
		const string GuestPasswordKey = "Meteor.Accounts.GuestPassword";
		const string CreateUserMethodName = "createUser";
		const string LoginUserMethodName = "login";
		const string BeginPasswordExchangeMethodName = "beginPasswordExchange";
		static LoginResult _response;

		/// <summary>
		/// This event is raised when the login method is about to complete.
		/// </summary>
		public static event Action<Error, LoginResult> LoginMethodWillComplete;
		/// <summary>
		/// This event is raised when the login method did complete.
		/// </summary>
		public static event Action<Error, LoginResult> LoginMethodDidComplete;

		/// <summary>
		/// The users collection.
		/// </summary>
		/// <value>The users.</value>
		public static Meteor.Internal.ICollection Users {
			get {
				return LiveData.Instance.Collections ["users"];
			}
		}

		/// <summary>
		/// Is the user currently logged in?
		/// </summary>
		/// <value><c>true</c> if is logged in; otherwise, <c>false</c>.</value>
		public static bool IsLoggedIn {
			get {
				return Error == null &&
				Response != null &&
				Response.id != null;
			}
		}

		/// <summary>
		/// The error if a login was attempted.
		/// </summary>
		/// <value>The error.</value>
		public static Error Error { get; private set; }

		/// <summary>
		/// The result of logging in.
		/// </summary>
		/// <value>The response.</value>
		public static LoginResult Response {
			get {
				return _response;
			}
			private set {
				_response = value;
				if (_response != null) {
					PlayerPrefs.SetString (TokenKey, _response.token);
					PlayerPrefs.SetString (IdKey, _response.id);
				}
			}
		}

		/// <summary>
		/// The User's ID.
		/// </summary>
		/// <value>The user identifier.</value>
		public static string UserId {
			get {
				if (Response != null) {
					return Response.id;
				}

				string storedId = PlayerPrefs.GetString (IdKey);

				if (storedId != null) {
					return storedId;
				}

				return null;
			}
		}

		/// <summary>
		/// The Meteor login token. Generally you will not use this directly unless you need to save it in a different service. Accounts automatically saves tokens from
		/// successful login attempts for you.
		/// </summary>
		/// <value>The token.</value>
		public static string Token {
			get {
				if (Response != null) {
					return Response.token;

				}

				string storedToken = PlayerPrefs.GetString (TokenKey, null);

				if (storedToken != null) {
					return storedToken;
				}

				return null;
			}
		}

		/// <summary>
		/// Logs in with Facebook. Requires the Facebook SDK and the compile preprocessor symbol FACEBOOK defined.
		/// </summary>
		/// <returns>The with facebook.</returns>
		public static Coroutine LoginWithFacebook ()
		{
			// Check that we're connected to the server. If we're not, print an error.
			if (!LiveData.Instance.Connected) {
				Debug.LogWarning ("Meteor.Accounts: You are not connected to a server. Before you access methods on this service, make sure to connect.");
			}

			return CoroutineHost.Instance.StartCoroutine (LoginWithFacebookCoroutine ());
		}

		private static IEnumerator LoginWithFacebookCoroutine ()
		{
			#if FACEBOOK
			Error = null;
			var facebookHasInitialized = false;
			FB.Init (() => facebookHasInitialized = true);

			while (!facebookHasInitialized) {
				yield return null;
			}


			FBResult loginResult = null;
			FB.Login ("email", result => loginResult = result);

			while (loginResult == null) {
				yield return null;
			}

			if (!FB.IsLoggedIn) {
				Response = null;
				Error = new Error () {
					error = 500,
					reason = "Could not login to Facebook."
				};
				yield break;
			}

			string meResultText = null;
			string meResultError = null;
			var meResult = false;
			FB.API ("/me", Facebook.HttpMethod.GET, result => {
				meResult = true;
				meResultError = result.Error;
				meResultText = result.Text;
			});

			while (!meResult) {
				yield return null;
			}

			if (meResultText == null) {
				Response = null;
				Error = new Error () {
					error = 500,
					reason = meResultError
				};
				yield break;
			}

			var fbUser = meResultText.Deserialize<FacebookUser> ();

			var loginMethod = Method<LoginUserResult>.Call ("facebookLoginWithAccessToken", FB.UserId, fbUser.email ?? string.Format ("-{0}@facebook.com", FB.UserId), fbUser.name, FB.AccessToken);
			loginMethod.OnResponse += HandleOnLogin;
			yield return (Coroutine)loginMethod;

			#else
			UnityEngine.Debug.LogError ("Facebook login is not enabled with a build setting, or you're missing the Facebook SDK. Set the FACEBOOK compile preprocessor define symbol.");
			Error = new Error () {
				error = 500,
				reason = "Facebook login is not enabled with a build setting, or you're missing the Facebook SDK."
			};
			#endif

			yield break;
		}

		/// <summary>
		/// Logs in with a Google account. Currently not supported.
		/// </summary>
		/// <returns>A Coroutine used to execute the login.</returns>
		public static Coroutine LoginWithGoogle ()
		{
			throw new NotImplementedException ();
		}

		/// <summary>
		/// Log in with a saved token.
		/// </summary>
		/// <returns>A method. See the method document to correctly execute methods. <see cref="Meteor.Method`1"/></returns>
		public static Method<LoginResult> LoginWithToken ()
		{
			// Check that we're connected to the server. If we're not, print an error.
			if (!LiveData.Instance.Connected) {
				Debug.LogWarning ("Meteor.Accounts: You are not connected to a server. Before you access methods on this service, make sure to connect.");
			}

			var loginMethod = LiveData.Instance.Call<LoginResult> (LoginUserMethodName, new Meteor.Internal.LoginWithTokenOptions () {
				resume = Token
			});

			loginMethod.OnResponse += HandleOnLogin;

			return loginMethod;
		}

		/// <summary>
		/// Log in with a specified username and password. See the method documentation for correctly executing methods. <see cref="Meteor.Method`1"/>.
		/// </summary>
		/// <returns>A method instance. This can be cast to a coroutine you can execute in an IEnumerator/Coroutine. <see cref="Meteor.Method`1"/>.</returns>
		/// <param name="username">Username.</param>
		/// <param name="password">Password.</param>
		public static Method<LoginResult> LoginWith (string username, string password)
		{
			// Check that we're connected to the server. If we're not, print an error.
			if (!LiveData.Instance.Connected) {
				Debug.LogWarning ("Meteor.Accounts: You are not connected to a server. Before you access methods on this service, make sure to connect.");
			}

			var loginMethod = LiveData.Instance.Call<LoginResult> (LoginUserMethodName, new SecureLoginUserOptions () {
				password = new PasswordDigest (password),
				user = new LoginUserUser () {
					username = username
				}
			});

			loginMethod.OnResponse += HandleOnLogin;

			return loginMethod;
		}

		static void HandleOnLogin (Error error, LoginResult response)
		{
			if (LoginMethodWillComplete != null) {
				LoginMethodWillComplete (error, response);
			}

			Error = error;
			Response = response;

			if (error == null) {
				// Register for push
				CoroutineHost.Instance.StartCoroutine (RegisterForPush ());
			} else {
				Debug.LogError (error.reason);
				Debug.LogError (error.details);
			}

			if (LoginMethodDidComplete != null) {
				LoginMethodDidComplete (error, response);
			}
		}

		private static IEnumerator RegisterForPush ()
		{
			#if PUSH && UNITY_IOS
			UnityEngine.iOS.NotificationServices.RegisterForNotifications (UnityEngine.iOS.NotificationType.Alert | UnityEngine.iOS.NotificationType.Badge | UnityEngine.iOS.NotificationType.Sound);
			var deviceToken = UnityEngine.iOS.NotificationServices.deviceToken;

			while (deviceToken == null) {
				if (!string.IsNullOrEmpty (UnityEngine.iOS.NotificationServices.registrationError)) {
					yield break;
				}
				deviceToken = UnityEngine.iOS.NotificationServices.deviceToken;
				yield return new WaitForEndOfFrame ();
			}

			// Convert device token to hex
			var deviceTokenHex = new System.Text.StringBuilder (deviceToken.Length * 2);

			foreach (byte b in deviceToken) {
				deviceTokenHex.Append (b.ToString ("X2"));
			}

			Debug.Log (string.Format ("deviceToken: {0}, Application.platform: {1}", deviceTokenHex, Application.platform.ToString ()));

			var registerForPush = (Coroutine)Method.Call ("registerForPush", Application.platform.ToString (), deviceTokenHex.ToString ());
			#else
			yield break;
			#endif
		}

		/// <summary>
		/// Login as a guest. Use inside an IEnumerator/Coroutine.
		/// </summary>
		/// <example>
		/// yield return Meteor.Accounts.LoginAsGuest();
		/// </example>
		/// <returns>The as guest.</returns>
		public static Coroutine LoginAsGuest ()
		{
			// Check that we're connected to the server. If we're not, print an error.
			if (!LiveData.Instance.Connected) {
				Debug.LogWarning ("Meteor.Accounts: You are not connected to a server. Before you access methods on this service, make sure to connect.");
			}

			return CoroutineHost.Instance.StartCoroutine (LoginAsGuestCoroutine ());
		}

		static IEnumerator LoginAsGuestCoroutine ()
		{
			if (!string.IsNullOrEmpty (Token)) {
				var tokenLogin = LoginWithToken ();
				// If we can login with token, go for it.
				yield return (Coroutine)tokenLogin;
				if (tokenLogin.Error == null) {
					yield break;
				}
			}
			// Failed to login with token or we don't have a token. So create an account if we don't hvae saved credentials

			var guestUsername = PlayerPrefs.GetString (GuestUsernameKey, null);
			var guestEmail = PlayerPrefs.GetString (GuestEmailKey, null);
			var guestPassword = PlayerPrefs.GetString (GuestPasswordKey, null);

			if (!string.IsNullOrEmpty (guestUsername)) {
				yield return (Coroutine)Accounts.LoginWith (guestUsername, guestPassword);
				if (Error == null) {
					yield break;
				}
			}

			// If we still have an 

			var padding = UnityEngine.Random.Range (0, Int32.MaxValue);
			guestEmail = string.Format ("anonymous{0}@{1}", padding, GuestEmailDomain);
			guestUsername = string.Format ("player{0}", padding);
			guestPassword = UnityEngine.Random.Range (0, Int32.MaxValue).ToString ();
			PlayerPrefs.SetString (GuestUsernameKey, guestUsername);
			PlayerPrefs.SetString (GuestEmailKey, guestEmail);
			PlayerPrefs.SetString (GuestPasswordKey, guestPassword);
			yield return (Coroutine)Accounts.CreateAndLoginWith (guestEmail, guestUsername, guestPassword);
		}

		/// <summary>
		/// Creates a user.
		/// This function logs in as the newly created user on successful completion.
		/// You must pass password and at least one of username or email — enough information for the user to be able to log in again later. If there are existing users with a username or email only differing in case, createUser will fail.
		/// </summary>
		/// <returns>The and login with.</returns>
		/// <param name="email">Email. Currently unsupported.</param>
		/// <param name="username">Username.</param>
		/// <param name="password">Password.</param>
		public static Method<LoginResult> CreateAndLoginWith (string email, string username, string password)
		{
			var createUserAndLoginMethod = LiveData.Instance.Call<LoginResult> (CreateUserMethodName, new  CreateUserOptions () {
				password = new PasswordDigest (password),
				username = username
			});

			createUserAndLoginMethod.OnResponse += HandleOnLogin;

			return createUserAndLoginMethod;
		}

		static Accounts ()
		{
			Error = new Error () {
				error = 500,
				reason = "You have not attempted to login yet!"
			};
		}
	}
}
