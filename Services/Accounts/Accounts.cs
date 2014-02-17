using System;
using System.Collections;
using UnityEngine;
using Meteor;
using Extensions;

namespace Meteor
{
	public static class Accounts
	{
		public static Error Error { get; private set; }

		public static string GuestEmailDomain = "example.com";
		public static string FacebookScope = "email";
		const string TokenKey = "Meteor.Accounts.Token";
		const string IdKey = "Meteor.Accounts.Id";
		const string GuestUsernameKey = "Meteor.Accounts.GuestUsername";
		const string GuestEmailKey = "Meteor.Accounts.GuestEmail";
		const string GuestPasswordKey = "Meteor.Accounts.GuestPassword";
		const string CreateUserMethodName = "createUser";
		const string LoginUserMethodName = "login";
		const string BeginPasswordExchangeMethodName = "beginPasswordExchange";
		static LoginUserResult _response;

		public static event Action<Error, LoginUserResult> UserDidLogin;

		public static Collection<MongoDocument> Users {
			get;
			private set;
		}

		public static LoginUserResult Response {
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

		public static string Token {
			get {
				if (Response == null) {
					return PlayerPrefs.GetString (TokenKey, null);
				} else {
					return Response.token;
				}
			}
		}

		public static Coroutine LoginWithFacebook ()
		{
			return CoroutineHost.Instance.StartCoroutine (LoginWithFacebookCoroutine ());
		}

		private static IEnumerator LoginWithFacebookCoroutine ()
		{
			#if FACEBOOK
			Error = null;
			var facebookHasInitialized = false;
			FB.Init(() => facebookHasInitialized = true);

			while (!facebookHasInitialized) {
				yield return null;
			}


			FBResult loginResult = null;
			FB.Login("email", result => loginResult = result);

			while (loginResult == null) {
				yield return null;
			}

			if (!FB.IsLoggedIn) {
				Response = null;
				Error = new Error() {
					error = 500,
					reason = "Could not login to Facebook."
				};
				yield break;
			}

			string meResultText = null;
			string meResultError = null;
			var meResult = false;
			FB.API("/me",Facebook.HttpMethod.GET, result => {
				meResult = true;
				meResultError = result.Error;
				meResultText = result.Text;
			});

			while (!meResult) {
				yield return null;
			}

			if (meResultText == null) {
				Response = null;
				Error = new Error() {
					error = 500,
					reason = meResultError
				};
				yield break;
			}

			var fbUser = meResultText.Deserialize<FacebookUser>();

			var loginMethod = Method<LoginUserResult>.Call ("facebookLoginWithAccessToken", FB.UserId, fbUser.email ?? string.Format("-{0}@facebook.com", FB.UserId), fbUser.name, FB.AccessToken);
			loginMethod.OnResponse += HandleOnLogin;
			yield return (Coroutine)loginMethod;

			#else
			UnityEngine.Debug.LogError ("Facebook login is not enabled with a build setting, or you're missing the Facebook SDK.");
			Error = new Error () {
				error = 500,
				reason = "Facebook login is not enabled with a build setting, or you're missing the Facebook SDK."
			};
			#endif

			yield break;
		}

		public static Coroutine LoginWithGoogle ()
		{
			throw new NotImplementedException ();
		}

		public static Method<LoginUserResult> LoginWithToken ()
		{
			var loginMethod = LiveData.Instance.Call<LoginUserResult> (LoginUserMethodName, new Meteor.LoginWithTokenOptions () {
				resume = Token
			});

			loginMethod.OnResponse += HandleOnLogin;

			return loginMethod;
		}

		public static Method<LoginUserResult> LoginWith (string username, string password)
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

			if (UserDidLogin != null)
			{
				UserDidLogin (error, response);
			}

			if (error == null) {
				CoroutineHost.Instance.StartCoroutine (RegisterForPush ());
			} else {
				Debug.LogWarning (error.reason);
			}
		}

		private static IEnumerator RegisterForPush ()
		{
			#if PUSH && UNITY_IOS
			NotificationServices.RegisterForRemoteNotificationTypes(RemoteNotificationType.Alert | RemoteNotificationType.Badge | RemoteNotificationType.Sound);
			var deviceToken = NotificationServices.deviceToken;

			while (deviceToken == null) {
				if (!string.IsNullOrEmpty(NotificationServices.registrationError)) {
					yield break;
				}
				deviceToken = NotificationServices.deviceToken;
				yield return new WaitForEndOfFrame();
			}

			// Convert device token to hex
			var deviceTokenHex = new System.Text.StringBuilder(deviceToken.Length*2);

			foreach (byte b in deviceToken) {
				deviceTokenHex.Append(b.ToString("X2"));
			}

			Debug.Log(string.Format("deviceToken: {0}, Application.platform: {1}", deviceTokenHex, Application.platform.ToString()));

			var registerForPush = (Coroutine)Method.Call("registerForPush", Application.platform.ToString(), deviceTokenHex.ToString());
			#else
			yield break;
			#endif
		}

		public static Coroutine LoginAsGuest ()
		{
			return CoroutineHost.Instance.StartCoroutine (LoginAsGuestCoroutine ());
		}

		static IEnumerator LoginAsGuestCoroutine ()
		{
			var tokenLogin = LoginWithToken ();
			// If we can login with token, go for it.
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
			guestUsername = string.Format ("anonymous{0}@{1}", padding, GuestEmailDomain);
			guestEmail = string.Format ("player{0}", padding);
			guestPassword = UnityEngine.Random.Range (0, Int32.MaxValue).ToString ();
			PlayerPrefs.SetString (GuestUsernameKey, guestUsername);
			PlayerPrefs.SetString (GuestEmailKey, guestEmail);
			PlayerPrefs.SetString (GuestPasswordKey, guestPassword);
			yield return Accounts.CreateAndLoginWith (guestUsername, guestEmail, guestPassword);
		}

		public static Coroutine LoginWithDevice ()
		{
			return CoroutineHost.Instance.StartCoroutine (LoginWithDeviceCoroutine ());
		}

		static IEnumerator LoginWithDeviceCoroutine()
		{
			var loginMethod = Method<LoginUserResult>.Call ("loginWithIDFV", SystemInfo.deviceUniqueIdentifier);
			loginMethod.OnResponse += HandleOnLogin;
			yield return (Coroutine)loginMethod;
		}

		public static Coroutine CreateAndLoginWith (string email, string username, string password)
		{
			var createUserAndLoginMethod = LiveData.Instance.Call<LoginUserResult> (CreateUserMethodName, new  CreateUserOptions () {
				profile = new Profile()
				{
					name = username
				},
				email = email,
				password = password,
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
			// Check that we're connected to the server. If we're not, print an error.
			if (!LiveData.Instance.Connected) {
				Debug.LogWarning ("Meteor.Accounts: You are not connected to a server. Before you access methods on this service, make sure to connect.");
			}
		}
	}
}
