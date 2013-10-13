using System;
using Net.DDP.Client;

public interface IAccountManager
{
	/// <summary>
	/// Login success when true, failure when false.
	/// </summary>
	event Action<bool> OnLogin;
	void LoginWithFacebook();
	void LoginWithGoogle();
	void LoginWith(string username, string password);
	void CreateAndLoginWith(string email, string username, string password);
	Net.DDP.Client.LiveData Client {get; set; }
}


