using System;

public class CoroutineHost : MonoSingleton<CoroutineHost>
{
	public CoroutineHost ()
	{
	}

	void OnApplicationQuit() {
		base.OnApplicationQuit ();
		try {
			Meteor.LiveData.LiveData.Instance.Close();
		} catch (Exception e) {

		}
	}
}

